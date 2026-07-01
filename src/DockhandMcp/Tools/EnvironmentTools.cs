using System.ComponentModel;
using ModelContextProtocol.Server;

namespace DockhandMcp.Tools;

[McpServerToolType]
internal sealed class EnvironmentTools(DockhandClient client)
{
    [McpServerTool(Name = "dockhand_list_environments"),
     Description("List all Dockhand environments (managed Docker hosts, incl. Hawser Standard/Edge agents). Returns each environment's id, name, and connection type. The numeric 'id' is the env id used to scope almost every other Dockhand tool. Read-only. Call this first to discover which host to act on.")]
    public Task<string> ListEnvironments(CancellationToken ct = default)
        => client.GetAsync("environments", ct);

    [McpServerTool(Name = "dockhand_get_environment"),
     Description("Get a single Dockhand environment by its numeric id, with full detail. Read-only.")]
    public Task<string> GetEnvironment(
        [Description("Numeric environment id")] int id,
        CancellationToken ct = default)
        => client.GetAsync($"environments/{id}", ct);

    [McpServerTool(Name = "dockhand_test_environment"),
     Description("Test connectivity to a saved Dockhand environment (checks the Docker/Hawser connection). Read-only diagnostic.")]
    public Task<string> TestEnvironment(
        [Description("Numeric environment id")] int id,
        CancellationToken ct = default)
        => client.PostAsync($"environments/{id}/test", null, ct);

    [McpServerTool(Name = "dockhand_system_info"),
     Description("Get Docker system info (version, containers/images counts, resources) for an environment. Read-only.")]
    public Task<string> SystemInfo(
        [Description("Numeric environment id")] int env,
        CancellationToken ct = default)
        => client.GetAsync("system" + DockhandClient.Env(env), ct);
}
