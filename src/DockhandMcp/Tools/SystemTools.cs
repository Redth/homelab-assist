using System.ComponentModel;
using ModelContextProtocol.Server;

namespace DockhandMcp.Tools;

[McpServerToolType]
internal sealed class SystemTools(DockhandClient client)
{
    [McpServerTool(Name = "dockhand_host_info"),
     Description("Get information about the Dockhand host/server itself (the control plane), independent of any environment. Read-only.")]
    public Task<string> HostInfo(CancellationToken ct = default)
        => client.GetAsync("host", ct);

    [McpServerTool(Name = "dockhand_health"),
     Description("Health check of the Dockhand server. Read-only. Use to confirm connectivity/auth are working.")]
    public Task<string> Health(CancellationToken ct = default)
        => client.GetAsync("health", ct);
}
