using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NpmMcp;
using NpmMcp.Tools;

var builder = Host.CreateApplicationBuilder(args);

// stdio transport: logs MUST go to stderr only, or they corrupt the JSON-RPC stream on stdout.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services.AddSingleton<NpmClient>();

builder.Services.AddMcpServer()
    .WithStdioServerTransport()
    // Explicit per-type registration (NOT WithToolsFromAssembly) so registration is AOT/trim-safe.
    .WithTools<ProxyHostTools>()
    .WithTools<CertificateTools>()
    .WithTools<RedirectionTools>()
    .WithTools<RoutingTools>();

await builder.Build().RunAsync();
