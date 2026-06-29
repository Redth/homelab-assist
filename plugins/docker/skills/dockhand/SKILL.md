---
name: dockhand
description: Manage Docker via Dockhand — the preferred control plane in this homelab — and its Hawser remote agents. Covers the Dockhand architecture (central server + Hawser Standard/Edge agents), how to drive it (the mcp-dockhand MCP server or its REST API), compose-stack and container operations, multi-host/environment targeting, and Git-synced stacks. Load this when the user manages Docker with Dockhand.
---

# Dockhand (+ Hawser)

Dockhand (https://github.com/Finsys/dockhand, https://dockhand.pro) is the **preferred** Docker control plane
here. It's a central server that manages many Docker hosts, storing stack definitions centrally and pushing
to hosts directly or via **Hawser** agents. Obey
the `homelab-core:homelab-safety` skill and load
[`docker-compose-stacks`](../docker-compose-stacks/SKILL.md) for the compose fundamentals.

## Architecture (so you target the right host)

- **Dockhand server** — central control plane; stores all stack definitions (SQLite/Postgres). Remote hosts
  hold only data/volumes.
- **Environments** — each managed Docker host is an "environment"; most API calls need an `environmentId` to
  say *which host*. Always confirm you're acting on the intended environment.
- **Hawser** — lightweight Go agent on remote hosts (https://github.com/Finsys/hawser), in one of two modes:
  - **Standard** — agent listens on a port (default 2376); Dockhand connects in. LAN/static homelab. Token
    auth required off-loopback.
  - **Edge** — agent dials **out** to Dockhand over WSS; good for NAT/dynamic IP/VPS. Outbound-only.
  - Health: `/_hawser/health`. Edge sends metrics ~every 30s. **Hawser tokens are root-equivalent** — treat
    as secrets.

When the user has "several Hawser instances managed by one Dockhand," each Hawser host is a separate
environment — pick the correct one for every operation and say which one in your plan.

## How to drive Dockhand

In order of preference:

1. **MCP server** — [`strausmann/mcp-dockhand`](https://github.com/strausmann/mcp-dockhand) (`ghcr.io/strausmann/mcp-dockhand`)
   exposes ~130 tools (containers, stacks, images, networks, volumes, Git, vuln scanning, system admin). If
   configured (see the plugin `.mcp.json`), use its tools — it's the most reliable surface. Auth via
   `DOCKHAND_URL` / `DOCKHAND_USERNAME` / `DOCKHAND_PASSWORD`; it re-logs in on 401.
2. **Dockhand REST API** — 130+ endpoints (containers, stacks, images, networks, volumes, environments,
   agents, settings), most requiring `environmentId`. Token-based auth (Argon2id) + optional OIDC/SSO. As of
   early 2026 a formal OpenAPI spec was still in progress — **verify current API shape against the live
   instance / docs** (https://finsys-dockhand.mintlify.app/) before relying on specific endpoints
   (`homelab-research`). See [`reference/dockhand-api.md`](../../reference/dockhand-api.md).

## Common tasks

- **Inspect (read):** list environments/agents, list stacks on an environment, container status, logs,
  image/vuln scan results.
- **Stack ops (significant):** create/update a compose stack, deploy/redeploy, stop, pull images. Stacks are
  defined centrally — edit in Dockhand, which pushes to the host (don't hand-edit files on the host and create
  drift).
- **Git-synced stacks:** Dockhand can auto-sync stacks from Git with webhooks. If a stack is Git-managed,
  prefer changing it **in Git** (PR/commit) and letting Dockhand sync, rather than editing in the UI/API —
  otherwise you fight the sync. Confirm which model a given stack uses.
- **Destructive:** removing a stack, `down -v`-equivalent (deletes volumes), deleting an environment/agent —
  summarize blast radius + data risk, confirm.

## Targeting & safety checklist

1. Confirm the **environment** (which Hawser host) — name it in your plan.
2. Confirm whether the stack is **Git-synced** (change in Git) or UI/API-managed.
3. For updates, note image tag behavior (`:latest` vs pinned) and which containers recreate.
4. Summarize plan + downtime + rollback, get approval, then act. Verify health after (don't assume success).

## Gaps / build opportunities

If the REST API proves insufficient or the MCP server lacks a needed tool, this is a candidate for a custom
MCP tool (per the repo's hybrid MCP strategy) — note it rather than dropping to SSH on the host, which fights
Dockhand's central-definition model.
