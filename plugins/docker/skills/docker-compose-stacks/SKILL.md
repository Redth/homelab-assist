---
name: docker-compose-stacks
description: Shared foundational knowledge for working with Docker Compose stacks — the compose file format, services/networks/volumes/secrets, environment and .env handling, healthchecks, common operations (up/down/pull/logs/ps), and safe-change practices. Load this for any compose-based work, alongside the control-plane skill (Dockhand/Portainer/Dockge) that actually applies the changes.
---

# Docker Compose stacks (foundation)

This is the base skill that underlies every Docker control plane in this suite — Dockhand, Portainer, and
Dockge all ultimately manage **compose stacks**. Load it together with the tool-specific skill. Obey
the `homelab-core:homelab-safety` skill.

## The compose file

Modern `compose.yaml` (Compose Spec — the merged successor to legacy v2/v3). Canonical reference:
https://docs.docker.com/reference/compose-file/ (and Docker's `llms.txt`). Use `docker compose` (v2, a
plugin) not the legacy `docker-compose` binary.

Key sections:

- `services:` — each container: `image`, `environment`/`env_file`, `ports`, `volumes`, `networks`,
  `depends_on`, `restart`, `healthcheck`, `labels`, `deploy`.
- `networks:` — user-defined networks (bridge, or `macvlan`/`ipvlan` for VLAN setups — see
  [`docker-on-proxmox`](../docker-on-proxmox/SKILL.md)).
- `volumes:` — named volumes vs bind mounts (`./data:/data`). Bind mounts on a Proxmox LXC host can hit
  permission/idmap issues — see `docker-on-proxmox`.
- `secrets:`/`configs:` — prefer over baking secrets into env where supported.
- `labels:` — drive integrations like the NPM reverse-proxy sync (see
  [`npm-reverse-proxy`](../npm-reverse-proxy/SKILL.md)).

## Common operations

| Intent | Command (raw CLI) | Class |
| --- | --- | --- |
| List stacks/containers | `docker compose ls`, `docker compose ps` | read |
| Show config (resolved) | `docker compose config` | read |
| Logs | `docker compose logs -f <svc>` | read |
| Pull images | `docker compose pull` | significant |
| Deploy/update | `docker compose up -d` | significant |
| Recreate one service | `docker compose up -d --force-recreate <svc>` | significant |
| Stop | `docker compose stop` / `down` (down removes containers+networks) | significant |
| Remove + volumes | `docker compose down -v` | **destructive** (deletes named volumes/data) |

Through a control plane you'll usually trigger the equivalent action via its API/UI rather than the CLI — the
class (read/significant/destructive) is the same and the safety gate still applies.

## `.env` and environment handling

- Compose interpolates `${VAR}` from a sibling `.env` and the shell environment.
- **Never commit real secrets.** Keep `.env` out of version control; reference secrets via env/secret stores.
- When editing a stack's env, show the diff and note which services restart.

## Healthchecks & dependencies

- Add `healthcheck:` so `depends_on: condition: service_healthy` works and so the control plane shows real
  health, not just "running".
- `restart: unless-stopped` is the common homelab default.

## Safe-change practices

- **Pin or pull deliberately.** `:latest` updates silently on `pull` + `up`; for important services pin a
  tag/digest so updates are intentional.
- **Show the diff** of any compose/env change and which services it recreates before applying.
- **`down -v` deletes data** — treat as destructive, name the volumes at risk, confirm explicitly.
- After a deploy, verify: `ps` shows healthy, logs are clean, and the service responds — don't assume success
  from the deploy command exiting 0.

## Where the file actually lives

Depends on the control plane:
- **Dockhand** stores stack definitions centrally (DB) and pushes to hosts/agents → edit via Dockhand.
- **Dockge** stores them as files in `/opt/stacks/<name>/compose.yaml` → edit file (often filesystem/SSH).
- **Portainer** stores stack definitions (and can pull from Git) → edit via Portainer.
Know which, so you edit in the right place and don't create drift between the control plane and disk.
