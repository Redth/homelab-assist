# homelab-assist

A Claude Code **plugin marketplace** for managing homelab infrastructure, in `~/code/homelabassist`, hosted at
GitHub **`Redth/homelab-assist`** (PUBLIC; was briefly `Redth/homelab`, renamed). Scaffolded 2026-06 in session
17191c3f.

## Layout
- `.claude-plugin/marketplace.json` — catalog. Plugin `source` values MUST be explicit relative paths starting
  with `./` (e.g. `"./plugins/proxmox"`). Do NOT use `metadata.pluginRoot` + bare names — older Claude Code
  versions reject that with *"this plugin uses a source type your Claude Code version does not support."*
  Marketplace internal name is `homelab-assist`, so installs are `<plugin>@homelab-assist` regardless of repo name.
- `plugins/homelab-core/` — the spine (5 skills): `homelab-safety`, `homelab-ssh`, `homelab-secrets`,
  `homelab-research`, `homelab-overview`. See [[Homelab conventions]].
- `plugins/proxmox/` — REST-API-first skills + `proxmox-host-config` (SSH-only) + `scripts/pve-api.sh`.
- `plugins/docker/` — Dockhand-first + Portainer/Dockge/compose skills + reverse-proxy + docker-on-proxmox,
  and the native MCP servers (see [[Homelab MCP servers]]).
- `src/` — custom Native AOT C# MCP servers. `docs/` — DESIGN, VERSIONING, contributing.

## Install
```
/plugin marketplace add Redth/homelab-assist
/plugin install homelab-core@homelab-assist
/plugin install docker@homelab-assist
```
Cross-plugin skill references use the **namespaced name** (`homelab-core:homelab-safety`), never a relative
file path — cross-plugin file links don't resolve at runtime (each plugin installs to its own dir).

## Versioning
Two-phase: commit-SHA during active dev (omit `version`), explicit per-plugin semver + CHANGELOG once stable.
MCP-server binaries version independently via tags (see [[Homelab MCP servers]]).

## Related
- [[Homelab MCP servers]]
- [[Dockhand REST API]]
- [[Homelab conventions]]
- [[Redth homelab environment]]
