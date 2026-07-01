using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace DockhandMcp.Tools;

[McpServerToolType]
internal sealed class StackTools(DockhandClient client)
{
    [McpServerTool(Name = "dockhand_list_stacks"),
     Description("List all compose stacks on a Dockhand environment (by env id). Returns stack names and status. Read-only. Returns an empty list if the env id is wrong or the host has no stacks.")]
    public Task<string> ListStacks(
        [Description("Environment id (from dockhand_list_environments)")] int env,
        CancellationToken ct = default)
        => client.GetAsync("stacks" + DockhandClient.Env(env), ct);

    [McpServerTool(Name = "dockhand_get_stack_compose"),
     Description("Get the compose file content of a stack (plus its on-disk paths). Read-only. Use this before proposing edits.")]
    public Task<string> GetStackCompose(
        [Description("Environment id")] int env,
        [Description("Stack name")] string name,
        CancellationToken ct = default)
        => client.GetAsync($"stacks/{DockhandClient.Enc(name)}/compose" + DockhandClient.Env(env), ct);

    [McpServerTool(Name = "dockhand_get_stack_env"),
     Description("Get the environment variables configured for a stack (secrets are masked). Read-only.")]
    public Task<string> GetStackEnv(
        [Description("Environment id")] int env,
        [Description("Stack name")] string name,
        CancellationToken ct = default)
        => client.GetAsync($"stacks/{DockhandClient.Enc(name)}/env" + DockhandClient.Env(env), ct);

    [McpServerTool(Name = "dockhand_deploy_stack"),
     Description("Deploy/redeploy a stack (docker compose up). SIGNIFICANT change — confirm with the user first, note likely downtime for recreated services. Optionally pull newer images or force-recreate. Returns the deploy result.")]
    public Task<string> DeployStack(
        [Description("Environment id")] int env,
        [Description("Stack name")] string name,
        [Description("Pull newer images before deploying")] bool pull = false,
        [Description("Build images from source if the compose defines build:")] bool build = false,
        [Description("Force-recreate containers even if unchanged")] bool forceRecreate = false,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(new DeployOptions(pull ? true : null, build ? true : null, forceRecreate ? true : null),
            DockhandJsonContext.Default.DeployOptions);
        return client.PostAsync($"stacks/{DockhandClient.Enc(name)}/deploy" + DockhandClient.Env(env), json, ct);
    }

    [McpServerTool(Name = "dockhand_start_stack"),
     Description("Start a stopped stack. SIGNIFICANT change — confirm first.")]
    public Task<string> StartStack(
        [Description("Environment id")] int env, [Description("Stack name")] string name, CancellationToken ct = default)
        => client.PostAsync($"stacks/{DockhandClient.Enc(name)}/start" + DockhandClient.Env(env), null, ct);

    [McpServerTool(Name = "dockhand_stop_stack"),
     Description("Stop a running stack (containers stop but are not removed). SIGNIFICANT change — confirm first, note downtime.")]
    public Task<string> StopStack(
        [Description("Environment id")] int env, [Description("Stack name")] string name, CancellationToken ct = default)
        => client.PostAsync($"stacks/{DockhandClient.Enc(name)}/stop" + DockhandClient.Env(env), null, ct);

    [McpServerTool(Name = "dockhand_restart_stack"),
     Description("Restart a stack. SIGNIFICANT change — confirm first, note brief downtime.")]
    public Task<string> RestartStack(
        [Description("Environment id")] int env, [Description("Stack name")] string name, CancellationToken ct = default)
        => client.PostAsync($"stacks/{DockhandClient.Enc(name)}/restart" + DockhandClient.Env(env), null, ct);

    [McpServerTool(Name = "dockhand_update_stack_compose"),
     Description("Replace a stack's compose file content. SIGNIFICANT change — confirm first; show the diff. If restart=true the stack is redeployed (force-recreate) after saving; if false it only saves the file.")]
    public Task<string> UpdateStackCompose(
        [Description("Environment id")] int env,
        [Description("Stack name")] string name,
        [Description("The full new compose YAML content")] string content,
        [Description("Redeploy the stack after saving (true) or just save the file (false)")] bool restart = false,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(new ComposeUpdate(content, restart), DockhandJsonContext.Default.ComposeUpdate);
        return client.PutAsync($"stacks/{DockhandClient.Enc(name)}/compose" + DockhandClient.Env(env), json, ct);
    }

    [McpServerTool(Name = "dockhand_create_stack"),
     Description("Create a new compose stack from inline compose YAML. SIGNIFICANT change — confirm first. If start=true it deploys immediately; otherwise it is created but not started.")]
    public Task<string> CreateStack(
        [Description("Environment id")] int env,
        [Description("New stack name")] string name,
        [Description("The full compose YAML content")] string compose,
        [Description("Deploy immediately after creating")] bool start = false,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(new StackCreate(name, compose, start), DockhandJsonContext.Default.StackCreate);
        return client.PostAsync("stacks" + DockhandClient.Env(env), json, ct);
    }

    [McpServerTool(Name = "dockhand_down_stack"),
     Description("Take a stack down (docker compose down — stops AND removes its containers/networks). DESTRUCTIVE, and if removeVolumes=true it DELETES named volumes (data loss). Confirm explicitly and call out volume data at risk.")]
    public Task<string> DownStack(
        [Description("Environment id")] int env,
        [Description("Stack name")] string name,
        [Description("Also remove named volumes — DELETES their data")] bool removeVolumes = false,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(new DownOptions(removeVolumes ? true : null), DockhandJsonContext.Default.DownOptions);
        return client.PostAsync($"stacks/{DockhandClient.Enc(name)}/down" + DockhandClient.Env(env), json, ct);
    }

    [McpServerTool(Name = "dockhand_delete_stack"),
     Description("Permanently delete a stack from Dockhand. DESTRUCTIVE and irreversible — confirm first. force also tears it down; volumes also deletes its named volumes (data loss).")]
    public Task<string> DeleteStack(
        [Description("Environment id")] int env,
        [Description("Stack name")] string name,
        [Description("Force down before deleting")] bool force = false,
        [Description("Also delete named volumes — DELETES their data")] bool volumes = false,
        CancellationToken ct = default)
        => client.DeleteAsync($"stacks/{DockhandClient.Enc(name)}" + DockhandClient.Env(env) + $"&force={(force ? "true" : "false")}&volumes={(volumes ? "true" : "false")}", ct);
}
