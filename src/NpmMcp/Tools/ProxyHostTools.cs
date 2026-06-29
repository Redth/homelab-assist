using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NpmMcp.Tools;

[McpServerToolType]
internal sealed class ProxyHostTools(NpmClient client)
{
    [McpServerTool(Name = "npm_list_proxy_hosts"),
     Description("List all nginx-proxy-manager proxy hosts (domains routed to upstream services), including enabled state, forward host/port/scheme, SSL/certificate, and flags. Read-only.")]
    public Task<string> ListProxyHosts(CancellationToken ct = default)
        => client.GetAsync("nginx/proxy-hosts?expand=certificate,access_list,owner", ct);

    [McpServerTool(Name = "npm_get_proxy_host"),
     Description("Get a single proxy host by its numeric id, with full detail. Read-only.")]
    public Task<string> GetProxyHost(
        [Description("Numeric proxy host id")] int id,
        CancellationToken ct = default)
        => client.GetAsync($"nginx/proxy-hosts/{id}?expand=certificate,access_list,owner", ct);

    [McpServerTool(Name = "npm_create_proxy_host"),
     Description("Create a proxy host that routes one or more domains to an upstream host:port. SIGNIFICANT change — confirm with the user first. Returns the created object or NPM's error body.")]
    public Task<string> CreateProxyHost(
        [Description("Comma-separated domain name(s), e.g. \"app.example.com,www.app.example.com\"")] string domainNames,
        [Description("Upstream host/IP that nginx forwards to (must be reachable from the NPM container)")] string forwardHost,
        [Description("Upstream port")] int forwardPort,
        [Description("Upstream scheme: \"http\" or \"https\"")] string forwardScheme = "http",
        [Description("Certificate id to attach for HTTPS, or 0 for none")] int certificateId = 0,
        [Description("Force HTTPS (redirect http→https). Requires a valid certificate.")] bool sslForced = false,
        [Description("Enable HTTP/2")] bool http2Support = false,
        [Description("Enable NPM 'Block Common Exploits'")] bool blockExploits = true,
        [Description("Allow WebSocket upgrade")] bool allowWebsocketUpgrade = true,
        [Description("Enable asset caching")] bool cachingEnabled = false,
        CancellationToken ct = default)
    {
        var payload = new ProxyHostPayload(
            DomainNames: SplitDomains(domainNames),
            ForwardScheme: forwardScheme,
            ForwardHost: forwardHost,
            ForwardPort: forwardPort,
            CertificateId: certificateId,
            SslForced: sslForced,
            Http2Support: http2Support,
            BlockExploits: blockExploits,
            AllowWebsocketUpgrade: allowWebsocketUpgrade,
            CachingEnabled: cachingEnabled,
            AccessListId: 0,
            AdvancedConfig: "",
            Locations: [],
            Meta: new NpmMeta());
        var json = JsonSerializer.Serialize(payload, NpmJsonContext.Default.ProxyHostPayload);
        return client.PostAsync("nginx/proxy-hosts", json, ct);
    }

    [McpServerTool(Name = "npm_update_proxy_host"),
     Description("Update fields on an existing proxy host. Only the parameters you provide are changed. SIGNIFICANT change — confirm with the user first.")]
    public Task<string> UpdateProxyHost(
        [Description("Numeric proxy host id")] int id,
        [Description("New comma-separated domain name(s), or empty to leave unchanged")] string domainNames = "",
        [Description("New upstream host, or empty to leave unchanged")] string forwardHost = "",
        [Description("New upstream port, or 0 to leave unchanged")] int forwardPort = 0,
        [Description("New upstream scheme (http/https), or empty to leave unchanged")] string forwardScheme = "",
        [Description("New certificate id, or -1 to leave unchanged")] int certificateId = -1,
        CancellationToken ct = default)
    {
        var payload = new ProxyHostPayload(
            DomainNames: string.IsNullOrWhiteSpace(domainNames) ? null : SplitDomains(domainNames),
            ForwardHost: string.IsNullOrWhiteSpace(forwardHost) ? null : forwardHost,
            ForwardPort: forwardPort > 0 ? forwardPort : null,
            ForwardScheme: string.IsNullOrWhiteSpace(forwardScheme) ? null : forwardScheme,
            CertificateId: certificateId >= 0 ? certificateId : null);
        var json = JsonSerializer.Serialize(payload, NpmJsonContext.Default.ProxyHostPayload);
        return client.PutAsync($"nginx/proxy-hosts/{id}", json, ct);
    }

    [McpServerTool(Name = "npm_enable_proxy_host"),
     Description("Enable a disabled proxy host. SIGNIFICANT change — confirm with the user first.")]
    public Task<string> EnableProxyHost(
        [Description("Numeric proxy host id")] int id, CancellationToken ct = default)
        => client.PostAsync($"nginx/proxy-hosts/{id}/enable", null, ct);

    [McpServerTool(Name = "npm_disable_proxy_host"),
     Description("Disable an active proxy host (stops routing its domains). SIGNIFICANT change — confirm with the user first.")]
    public Task<string> DisableProxyHost(
        [Description("Numeric proxy host id")] int id, CancellationToken ct = default)
        => client.PostAsync($"nginx/proxy-hosts/{id}/disable", null, ct);

    [McpServerTool(Name = "npm_delete_proxy_host"),
     Description("Permanently delete a proxy host. DESTRUCTIVE and irreversible — confirm with the user first and make sure they understand the domain will stop routing.")]
    public Task<string> DeleteProxyHost(
        [Description("Numeric proxy host id")] int id, CancellationToken ct = default)
        => client.DeleteAsync($"nginx/proxy-hosts/{id}", ct);

    private static string[] SplitDomains(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
