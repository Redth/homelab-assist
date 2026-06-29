---
name: dockge
description: Manage Docker compose stacks via Dockge — a file-based stack manager that stores stacks as compose files under /opt/stacks. Covers its file-on-disk model, the lack of a REST API/MCP (so operations are filesystem- or SSH-oriented), and how to edit stacks safely. Load this when the user manages Docker with Dockge.
---

# Dockge

Dockge (https://github.com/louislam/dockge) is a lightweight, **file-based** compose manager. It's popular in
homelabs but, unlike Dockhand/Portainer, exposes **no documented REST API and no MCP server** — its UI talks
over Socket.IO. Practical management therefore leans on the **filesystem** (and SSH). Obey
the `homelab-core:homelab-safety` skill and load
[`docker-compose-stacks`](../docker-compose-stacks/SKILL.md).

## The model

- Stacks live as real files: `${DOCKGE_STACKS_DIR}` (default **`/opt/stacks`**), one dir per stack containing
  `compose.yaml` + `.env`.
- Because stacks are plain files, they're friendly to Git/backup and to direct editing — but edits and deploys
  generally happen via the Dockge UI or on the host filesystem.
- Agent system (v1.4.0+) lets one Dockge manage multiple hosts, each with its own `/opt/stacks`.

## Working with it

Since there's no API/MCP:

- **Read:** you can read `compose.yaml`/`.env` under `/opt/stacks/<name>` and `docker compose ps/logs` to
  inspect — read-only, fine.
- **Change a stack:** editing the compose file on disk and (re)deploying is a **host filesystem operation** →
  treat as significant. If it requires SSH to the host, clear the SSH-confirmation gate
  (`homelab-core:homelab-safety`) and follow the SSH execution discipline (`homelab-core:homelab-ssh`):
  confirm the host, **back up the compose/`.env` before editing**, validate with `docker compose config`, then
  deploy via the Dockge UI or `docker compose up -d` in that stack dir. Keep the UI and disk in sync.
- **Prefer the UI** for deploy/stop when the user has it open, to avoid the UI showing stale state.

## When to steer toward Dockhand

The user prefers Dockhand. If a task needs reliable programmatic control (bulk changes, automation,
multi-host orchestration), note that Dockge's lack of an API makes it awkward and Dockhand/Portainer are
better suited — surface that trade-off rather than silently doing fragile filesystem surgery.

## Build opportunity

A Dockge MCP server doesn't exist (its Socket.IO interface makes it harder). If Dockge automation becomes
important, building one is a candidate under the repo's hybrid MCP strategy — note it.

## Safety checklist

Identify the stack dir and host → read current files first → for edits, confirm the SSH/filesystem change →
back up the compose/`.env` before editing → summarize + approve → deploy → verify health.
