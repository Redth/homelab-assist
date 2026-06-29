---
name: proxmox-overview
description: Entry point and router for managing Proxmox VE. Load this first for any Proxmox request — it establishes the version check, the API-first-vs-SSH decision, how to authenticate, and which specific Proxmox skill to use (VMs, LXC, storage/backups, updates, or host-level config like GPU passthrough).
---

# Proxmox VE — overview & routing

Start here for any Proxmox task. This skill sets up how to connect, forces a version check, and routes to the
right specific skill. It is **API-first** and obeys the homelab-core `homelab-core:homelab-safety` and
`homelab-core:homelab-research` policies.

## Step 1 — version check (always first)

Proxmox 8.x and 9.x differ in real, breaking ways (removed kernel modules, VNC proxy changes, IOMMU
enforcement). Before acting on anything non-obvious:

- `GET /version` via the API (`scripts/pve-api.sh GET /version`), or `pveversion -v` on the node.
- Scope all research to that version (see `homelab-research` + `reference/version-notes.md`).

## Step 2 — connect via the API (preferred)

Two ways, in order of preference:

1. **MCP server** (if the user configured one — see the plugin's `.mcp.json`, e.g. ProxmoxMCP-Plus). Use its
   tools directly.
2. **REST API** with an **API token** via [`scripts/pve-api.sh`](../../scripts/pve-api.sh). See
   [`proxmox-api`](../proxmox-api/SKILL.md) for auth setup, endpoints, and examples.

Use API tokens (`PVEAPIToken=user@realm!name=secret`), not passwords — stateless, no CSRF, revocable.

## Step 3 — decide API vs SSH

| Want to… | Use |
| --- | --- |
| Start/stop/create/delete/migrate VMs & LXC, snapshots | **API** → `proxmox-vms`, `proxmox-lxc` |
| List/inspect storage, run/restore backups (vzdump/PBS) | **API** → `proxmox-storage-backups` |
| Check/apply updates, repo status, node health | **API** → `proxmox-updates` |
| Cluster status, tasks, resource usage | **API** → `proxmox-api` |
| GPU/PCIe passthrough, `lxc.idmap`, hookscripts, kernel/VFIO, `/etc/network/interfaces` | **SSH** → `proxmox-host-config` (confirm SSH drop first) |

If the API can do it, **do not** use SSH. SSH is only for the host-level skill, and only after the user
approves the SSH drop per the safety policy.

## Step 4 — apply the safety gate

Any change (start/stop/create/delete/config edit) needs a batched plain-language summary and approval first.
Destructive operations (delete VM/CT, remove storage, restore-over-existing) must call out irreversibility
and data risk. See `homelab-safety`. Snapshot before risky changes when possible.

## Specific skills in this plugin

- [`proxmox-api`](../proxmox-api/SKILL.md) — auth (tokens), the `pve-api.sh` helper, common endpoints, tasks.
- [`proxmox-vms`](../proxmox-vms/SKILL.md) — QEMU/KVM VM lifecycle, config, snapshots, cloud-init.
- [`proxmox-lxc`](../proxmox-lxc/SKILL.md) — LXC container lifecycle, privileged vs unprivileged, mounts.
- [`proxmox-storage-backups`](../proxmox-storage-backups/SKILL.md) — storage types, vzdump, PBS, restore.
- [`proxmox-updates`](../proxmox-updates/SKILL.md) — package updates, repos, upgrade gotchas.
- [`proxmox-host-config`](../proxmox-host-config/SKILL.md) — SSH-level: config files, GPU passthrough, idmap.

## Reference

- [`reference/api-endpoints.md`](../../reference/api-endpoints.md) — map of common endpoints.
- [`reference/config-files.md`](../../reference/config-files.md) — where host config lives.
- [`reference/gpu-passthrough.md`](../../reference/gpu-passthrough.md) — passthrough deep-dive.
- [`reference/version-notes.md`](../../reference/version-notes.md) — 8.x vs 9.x gotchas.
