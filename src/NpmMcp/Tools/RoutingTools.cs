using System.ComponentModel;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NpmMcp.Tools;

[McpServerToolType]
internal sealed class RoutingTools(NpmClient client)
{
    [McpServerTool(Name = "npm_debug_routing"),
     Description("Diagnose how nginx-proxy-manager routes a given domain: finds the matching proxy host or redirection host, and reports the upstream forward host/port/scheme, whether it is enabled, SSL-forced, and which certificate is attached. Read-only. Use this to debug why a domain returns 502/404 or isn't routing.")]
    public async Task<string> DebugRouting(
        [Description("The domain/hostname to look up, e.g. \"jellyfin.example.com\"")] string domain,
        CancellationToken ct = default)
    {
        domain = domain.Trim().TrimEnd('.').ToLowerInvariant();
        var sb = new StringBuilder();
        sb.Append("Routing analysis for '").Append(domain).Append("':\n");

        try
        {
            return await AnalyzeAsync(domain, sb, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return $"Could not reach nginx-proxy-manager to analyze routing: {ex.Message}";
        }
    }

    private async Task<string> AnalyzeAsync(string domain, StringBuilder sb, CancellationToken ct)
    {
        var matched = false;

        // Proxy hosts
        var proxyJson = await client.GetRawAsync("nginx/proxy-hosts?expand=certificate", ct).ConfigureAwait(false);
        using (var doc = JsonDocument.Parse(proxyJson))
        {
            foreach (var host in doc.RootElement.EnumerateArray())
            {
                if (!DomainsContain(host, domain)) continue;
                matched = true;
                sb.Append("\n● Matched PROXY host id=").Append(GetInt(host, "id")).Append('\n');
                sb.Append("  domains: ").Append(JoinDomains(host)).Append('\n');
                sb.Append("  enabled: ").Append(IsTruthy(host, "enabled")).Append('\n');
                sb.Append("  forwards to: ").Append(GetStr(host, "forward_scheme")).Append("://")
                  .Append(GetStr(host, "forward_host")).Append(':').Append(GetInt(host, "forward_port")).Append('\n');
                sb.Append("  ssl_forced: ").Append(IsTruthy(host, "ssl_forced"))
                  .Append("  http2: ").Append(IsTruthy(host, "http2_support")).Append('\n');
                AppendCert(sb, host);

                if (!IsTruthy(host, "enabled"))
                    sb.Append("  ⚠ This host is DISABLED — the domain will not route. Enable it.\n");
                sb.Append("  ↳ If you get 502: confirm the upstream (forward_host:port) is reachable FROM the NPM container's network. ")
                  .Append("If 404: the request may be matching a different host or the upstream returns 404.\n");
            }
        }

        // Redirection hosts
        var redirJson = await client.GetRawAsync("nginx/redirection-hosts", ct).ConfigureAwait(false);
        using (var doc = JsonDocument.Parse(redirJson))
        {
            foreach (var host in doc.RootElement.EnumerateArray())
            {
                if (!DomainsContain(host, domain)) continue;
                matched = true;
                sb.Append("\n● Matched REDIRECTION host id=").Append(GetInt(host, "id")).Append('\n');
                sb.Append("  domains: ").Append(JoinDomains(host)).Append('\n');
                sb.Append("  enabled: ").Append(IsTruthy(host, "enabled")).Append('\n');
                sb.Append("  redirects to: ").Append(GetStr(host, "forward_scheme")).Append("://")
                  .Append(GetStr(host, "forward_domain_name"))
                  .Append(" (HTTP ").Append(GetInt(host, "forward_http_code")).Append(")\n");
            }
        }

        if (!matched)
        {
            sb.Append("\n✗ No proxy or redirection host matches '").Append(domain).Append("'.\n");
            sb.Append("  Checklist: (1) DNS for the domain points at NPM's IP; (2) the container exposes the expected port and ")
              .Append("has the right npm.* labels if you use npm-docker-sync; (3) npm-docker-sync is running and reconciled this host.\n");
        }
        return sb.ToString();
    }

    private static bool DomainsContain(JsonElement host, string domain)
    {
        if (!host.TryGetProperty("domain_names", out var names) || names.ValueKind != JsonValueKind.Array)
            return false;
        foreach (var n in names.EnumerateArray())
        {
            if (n.ValueKind == JsonValueKind.String &&
                string.Equals(n.GetString(), domain, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string JoinDomains(JsonElement host)
    {
        if (!host.TryGetProperty("domain_names", out var names) || names.ValueKind != JsonValueKind.Array)
            return "(none)";
        var parts = new List<string>();
        foreach (var n in names.EnumerateArray())
            if (n.ValueKind == JsonValueKind.String) parts.Add(n.GetString()!);
        return string.Join(", ", parts);
    }

    private static void AppendCert(StringBuilder sb, JsonElement host)
    {
        var certId = GetInt(host, "certificate_id");
        if (certId == 0) { sb.Append("  certificate: none\n"); return; }
        sb.Append("  certificate_id: ").Append(certId);
        if (host.TryGetProperty("certificate", out var cert) && cert.ValueKind == JsonValueKind.Object)
        {
            sb.Append(" (").Append(GetStr(cert, "nice_name"));
            if (cert.TryGetProperty("expires_on", out var exp) && exp.ValueKind == JsonValueKind.String)
                sb.Append(", expires ").Append(exp.GetString());
            sb.Append(')');
        }
        sb.Append('\n');
    }

    private static string GetStr(JsonElement e, string prop)
        => e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

    private static long GetInt(JsonElement e, string prop)
        => e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;

    // NPM returns booleans as 0/1 numbers in some versions and true/false in others.
    private static bool IsTruthy(JsonElement e, string prop)
    {
        if (!e.TryGetProperty(prop, out var v)) return false;
        return v.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => v.TryGetInt64(out var n) && n != 0,
            JsonValueKind.String => v.GetString() is "1" or "true",
            _ => false
        };
    }
}
