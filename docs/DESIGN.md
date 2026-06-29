# Design

## Goals

A **living** marketplace of Claude Code plugins that let an AI session safely perform real homelab
management. First targets: **Proxmox VE** and **Docker**. The architecture must scale to more targets
(TrueNAS, OPNsense, Home Assistant, k3s, etc.) without re-litigating the cross-cutting concerns each time.

## Layering

```
homelab-core   ── safety + research + routing conventions (the spine)
    ▲
    │  every infra skill references back to these
    │
proxmox        ── REST-API-first skills, SSH-fallback host-config skill
docker         ── Dockhand-first + compose + Portainer/Dockge + networking
```

`homelab-core` owns the two rules that must never drift:

- **Safety / approval gate** (`homelab-safety` skill): what is read-only vs significant vs destructive, and
  the SSH-confirmation requirement.
- **Version-correct research** (`homelab-research` skill): how to find canonical, current, version-specific
  docs and not trust stale search results.

Infra skills are written to **defer** to these — they don't re-implement the policy, they invoke it.

## API-first, SSH-as-fallback

| Layer | Use when | Mechanism |
| --- | --- | --- |
| **Control-plane API** | Default for everything possible | Proxmox REST API (token auth); Dockhand/Portainer REST; Docker Engine API |
| **Bundled/known MCP server** | When configured by the user | `.mcp.json` referencing community servers (opt-in) |
| **SSH / host file edit** | Only when the API can't do it | Documented per-skill, gated behind explicit user confirmation |

Examples that require SSH on Proxmox: GPU/PCIe passthrough (`/etc/pve/qemu-server/<id>.conf`,
`/etc/modprobe.d`, kernel cmdline), unprivileged-LXC `lxc.idmap`, hookscripts in `/var/lib/vz/snippets`,
advanced `/etc/network/interfaces`. These are isolated into `proxmox-host-config`.

## MCP strategy: hybrid

- **Reuse** mature community servers where they exist and are well-maintained:
  - Proxmox → [`ProxmoxMCP-Plus`](https://github.com/RekklesNA/ProxmoxMCP-Plus) (Python, OpenAPI bridge)
  - Dockhand → [`strausmann/mcp-dockhand`](https://github.com/strausmann/mcp-dockhand) (130+ tools)
  - Portainer → [`portainer/portainer-mcp`](https://github.com/portainer/portainer-mcp) (official, OpenAPI-generated)
- **Build our own** (C#/.NET, matching the maintainer's toolchain) only for gaps **that have a real
  API/protocol to wrap**:
  - **`npm` — nginx-proxy-manager (BUILT)** — `src/NpmMcp`. Manages proxy hosts, certificates, redirects, and
    a `debug_routing` diagnostic over the NPM REST API. First custom server / proven template.
  - Dockge (has a Socket.IO interface) — possible future candidate if its protocol is worth wrapping.
- **We do NOT build SSH-wrapping MCP servers.** If the only way a server could do its job is by shelling out
  over SSH, an MCP wrapper buys nothing — it adds packaging and a *second* trust boundary on top of SSH's
  existing risk. Those operations stay in **guardrailed skills** instead: `homelab-core:homelab-ssh` (the SSH
  execution discipline) plus the host-level skills (`proxmox:proxmox-host-config`, `docker:dockge`). This is
  why there is no "Proxmox host-config MCP server" — see `homelab-core:homelab-ssh`.
- **MCP is always optional.** A `.mcp.json` declares recommended servers with secrets pulled from env / user
  config. Skills must function with documented REST calls + helper scripts when no server is configured.

### Custom server build & distribution (Native AOT)

Our own servers are **Native AOT** C# projects under `src/` — single self-contained binaries needing **no
.NET runtime** on the user's machine. Because AOT compiles per-platform, distribution is:

1. **CI matrix** (`.github/workflows/release-*.yml`) builds the binary for `linux-x64`, `linux-arm64`,
   `win-x64`, `osx-x64`, `osx-arm64` and attaches them to a **GitHub Release** (triggered by a
   `npm-mcp-v*`-style tag).
2. **Thin launcher** in the plugin (`plugins/<plugin>/mcp/<server>/`) detects the client OS/arch, downloads
   the matching binary on first run (release tag pinned in a `VERSION` file), caches it, and execs it. The
   plugin's `.mcp.json` points `command` at the launcher via `${CLAUDE_PLUGIN_ROOT}`.

This keeps the repo lean (no committed binaries) while letting the plugin run on any client architecture
offline-after-first-fetch. See [`src/README.md`](../src/README.md) for the AOT rules and release steps.

## Secrets

Never commit credentials. The preferred home for secrets is the **OS-native secret store** (macOS Keychain,
Windows Credential Vault, Linux libsecret/`pass`), accessed via the `homelab-core:homelab-secrets` skill and
its `homelab-secret` helper — not plaintext dotfiles, the repo, or command-line args. Secrets are injected
into a command/server's environment just-in-time:

- **Helper scripts** read tokens from env; wrap them as `homelab-secret run <KEY> -- <script>`.
- **Our MCP servers** (e.g. `npm`) have launchers that auto-resolve missing `*_` env vars from the store when
  `homelab-secret` is on `PATH`, so nothing has to live in a dotfile.
- **Reused community MCP servers** take secrets via env (`${VAR}`); populate that env from the store
  (`homelab-secret run`) or the session that launches Claude Code.

`.gitignore` blocks `.env`, `*.secret`, and keys. The agent must **never print a secret value** into the
conversation. The exact `.mcp.json` interpolation tokens supported by the current Claude Code version
(`${CLAUDE_PLUGIN_ROOT}`, `${user_config.*}`, `${VAR}`) are verified against the official docs during
implementation.

## Adding a new target plugin

1. Create `plugins/<target>/.claude-plugin/plugin.json`.
2. Add an `<target>-overview` router skill that (a) detects version, (b) decides API vs SSH, (c) points to
   `homelab-core` safety + research.
3. Add task skills (lifecycle, storage, etc.), each deferring to the safety gate for writes.
4. Add a `reference/` folder for version notes, endpoint maps, and config-file locations.
5. Optionally add a `.mcp.json` referencing a known server (opt-in).
6. Register the plugin in `.claude-plugin/marketplace.json`.
