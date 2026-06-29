using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NpmMcp.Tools;

[McpServerToolType]
internal sealed class RedirectionTools(NpmClient client)
{
    [McpServerTool(Name = "npm_list_redirection_hosts"),
     Description("List all nginx-proxy-manager redirection hosts (domains that redirect to another domain/URL), including target, HTTP code, and enabled state. Read-only.")]
    public Task<string> ListRedirectionHosts(CancellationToken ct = default)
        => client.GetAsync("nginx/redirection-hosts?expand=certificate,owner", ct);

    [McpServerTool(Name = "npm_get_redirection_host"),
     Description("Get a single redirection host by its numeric id, with full detail. Read-only.")]
    public Task<string> GetRedirectionHost(
        [Description("Numeric redirection host id")] int id,
        CancellationToken ct = default)
        => client.GetAsync($"nginx/redirection-hosts/{id}?expand=certificate,owner", ct);
}
