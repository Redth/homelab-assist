---
name: docker-overview
description: Entry point and router for managing Docker in a homelab. Load this first for any Docker request — it identifies which control plane is in use (Dockhand preferred, or Portainer/Dockge/raw Engine), establishes the version check, and routes to the right skill (compose stacks, Dockhand, reverse-proxy label routing, or Docker-on-Proxmox networking).
---

# Docker (homelab) — overview & routing

Start here for any Docker task. This suite is **control-plane-first** (manage through Dockhand/Portainer APIs
rather than raw SSH) and obeys the homelab-core `homelab-core:homelab-safety` and
`homelab-core:homelab-research` policies.

## Step 1 — which control plane?

Ask or detect what the user manages Docker with — the workflow differs:

| Tool | Use the skill | Notes |
| --- | --- | --- |
| **Dockhand** (+ Hawser agents) | [`dockhand`](../dockhand/SKILL.md) | **Preferred.** Central server manages many hosts via Hawser agents. Has an MCP server. |
| **Portainer** | [`portainer`](../portainer/SKILL.md) | Mature REST API + official MCP. |
| **Dockge** | [`dockge`](../dockge/SKILL.md) | File-based stacks in `/opt/stacks`; no REST API/MCP — often filesystem/SSH. |
| **Raw Docker / compose CLI** | [`docker-compose-stacks`](../docker-compose-stacks/SKILL.md) | The shared foundation under all of the above. |

If unsure, default to the user's stated preference (Dockhand) and confirm. The **compose knowledge** in
[`docker-compose-stacks`](../docker-compose-stacks/SKILL.md) applies no matter which control plane you use —
load it alongside the tool-specific skill.

## Step 2 — version check

Check versions before acting on anything non-obvious: `docker version`, `docker compose version`, and the
control plane's own version (Dockhand/Portainer/Dockge UI or API). MCP/API clients are often version-matched
(e.g. Portainer MCP `~=2.42` ↔ Portainer 2.42.x). Prefer Docker's `llms.txt`
(https://docs.docker.com/llms.txt) when researching (`homelab-research`).

## Step 3 — recognize the homelab patterns

This suite encodes specific patterns the user relies on:

- **Reverse-proxy routing via labels** → [`npm-reverse-proxy`](../npm-reverse-proxy/SKILL.md): container
  labels (via `Redth/npm-docker-sync`) auto-create nginx-proxy-manager proxy hosts for domain routing.
- **Docker host inside Proxmox** → [`docker-on-proxmox`](../docker-on-proxmox/SKILL.md): the Docker host is
  often a Proxmox VM or (un)privileged LXC, with VLAN-tagged networking and passed-through storage. Networking
  or permission problems frequently originate at the Proxmox layer, not Docker.

## Step 4 — safety gate

Reads (list stacks/containers/logs, show compose) are free. Any deploy/update/restart/down, image pull,
volume change, or stack delete is a significant/destructive change → summarize the plan and get approval
(`homelab-safety`). Avoid SSH where a control-plane API can do the job; if SSH is needed (e.g. Dockge stack
files, host networking), confirm the SSH drop first.

## Skills in this plugin

- [`docker-compose-stacks`](../docker-compose-stacks/SKILL.md) — shared compose knowledge (the base skill).
- [`dockhand`](../dockhand/SKILL.md) — Dockhand + Hawser (primary).
- [`portainer`](../portainer/SKILL.md) — Portainer REST API + MCP.
- [`dockge`](../dockge/SKILL.md) — Dockge file-based stacks.
- [`npm-reverse-proxy`](../npm-reverse-proxy/SKILL.md) — NPM label routing / npm-docker-sync.
- [`docker-on-proxmox`](../docker-on-proxmox/SKILL.md) — VLAN, storage, LXC-vs-VM integration.
