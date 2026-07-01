using System.ComponentModel;
using ModelContextProtocol.Server;

namespace DockhandMcp.Tools;

[McpServerToolType]
internal sealed class ContainerTools(DockhandClient client)
{
    [McpServerTool(Name = "dockhand_list_containers"),
     Description("List containers on a Dockhand environment. Read-only. By default includes stopped containers (all=true).")]
    public Task<string> ListContainers(
        [Description("Environment id")] int env,
        [Description("Include stopped containers")] bool all = true,
        CancellationToken ct = default)
        => client.GetAsync($"containers{DockhandClient.Env(env)}&all={(all ? "true" : "false")}", ct);

    [McpServerTool(Name = "dockhand_inspect_container"),
     Description("Inspect a container (raw Docker inspect JSON: mounts, networks, env, state). Read-only.")]
    public Task<string> InspectContainer(
        [Description("Environment id")] int env,
        [Description("Container id or name")] string id,
        CancellationToken ct = default)
        => client.GetAsync($"containers/{DockhandClient.Enc(id)}/inspect" + DockhandClient.Env(env), ct);

    [McpServerTool(Name = "dockhand_container_logs"),
     Description("Get recent logs for a container. Read-only. tail is the number of lines (default 200; use a number, or \"all\").")]
    public Task<string> ContainerLogs(
        [Description("Environment id")] int env,
        [Description("Container id or name")] string id,
        [Description("Number of trailing log lines, or \"all\"")] string tail = "200",
        CancellationToken ct = default)
        => client.GetAsync($"containers/{DockhandClient.Enc(id)}/logs{DockhandClient.Env(env)}&tail={DockhandClient.Enc(tail)}", ct);

    [McpServerTool(Name = "dockhand_start_container"),
     Description("Start a container. SIGNIFICANT change — confirm first.")]
    public Task<string> StartContainer(
        [Description("Environment id")] int env, [Description("Container id or name")] string id, CancellationToken ct = default)
        => client.PostAsync($"containers/{DockhandClient.Enc(id)}/start" + DockhandClient.Env(env), null, ct);

    [McpServerTool(Name = "dockhand_stop_container"),
     Description("Stop a container. SIGNIFICANT change — confirm first, note downtime.")]
    public Task<string> StopContainer(
        [Description("Environment id")] int env, [Description("Container id or name")] string id, CancellationToken ct = default)
        => client.PostAsync($"containers/{DockhandClient.Enc(id)}/stop" + DockhandClient.Env(env), null, ct);

    [McpServerTool(Name = "dockhand_restart_container"),
     Description("Restart a container. SIGNIFICANT change — confirm first, note brief downtime.")]
    public Task<string> RestartContainer(
        [Description("Environment id")] int env, [Description("Container id or name")] string id, CancellationToken ct = default)
        => client.PostAsync($"containers/{DockhandClient.Enc(id)}/restart" + DockhandClient.Env(env), null, ct);

    [McpServerTool(Name = "dockhand_delete_container"),
     Description("Remove a container. DESTRUCTIVE — confirm first. Prefer managing containers via their stack. force removes a running container.")]
    public Task<string> DeleteContainer(
        [Description("Environment id")] int env,
        [Description("Container id or name")] string id,
        [Description("Force-remove even if running")] bool force = false,
        CancellationToken ct = default)
        => client.DeleteAsync($"containers/{DockhandClient.Enc(id)}{DockhandClient.Env(env)}&force={(force ? "true" : "false")}", ct);
}
