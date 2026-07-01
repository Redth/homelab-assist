# MCP servers (C# / Native AOT)

Custom MCP servers we build to fill gaps the existing community servers don't cover (see the repo's hybrid
MCP strategy in [`docs/DESIGN.md`](../docs/DESIGN.md)). Each is a Native-AOT C# project so it ships as a
single self-contained binary with **no .NET runtime required** on the user's machine.

## Servers

| Project | Plugin | Fills gap |
| --- | --- | --- |
| [`NpmMcp`](NpmMcp) | `docker` | Manage nginx-proxy-manager directly (proxy hosts, certs, redirects, routing debug). No community MCP existed. |
| [`DockhandMcp`](DockhandMcp) | `docker` | Manage Dockhand (environments, stacks, containers, images) directly — same idea as `strausmann/mcp-dockhand` but a self-contained CLI binary, no Docker container needed. |

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

- **CI:** [`.github/workflows/release-mcp.yml`](../.github/workflows/release-mcp.yml) is generic — a tag
  `<name>-mcp-v<semver>` builds `src/<Name>Mcp` for `linux-x64`, `linux-arm64`, `win-x64`, `osx-x64`,
  `osx-arm64` and attaches the binaries to a GitHub Release. (e.g. `npm-mcp-v0.1.0` → `src/NpmMcp`,
  `dockhand-mcp-v0.1.0` → `src/DockhandMcp`.)
- **Consumption:** the `docker` plugin ships one generic launcher
  ([`plugins/docker/mcp/mcp-launch`](../plugins/docker/mcp/mcp-launch)); `.mcp.json` calls it with the server
  name (`args: ["dockhand"]`). It detects the client OS/arch, downloads the matching binary on first run (tag
  pinned in `mcp/<name>/VERSION`), caches it, resolves any secrets listed in `mcp/<name>/SECRETS` from the OS
  secret store, and execs the binary.

## Adding a new server

1. Create `src/<Name>Mcp` (copy an existing project's `.csproj`; keep the AOT settings).
2. Add `plugins/docker/mcp/<name>/VERSION` (pin `<name>-mcp-v0.1.0`) and `SECRETS` (env-var names).
3. Add an entry to `plugins/docker/.mcp.json` pointing `command` at `mcp/mcp-launch` with `args: ["<name>"]`.
4. Commit, then push the tag `<name>-mcp-v0.1.0` — CI builds and releases all platforms.

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
