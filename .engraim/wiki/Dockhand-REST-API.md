# Dockhand REST API

Reverse-engineered (session 17191c3f) from `strausmann/mcp-dockhand` (Node) and `Finsys/dockhand` (SvelteKit)
source — Dockhand has **no published OpenAPI spec**. Base path is **`/api`** (no version prefix). Powers
[[Homelab MCP servers|src/DockhandMcp]]. Verify against a live instance before trusting response DTO shapes.

## Auth (three modes)
- **Bearer PAT (preferred)**: `Authorization: Bearer dh_<token>`. Tokens are `dh_` + base64url; issue via UI or
  `POST /api/auth/tokens` (needs a cookie session to mint — a `dh_` token cannot mint tokens). No login
  round-trip, no re-login. → set `DOCKHAND_TOKEN`.
- **Cookie login**: `POST /api/auth/login` body `{username,password,provider:"local"}` → Set-Cookie session
  (~24h). Re-auth on any 401 and retry once. Optional `mfaToken`; `{requiresMfa:true}` if MFA needed.
- **Auth disabled**: `POST /api/auth/login` returns `400 "Authentication is not enabled"` → proceed with no auth.

## Scoping — critical
Nearly every resource call takes **`?env=<id>`** (query param, numeric environment id from
`GET /api/environments`). Omitting it makes list endpoints return `[]` (not an error). **Exception:**
`POST /api/containers/{id}/exec` uses `?envId=` instead of `?env=`.

## Envelope & SSE
- No global envelope: GETs return **raw arrays/objects**. Errors: `{ "error": "..." }` with proper HTTP status.
  Logs → `{ logs }`; compose → `{ content, ... }`. Many POSTs return `{success:true}` or empty.
- **SSE**: deploy/start/stop/down/restart/create-with-start (and batch-update) stream `text/event-stream`:
  `event: progress` lines then a terminal `event: result` with `{success,output?,error?}`. Client must set
  `Accept: text/event-stream`, read to completion, and return the last `data:` (the result). Note some of these
  return plain JSON depending on flags (e.g. `PUT .../compose` only streams when `restart:true`) — check
  Content-Type.

## Key endpoints
- Environments: `GET /api/environments`, `GET|PUT|DELETE /api/environments/{id}`, `POST .../{id}/test`.
- Stacks (by **name**): `GET /api/stacks?env=`, `GET .../{name}/compose`, `PUT .../{name}/compose {content,restart}`,
  `POST /api/stacks {name,compose,start}`, `POST .../{name}/deploy|start|stop|restart`,
  `POST .../{name}/down {removeVolumes}`, `DELETE .../{name}?env=&force=&volumes=`.
- Containers (by **id**): `GET /api/containers?env=&all=`, `.../{id}/inspect`, `.../{id}/logs?tail=` (→`{logs}`),
  `POST .../{id}/{start|stop|restart}`, `DELETE .../{id}?force=`, `POST .../{id}/exec?envId=`.
- Images: `GET /api/images?env=`, `POST /api/images/pull`. System: `GET /api/system?env=`, `/api/host`, `/api/health`.
- URL-encode stack name / container id path segments. No CSRF header needed.

## Related
- [[Homelab MCP servers]]
- [[homelab-assist]]
