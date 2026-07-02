# Homelab MCP servers (Native AOT C#)

Custom MCP servers for the [[homelab-assist]] `docker` plugin, built as **Native AOT** C# (net10.0) so they
ship as self-contained per-arch binaries — **no .NET runtime, no Docker container** on the user's machine.

## Built servers
- **`npm`** (`src/NpmMcp`, 12 tools) — nginx-proxy-manager: proxy hosts, certs, redirects, `npm_debug_routing`.
  Released `npm-mcp-v0.1.0`.
- **`dockhand`** (`src/DockhandMcp`, 26 tools) — Dockhand control plane (environments/stacks/containers/images/
  system). Native replacement for the container-based `strausmann/mcp-dockhand`. Released `dockhand-mcp-v0.1.0`.
  See [[Dockhand REST API]].

Both **verified end-to-end** (launcher downloads binary from the GitHub Release and handshakes) but **NOT yet
tested against a live NPM / Dockhand instance** — payload shapes/auth come from source analysis. Open item.

## AOT rules (must follow or it breaks at publish)
- `PublishAot=true` + `IsAotCompatible=true` (analyzers on); builds stay warning-free.
- JSON via **System.Text.Json source generation** (`[JsonSerializable]` context) — no reflection serialization.
- Register tools with **`.WithTools<T>()`**, never `.WithToolsFromAssembly()` (reflection → AOT failure).
- Tool params are primitives, return `string`; pass external JSON through as raw text (GET passthrough), only
  strongly-type the small payloads you construct. Surface config/auth errors as readable JSON tool results.
- The `dotnet new mcpserver` template did NOT resolve under the pinned SDK — hand-write the `.csproj` instead.
- ModelContextProtocol nuget 1.4.0; Microsoft.Extensions.Hosting 10.0.9. `src/global.json` pins SDK 10.0.300.
  .NET 10 `dotnet new sln` creates `.slnx` (XML) by default.

## Distribution (generalized)
- **One generic launcher** `plugins/docker/mcp/mcp-launch` (+`.cmd`), called as `mcp-launch <name>` (args in
  `.mcp.json`). Each server is just `plugins/docker/mcp/<name>/{VERSION,SECRETS}`. It detects OS/arch,
  downloads `<name>-mcp-<rid>` from the Release on first run, caches under `$CLAUDE_PLUGIN_DATA`, resolves any
  env vars listed in `SECRETS` from the OS store via [[homelab-secret]] (env wins over store), and execs.
- **One generic workflow** `.github/workflows/release-mcp.yml`: pushing tag `<name>-mcp-v<semver>` builds
  `src/<Name>Mcp` (name capitalized) for all 5 RIDs and attaches binaries to a GitHub Release.
- **RIDs**: linux-x64, linux-arm64 (`ubuntu-24.04-arm` runner works), win-x64, osx-arm64, and **osx-x64
  cross-compiled on `macos-latest`** — deliberately avoid the `macos-13` Intel runner (scarce, queues for
  many minutes). Same-OS cross-arch AOT (osx-arm64 host → osx-x64) also works locally.

## Add a new server
1. `src/<Name>Mcp` (copy an existing `.csproj`, keep AOT settings). 2. `plugins/docker/mcp/<name>/VERSION`
(`<name>-mcp-v0.1.0`) + `SECRETS`. 3. `.mcp.json` entry → `mcp/mcp-launch` with `args: ["<name>"]`.
4. Push tag `<name>-mcp-v0.1.0`. Release: bump csproj `<Version>` + VERSION file + push tag.

## Decision: no SSH-wrapping MCP servers
Custom MCP servers are only for real APIs/protocols (NPM REST, Dockhand REST). Anything whose only mechanism is
SSH stays a guardrailed skill — an MCP wrapper adds packaging + a second trust boundary without reducing risk.
So there is NO Proxmox-host-config MCP. See [[Homelab conventions]].

## Related
- [[homelab-assist]]
- [[Dockhand REST API]]
- [[Homelab conventions]]
- [[homelab-secret]]
