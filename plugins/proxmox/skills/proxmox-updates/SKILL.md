---
name: proxmox-updates
description: Check for and apply Proxmox VE package updates safely via the API, understand the enterprise vs no-subscription repositories, and navigate major-version upgrade gotchas (8.x to 9.x). Load this when the user wants to check for updates, patch, or upgrade Proxmox. Major upgrades are high-risk — research version-specific guidance first.
---

# Proxmox updates & upgrades

Patching a hypervisor can take down every VM/LXC on it. Be deliberate: check, summarize, confirm, and for
major upgrades, research the version-specific upgrade guide first.

## Check for updates (read-only)

```bash
scripts/pve-api.sh GET /nodes/{node}/apt/update            # list available package updates
scripts/pve-api.sh GET /version                            # current PVE version
```

`pveversion -v` on the node gives the verbose component breakdown.

## Repositories — know which is configured

- **Enterprise repo** (`pve-enterprise`) requires a valid subscription; without one its updates 401.
- **No-subscription repo** (`pve-no-subscription`) is the common homelab choice — stable enough, no support.
- **Test repo** — not for production.

Mixing repos or leaving the enterprise repo enabled without a subscription is a frequent homelab gotcha.
Inspect what's configured (`/etc/apt/sources.list.d/` on the host — read-only over SSH if needed, confirm the
SSH drop). The API exposes repository status under `/nodes/{node}/apt/repositories`.

```bash
scripts/pve-api.sh GET /nodes/{node}/apt/repositories
```

## Apply updates (significant change — confirm + plan)

Applying updates is a significant change. Summarize first: which node, how many packages, whether a **kernel
update** is included (implies a reboot and downtime for guests), and the maintenance window.

- The cleanest path is often `apt update && apt dist-upgrade` on the node (host-level → confirm SSH drop), or
  the UI's update button. The API's apt endpoints are mainly for **listing**; actually applying typically
  happens on the host.
- **Migrate or shut down guests** before rebooting a node. In a cluster, drain/migrate VMs to another node.
- Reboot only after confirming a kernel/microcode change requires it.

## Major upgrades (e.g. 8.x → 9.x) — high risk

**Always** read the official version-specific upgrade guide and known-issues page first
(`homelab-research` → `reference/version-notes.md` → e.g. https://pve.proxmox.com/wiki/Upgrade_from_8_to_9).
Known cross-version gotchas:

- Removed/renamed kernel modules (e.g. `vfio_virqfd` no longer exists on 9.x — remove from `/etc/modules`).
- IOMMU reserved-memory enforcement on newer kernels can break GPU passthrough on some boards.
- VNC proxy endpoint changes can break external VNC clients.
- Run `pve8to9` (or the relevant checker) and resolve every warning **before** upgrading.

Pre-upgrade checklist to summarize for the user:

1. Full backups of all guests (PBS preferred) and confirm they're restorable.
2. Snapshot/note current versions; read the upgrade guide for the exact source→target versions.
3. Run the upgrade checker; resolve warnings.
4. Plan a maintenance window; migrate guests off the node if clustered.
5. Reboot, verify all guests and passthrough come back, then proceed to the next node.

Get explicit approval before starting — this is among the highest-risk operations in the suite.
