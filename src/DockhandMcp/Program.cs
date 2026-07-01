using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DockhandMcp;
using DockhandMcp.Tools;

var builder = Host.CreateApplicationBuilder(args);

// stdio transport: logs MUST go to stderr only, or they corrupt the JSON-RPC stream on stdout.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<DockhandClient>();

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    // Explicit per-type registration (NOT WithToolsFromAssembly) so registration is AOT/trim-safe.
    .WithTools<EnvironmentTools>()
    .WithTools<StackTools>()
    .WithTools<ContainerTools>()
    .WithTools<ImageTools>()
    .WithTools<SystemTools>();

await builder.Build().RunAsync();
