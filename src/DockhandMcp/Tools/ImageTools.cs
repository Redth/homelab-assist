using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace DockhandMcp.Tools;

[McpServerToolType]
internal sealed class ImageTools(DockhandClient client)
{
    [McpServerTool(Name = "dockhand_list_images"),
     Description("List Docker images on a Dockhand environment. Read-only.")]
    public Task<string> ListImages(
        [Description("Environment id")] int env,
        CancellationToken ct = default)
        => client.GetAsync("images" + DockhandClient.Env(env), ct);

    [McpServerTool(Name = "dockhand_pull_image"),
     Description("Pull an image (e.g. \"nginx:latest\") onto a Dockhand environment. SIGNIFICANT change — confirm first. Returns the pull result.")]
    public Task<string> PullImage(
        [Description("Environment id")] int env,
        [Description("Image reference, e.g. ghcr.io/org/app:1.2.3")] string image,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(new ImagePull(image), DockhandJsonContext.Default.ImagePull);
        return client.PostAsync("images/pull" + DockhandClient.Env(env), json, ct);
    }
}
