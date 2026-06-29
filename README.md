# homelab-assist

A growing **Claude Code plugin marketplace** for managing homelab infrastructure with an AI session —
**API-first, SSH-as-last-resort**, with built-in safety confirmation and version-correct research habits.

## Plugins

| Plugin | What it does |
| --- | --- |
| **homelab-core** | Shared conventions: change-approval safety policy, SSH-confirmation gate, and how to research version-correct canonical docs. Install this alongside the others. |
| **proxmox** | Manage Proxmox VE via the REST API (VMs, LXC, storage, backups, updates, cluster). Drops to host-level config edits only when the API can't do it (GPU passthrough, `lxc.idmap`, hookscripts, networking). |
| **docker** | Manage Docker compose stacks. Dockhand-first (+ Hawser agents), plus Portainer and Dockge, shared compose knowledge, nginx-proxy-manager label routing, and Docker-on-Proxmox networking/storage patterns. |

## Install

In Claude Code:

```
/plugin marketplace add Redth/homelab-assist
/plugin install homelab-core@homelab-assist
/plugin install proxmox@homelab-assist
/plugin install docker@homelab-assist
```

For local development against this checkout:

```
/plugin marketplace add ./
/plugin install proxmox@homelab-assist
```

Skills become available namespaced, e.g. `/proxmox:proxmox-overview`, `/docker:dockhand`.

## Design principles

1. **Prefer APIs over SSH.** Proxmox REST API and Docker control-plane APIs (Dockhand/Portainer) are the
   default. SSH / host file edits are a documented fallback for things APIs genuinely can't do.
2. **Confirm before SSH, and before significant changes.** Read-only operations run freely. Any
   create/delete/restart/config-write — or any drop to SSH — first gets a **plain-language summary** of what
   will happen, for you to approve, refine, or reject. See [`homelab-core` safety policy](plugins/homelab-core/reference/safety-policy.md).
3. **Research version-correct info.** Homelab software changes fast and search results go stale. Skills check
   the running version first (`pveversion`, Dockhand/Portainer/Docker versions) and prefer canonical,
   AI-optimized (`llms.txt`) sources. See [doc-sources](plugins/homelab-core/reference/doc-sources.md).

## Credentials

Secrets (API tokens, passwords, SSH key passphrases) live in your **OS-native secret store** — macOS Keychain,
Windows Credential Vault, or Linux libsecret/`pass` — never in dotfiles, the repo, or command lines. The
`homelab-core` plugin ships a [`homelab-secret`](plugins/homelab-core/scripts/homelab-secret) helper
(`set`/`get`/`run`/`has`/`delete`) and the [`homelab-secrets`](plugins/homelab-core/skills/homelab-secrets/SKILL.md)
skill. Store once (`homelab-secret set NPM_PASSWORD`) and let launchers/scripts resolve it at runtime; the
`npm` MCP server's launcher auto-resolves missing `NPM_*` from the store when the helper is on your `PATH`.

## MCP servers (optional)

Each infra plugin ships a `.mcp.json` that **references mature community MCP servers** (opt-in). They are not
required — skills work through documented REST calls and helper scripts even with no MCP server configured.
Configure credentials via environment variables / plugin user config (never commit secrets). See each
plugin's README section and [`docs/DESIGN.md`](docs/DESIGN.md).

## Contributing & versioning

This is a living repo. See [`CONTRIBUTING.md`](CONTRIBUTING.md) and [`docs/VERSIONING.md`](docs/VERSIONING.md).
Short version: during active development we use **commit-SHA versioning** (every push auto-updates installs);
plugins move to **explicit semver + CHANGELOGs** as they stabilize.

## License

[MIT](LICENSE)
