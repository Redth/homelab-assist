# Dockhand API & MCP (reference)

**Verify against the live instance / current docs before relying on specifics** — as of early 2026 Dockhand
had 130+ REST endpoints but no published OpenAPI spec yet (it was in progress). The MCP server is the most
stable programmatic surface. Sources: https://github.com/Finsys/dockhand · https://dockhand.pro ·
https://finsys-dockhand.mintlify.app/ · MCP: https://github.com/strausmann/mcp-dockhand

## Architecture recap

- **Server**: central control plane, stores stack definitions (SQLite or Postgres via Drizzle).
- **Environment**: a managed Docker host; most operations need an `environmentId`.
- **Hawser**: Go agent on remote hosts. Modes:
  - **Standard**: agent listens (default port **2376**); Dockhand connects in. Token auth off-loopback.
  - **Edge**: agent dials out over **WSS** to Dockhand (NAT/dynamic-IP friendly). Outbound-only.
  - Health endpoint: `/_hawser/health`. Edge reports metrics ~30s.
  - Tokens are **root-equivalent** — secret material.

## Auth

- Dockhand: token-based (Argon2id) + optional OIDC/SSO; local accounts; TOTP 2FA. Sensitive creds encrypted
  at rest (AES-256-GCM).
- MCP server (`strausmann/mcp-dockhand`): `DOCKHAND_URL`, `DOCKHAND_USERNAME`, `DOCKHAND_PASSWORD`; session
  auth with auto re-login on 401; SSE for deploy streaming. Image: `ghcr.io/strausmann/mcp-dockhand:latest`.

## Endpoint families (names approximate — confirm live)

- Environments / agents (list, status, register)
- Stacks (list, get, create/update compose, deploy, stop, remove; Git sync)
- Containers (list, inspect, start/stop/restart, logs, exec)
- Images (list, pull, prune, vulnerability scan)
- Networks, Volumes (list, create, remove)
- System / settings

Most resource calls are **scoped to an `environmentId`** — always specify the intended host.

## Git-synced stacks

Dockhand can sync stacks from Git with webhooks (GitHub/GitLab). For Git-managed stacks, change the compose
**in Git** and let Dockhand sync, rather than editing via API/UI (which causes drift/conflicts). Confirm a
stack's management model before editing.

## Practical guidance

- Prefer the **MCP server** when configured; fall back to REST against the live instance, validating shapes
  first (`homelab-research`).
- Don't hand-edit compose files on the Hawser host — Dockhand owns the definitions centrally; host edits drift.
- If a needed operation isn't exposed, note it as a **build candidate** (custom MCP tool) per the repo's hybrid
  MCP strategy, instead of dropping to SSH.
