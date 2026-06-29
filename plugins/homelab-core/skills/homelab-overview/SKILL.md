---
name: homelab-overview
description: Orientation for the homelab-assist plugin suite — what plugins and skills exist (Proxmox, Docker), how they relate, and how to route a homelab request to the right skill. Load this when the user makes a broad homelab request, when you're unsure which homelab skill applies, or when a task spans both Proxmox and Docker.
---

# Homelab-assist: orientation & routing

This suite helps you manage a homelab through Claude Code. It is **API-first** and **safety-gated**. Start
here when a request is broad or spans layers.

## The plugins

- **homelab-core** (this plugin) — the conventions every other skill defers to:
  - [`homelab-safety`](../homelab-safety/SKILL.md) — change-approval + SSH-confirmation policy. Apply before
    any change or SSH drop.
  - [`homelab-ssh`](../homelab-ssh/SKILL.md) — the execution discipline for any work done over SSH (host
    identity, backup/rollback, validate-before-apply, dangerous-command guardrails, lockout avoidance). Load
    it whenever a task will run shell commands or edit files on a host.
  - [`homelab-secrets`](../homelab-secrets/SKILL.md) — store/retrieve credentials in the OS-native secret
    store (Keychain / Credential Vault / libsecret / `pass`) and handle SSH keys. Load it whenever a task
    needs a token, password, or SSH credential.
  - [`homelab-research`](../homelab-research/SKILL.md) — find version-correct canonical docs before acting on
    anything non-obvious.
- **proxmox** — manage Proxmox VE. Start at `proxmox:proxmox-overview`, which routes to VM/LXC/storage/
  backup/update skills (REST API) and to `proxmox-host-config` (SSH-level: GPU passthrough, idmap, network).
- **docker** — manage Docker compose stacks. Start at `docker:docker-overview`, which routes to Dockhand
  (primary), Portainer, Dockge, the shared compose skill, NPM reverse-proxy label routing, and the
  Docker-on-Proxmox networking/storage skill.

## Routing a request

1. **Always** load the safety policy before changing anything, and research the version before acting on
   anything you're unsure about.
2. Identify the layer:
   - Hypervisor / VM / LXC / passthrough / cluster / PVE backups → **proxmox** plugin.
   - Containers / compose stacks / reverse proxy / container networking → **docker** plugin.
   - Spanning both (e.g. "my Docker LXC can't see the NFS share" or "set up a VLAN for my Docker host") →
     start with `docker:docker-on-proxmox`, which bridges the two.
3. Drill into the specific task skill from the plugin's overview.

## Cross-layer awareness

The homelab is a stack: a Docker host is often a **Proxmox VM or LXC** (privileged or unprivileged), with
**VLAN-tagged** networking and **passed-through storage**. Problems frequently cross the boundary — a
container that "can't reach the network" may actually be a Proxmox bridge/VLAN issue; a "permission denied"
on a bind mount may be an unprivileged-LXC `idmap` issue. When debugging, consider both layers and use
`docker:docker-on-proxmox` to reason about how they interact.

## The two rules that never bend

- **Confirm before significant/destructive changes and before any SSH drop** (`homelab-safety`).
- **Be version-correct** — check the running version and canonical docs before acting (`homelab-research`).
