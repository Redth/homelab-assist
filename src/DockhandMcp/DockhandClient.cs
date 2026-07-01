using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DockhandMcp;

/// <summary>
/// Client for the Dockhand REST API (base path /api). Auth is one of:
///   - Bearer PAT   (DOCKHAND_TOKEN, a "dh_..." token) — preferred, no login round-trip
///   - username/pw  (DOCKHAND_USERNAME + DOCKHAND_PASSWORD) — cookie login, re-auth on 401
///   - none         (neither set) — for servers with auth disabled
/// GET responses are returned as raw JSON text; SSE responses (deploy/start/stop/etc.) are read to
/// completion and reduced to their final "result" event. Only constructed payloads are strongly typed
/// (via <see cref="DockhandJsonContext"/>) so the whole thing is Native-AOT safe.
/// </summary>
internal sealed class DockhandClient
{
    private enum Mode { None, Bearer, UserPass }

    private HttpClient? _http;
    private Mode _mode;
    private string? _token;
    private string _username = "";
    private string _password = "";
    private bool _loggedIn;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    private HttpClient EnsureConfigured()
    {
        if (_http is not null) return _http;

        var rawUrl = Environment.GetEnvironmentVariable("DOCKHAND_URL");
        if (string.IsNullOrWhiteSpace(rawUrl))
            throw new InvalidOperationException(
                "Dockhand is not configured. Set DOCKHAND_URL (e.g. http://dockhand.lan:3000) and either "
                + "DOCKHAND_TOKEN (a dh_ API token, preferred) or DOCKHAND_USERNAME + DOCKHAND_PASSWORD.");

        var token = Environment.GetEnvironmentVariable("DOCKHAND_TOKEN");
        var user = Environment.GetEnvironmentVariable("DOCKHAND_USERNAME");
        var pass = Environment.GetEnvironmentVariable("DOCKHAND_PASSWORD");

        if (!string.IsNullOrWhiteSpace(token))
        {
            _mode = Mode.Bearer;
            _token = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? token[7..] : token;
        }
        else if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(pass))
        {
            _mode = Mode.UserPass;
            _username = user!;
            _password = pass!;
        }
        else
        {
            _mode = Mode.None; // assume auth-disabled server; a 401 will produce a clear hint
        }

        var baseUrl = rawUrl!.TrimEnd('/');
        if (baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            baseUrl = baseUrl[..^4];

        var verify = (Environment.GetEnvironmentVariable("DOCKHAND_VERIFY_SSL") ?? "1") is not ("0" or "false" or "no");
        var handler = new SocketsHttpHandler { UseCookies = true, CookieContainer = new CookieContainer() };
        if (!verify)
            handler.SslOptions.RemoteCertificateValidationCallback = static (_, _, _, _) => true;

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl + "/api/"),
            Timeout = TimeSpan.FromMinutes(10) // deploys/pulls can be slow
        };
        return _http;
    }

    private async Task EnsureLoggedInAsync(CancellationToken ct)
    {
        if (_mode != Mode.UserPass || _loggedIn) return;
        await _authLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_loggedIn) return;
            var http = EnsureConfigured();
            var body = JsonSerializer.Serialize(new LoginRequest(_username, _password), DockhandJsonContext.Default.LoginRequest);
            using var req = new HttpRequestMessage(HttpMethod.Post, "auth/login")
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (resp.StatusCode == HttpStatusCode.BadRequest && text.Contains("not enabled", StringComparison.OrdinalIgnoreCase))
            {
                _mode = Mode.None; _loggedIn = true; return; // server has auth disabled
            }
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Dockhand login failed ({(int)resp.StatusCode}). Check DOCKHAND_USERNAME/PASSWORD. Server said: {text}");
            if (text.Contains("\"requiresMfa\":true", StringComparison.OrdinalIgnoreCase) || text.Contains("\"requiresMfa\": true", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Dockhand login requires MFA, which this server does not support. Use a DOCKHAND_TOKEN (dh_ API token) instead.");
            _loggedIn = true;
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
        await EnsureLoggedInAsync(ct).ConfigureAwait(false);

        using var req = new HttpRequestMessage(method, path);
        req.Headers.Accept.ParseAdd("application/json");
        req.Headers.Accept.ParseAdd("text/event-stream");
        if (_mode == Mode.Bearer)
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        if (json is not null)
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
        if (resp.StatusCode == HttpStatusCode.Unauthorized && _mode == Mode.UserPass && !isRetry)
        {
            _loggedIn = false; // session expired — re-login once
            return await SendAsync(method, path, json, ct, isRetry: true).ConfigureAwait(false);
        }

        var raw = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        var isSse = string.Equals(resp.Content.Headers.ContentType?.MediaType, "text/event-stream", StringComparison.OrdinalIgnoreCase);
        var bodyText = isSse ? ReduceSse(raw) : raw;
        return (resp.StatusCode, bodyText);
    }

    // SSE reduction: Dockhand streams `event: progress` lines then a terminal `event: result`. We return
    // the last `data:` payload (the result). If nothing parses, return the raw stream so nothing is lost.
    private static string ReduceSse(string raw)
    {
        string? last = null;
        foreach (var line in raw.Split('\n'))
        {
            var t = line.TrimEnd('\r');
            if (t.StartsWith("data:", StringComparison.Ordinal))
            {
                var payload = t.Length > 5 ? t[5..].TrimStart() : "";
                if (payload.Length > 0 && payload != "[DONE]") last = payload;
            }
        }
        return last ?? (string.IsNullOrWhiteSpace(raw) ? "{\"ok\":true}" : raw);
    }

    public Task<string> GetAsync(string path, CancellationToken ct) => GuardedAsync(HttpMethod.Get, path, null, ct);
    public Task<string> PostAsync(string path, string? json, CancellationToken ct) => GuardedAsync(HttpMethod.Post, path, json, ct);
    public Task<string> PutAsync(string path, string json, CancellationToken ct) => GuardedAsync(HttpMethod.Put, path, json, ct);
    public Task<string> DeleteAsync(string path, CancellationToken ct) => GuardedAsync(HttpMethod.Delete, path, null, ct);

    private async Task<string> GuardedAsync(HttpMethod method, string path, string? json, CancellationToken ct)
    {
        try
        {
            var (status, body) = await SendAsync(method, path, json, ct).ConfigureAwait(false);
            var ok = (int)status is >= 200 and < 300;
            if (ok) return string.IsNullOrWhiteSpace(body) ? "{\"ok\":true}" : body;
            var escaped = JsonSerializer.Serialize(body, DockhandJsonContext.Default.String);
            return $"{{\"ok\":false,\"status\":{(int)status},\"error\":{escaped}}}";
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            var escaped = JsonSerializer.Serialize(ex.Message, DockhandJsonContext.Default.String);
            return $"{{\"ok\":false,\"error\":{escaped}}}";
        }
    }

    // Helpers for building query strings.
    public static string Env(int env) => $"?env={env}";
    public static string Enc(string segment) => Uri.EscapeDataString(segment);
}
