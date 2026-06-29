# MCP servers (C# / Native AOT)

Custom MCP servers we build to fill gaps the existing community servers don't cover (see the repo's hybrid
MCP strategy in [`docs/DESIGN.md`](../docs/DESIGN.md)). Each is a Native-AOT C# project so it ships as a
single self-contained binary with **no .NET runtime required** on the user's machine.

## Servers

| Project | Plugin | Fills gap |
| --- | --- | --- |
| [`NpmMcp`](NpmMcp) | `docker` | Manage nginx-proxy-manager directly (proxy hosts, certs, redirects, routing debug). No community MCP existed. |

## Build & run locally

```bash
cd src/NpmMcp
dotnet build -c Release                 # fast JIT build for iteration
dotnet run                              # runs the stdio server (reads JSON-RPC on stdin)
```

Native AOT publish for the current platform:

```bash
dotnet publish -c Release -r osx-arm64 -o out      # or linux-x64, linux-arm64, win-x64, osx-x64
./out/npm-mcp                                       # self-contained native binary
```

The SDK is pinned via [`global.json`](global.json) to .NET 10. AOT cross-compilation is **per-platform**
(you build each RID on a matching OS), which is why distribution uses a CI matrix — see below.

## Distribution

- **CI:** [`.github/workflows/release-npm-mcp.yml`](../.github/workflows/release-npm-mcp.yml) builds the AOT
  binary for `linux-x64`, `linux-arm64`, `win-x64`, `osx-x64`, `osx-arm64` and attaches them to a GitHub
  Release. Trigger by pushing a tag like `npm-mcp-v0.1.0`.
- **Consumption:** the `docker` plugin ships a thin launcher
  ([`plugins/docker/mcp/npm/npm-mcp`](../plugins/docker/mcp/npm/npm-mcp)) that detects the client OS/arch,
  downloads the matching binary on first run (tag pinned in the sibling `VERSION` file), caches it, and execs
  it. The plugin's `.mcp.json` points at the launcher.

## AOT rules of thumb (followed by these projects)

- `IsAotCompatible=true` turns on the trim/AOT analyzers — builds must stay warning-free.
- No reflection-based JSON: use `System.Text.Json` **source generation** (`JsonSerializerContext`).
- Register MCP tools explicitly with `.WithTools<T>()`, never `.WithToolsFromAssembly()` (reflection).
- Keep tool parameters primitive and return strings; pass external JSON through rather than round-tripping it
  through reflected types.

## Releasing a new version

1. Bump `<Version>` in the project's `.csproj`.
2. Update the pinned tag in the plugin's `mcp/<server>/VERSION` (e.g. `npm-mcp-v0.2.0`).
3. Commit, then push the matching tag: `git tag npm-mcp-v0.2.0 && git push --tags`.
4. CI publishes the release; the launcher picks up the new binary on next start.
