---
name: portainer
description: Manage Docker (and Kubernetes) via Portainer's REST API or the official portainer-mcp server — environments/endpoints, stacks, containers/images/networks/volumes, and the Docker API proxy. Load this when the user manages Docker with Portainer rather than Dockhand.
---

# Portainer

Portainer (https://github.com/portainer/portainer) is a mature, well-documented control plane — a good
secondary to Dockhand. Obey the `homelab-core:homelab-safety` skill and load
[`docker-compose-stacks`](../docker-compose-stacks/SKILL.md) for compose fundamentals.

## How to drive it

1. **Official MCP** — [`portainer/portainer-mcp`](https://github.com/portainer/portainer-mcp), generated from
   Portainer's OpenAPI. **Match the version** to your Portainer (e.g. `mcp-portainer~=2.42` ↔ Portainer
   2.42.x). Auth via `PORTAINER_URL` + `PORTAINER_API_KEY`. Supports a read-only mode — prefer it for
   inspection. If configured, use it (see plugin `.mcp.json`).
2. **REST API** — full, version-aware, documented at https://docs.portainer.io/api/docs and
   https://api-docs.portainer.io/. Auth via `X-API-Key: <token>` header. It can also **proxy** straight to the
   underlying Docker/K8s API.

## Concepts

- **Environments / endpoints** — each managed Docker (or K8s) host. Operations are scoped to an endpoint ID
  (analogous to Dockhand's `environmentId`). Confirm which one.
- **Edge agents** — for remote/NAT hosts (analogous to Hawser Edge).
- **Stacks** — compose stacks managed by Portainer; can be Git-backed (GitOps) — if so, prefer changing in
  Git and letting Portainer redeploy.

## Common tasks

- **Read:** list endpoints, stacks, containers, images, volumes; logs; version/settings.
- **Significant:** create/update/redeploy a stack, pull images, start/stop/restart containers.
- **Destructive:** delete a stack/volume/endpoint, prune — summarize data risk + blast radius, confirm.

## Version-matching gotcha

Portainer's API and MCP are version-sensitive. Check the running Portainer version first and use the matching
MCP/client version, and verify endpoint shapes against the api-docs for that version (`homelab-research`).
Using a mismatched client version is a common source of confusing failures.

## Safety checklist

Confirm endpoint → confirm Git-managed vs not → note image tag/recreate behavior → summarize + approve →
verify health after. Prefer the API/MCP over SSH to the host.
