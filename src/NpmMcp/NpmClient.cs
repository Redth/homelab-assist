using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace NpmMcp;

/// <summary>
/// Thin client for the nginx-proxy-manager REST API. Authenticates with email+password to
/// obtain a bearer token, caches it, and re-authenticates on 401. GET responses are returned
/// as raw JSON text (the caller is an LLM that reads JSON directly); only the small payloads
/// we construct are strongly typed via <see cref="NpmJsonContext"/> for AOT safety.
/// </summary>
internal sealed class NpmClient
{
    private HttpClient? _http;
    private string _email = "";
    private string _password = "";
    private string? _token;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    /// <summary>
    /// Lazily reads config from the environment and builds the HttpClient. Throws a clear,
    /// user-facing message if required env vars are missing. Called on first request so a
    /// misconfiguration surfaces as a readable tool result, not a DI-time crash.
    /// </summary>
    private HttpClient EnsureConfigured()
    {
        if (_http is not null) return _http;

        var rawUrl = Environment.GetEnvironmentVariable("NPM_URL");
        var email = Environment.GetEnvironmentVariable("NPM_EMAIL");
        var password = Environment.GetEnvironmentVariable("NPM_PASSWORD");

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(rawUrl)) missing.Add("NPM_URL (e.g. http://npm.lan:81)");
        if (string.IsNullOrWhiteSpace(email)) missing.Add("NPM_EMAIL");
        if (string.IsNullOrWhiteSpace(password)) missing.Add("NPM_PASSWORD");
        if (missing.Count > 0)
            throw new InvalidOperationException(
                "nginx-proxy-manager is not configured. Set the following environment variable(s): "
                + string.Join(", ", missing) + ".");

        _email = email!;
        _password = password!;

        // Normalize: strip trailing slash and a trailing /api if the user included it.
        var baseUrl = rawUrl!.TrimEnd('/');
        if (baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^4];

        var verify = (Environment.GetEnvironmentVariable("NPM_VERIFY_SSL") ?? "1") is not ("0" or "false" or "no");
        var handler = new SocketsHttpHandler();
        if (!verify)
            handler.SslOptions.RemoteCertificateValidationCallback = static (_, _, _, _) => true;

        _http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl + "/api/"), Timeout = TimeSpan.FromSeconds(30) };
        return _http;
    }

    private async Task EnsureAuthAsync(CancellationToken ct)
    {
        if (_token is not null) return;
        await _authLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_token is not null) return;
            var body = JsonSerializer.Serialize(new TokenRequest(_email, _password), NpmJsonContext.Default.TokenRequest);
            using var req = new HttpRequestMessage(HttpMethod.Post, "tokens")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            using var resp = await EnsureConfigured().SendAsync(req, ct).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"NPM auth failed ({(int)resp.StatusCode}). Check NPM_EMAIL/NPM_PASSWORD. NPM said: {text}");
            var token = JsonSerializer.Deserialize(text, NpmJsonContext.Default.TokenResponse)?.Token;
            _token = token ?? throw new InvalidOperationException("NPM auth returned no token.");
        }
        finally
        {
            _authLock.Release();
        }
    }

    private async Task<(HttpStatusCode Status, string Body)> SendAsync(
        HttpMethod method, string path, string? json, CancellationToken ct, bool isRetry = false)
    {
        var http = EnsureConfigured();
        await EnsureAuthAsync(ct).ConfigureAwait(false);
        using var req = new HttpRequestMessage(method, path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        if (json is not null)
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
        if (resp.StatusCode == HttpStatusCode.Unauthorized && !isRetry)
        {
            _token = null; // token expired/invalid — re-auth once
            return await SendAsync(method, path, json, ct, isRetry: true).ConfigureAwait(false);
        }
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return (resp.StatusCode, body);
    }

    /// <summary>GET, returning the raw JSON body (or an error envelope the agent can read).</summary>
    public Task<string> GetAsync(string path, CancellationToken ct)
        => GuardedAsync(HttpMethod.Get, path, null, ct);

    public Task<string> PostAsync(string path, string? json, CancellationToken ct)
        => GuardedAsync(HttpMethod.Post, path, json, ct);

    public Task<string> PutAsync(string path, string json, CancellationToken ct)
        => GuardedAsync(HttpMethod.Put, path, json, ct);

    public Task<string> DeleteAsync(string path, CancellationToken ct)
        => GuardedAsync(HttpMethod.Delete, path, null, ct);

    // Converts config/network/auth exceptions into a readable error envelope so the model
    // sees exactly what went wrong (missing env, bad creds, host unreachable) instead of a
    // generic framework "an error occurred" message.
    private async Task<string> GuardedAsync(HttpMethod method, string path, string? json, CancellationToken ct)
    {
        try
        {
            var (status, body) = await SendAsync(method, path, json, ct).ConfigureAwait(false);
            return Envelope(status, body);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            var escaped = JsonSerializer.Serialize(ex.Message, NpmJsonContext.Default.String);
            return $"{{\"ok\":false,\"error\":{escaped}}}";
        }
    }

    /// <summary>Raw GET for internal parsing (routing debug). Throws on non-success.</summary>
    public async Task<string> GetRawAsync(string path, CancellationToken ct)
    {
        var (status, body) = await SendAsync(HttpMethod.Get, path, null, ct).ConfigureAwait(false);
        if ((int)status is < 200 or >= 300)
            throw new InvalidOperationException($"NPM GET {path} failed ({(int)status}): {body}");
        return body;
    }

    private static string Envelope(HttpStatusCode status, string body)
    {
        var ok = (int)status is >= 200 and < 300;
        if (ok)
            return string.IsNullOrWhiteSpace(body) ? "{\"ok\":true}" : body;
        // Surface NPM's error body verbatim so the model/user can see exactly what failed.
        var escaped = JsonSerializer.Serialize(body, NpmJsonContext.Default.String);
        return $"{{\"ok\":false,\"status\":{(int)status},\"error\":{escaped}}}";
    }
}
