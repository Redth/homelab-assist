---
name: proxmox-api
description: How to authenticate to and call the Proxmox VE REST API — API token setup, the pve-api.sh helper, common endpoints, and how to poll background tasks. Load this when you need to make Proxmox API calls directly (any VM/LXC/storage/backup/cluster operation that goes through the API rather than an MCP server).
---

# Proxmox VE REST API

The Proxmox API is at `https://<host>:8006/api2/json/...` and mirrors everything `pvesh` / the UI can do for
lifecycle, storage, backups, cluster, and node management. Use it as the default for Proxmox management.

## Authentication — use API tokens

Prefer **API tokens** over password/ticket auth: stateless, no CSRF token needed, individually revocable,
support privilege separation and expiry.

Create one (UI: *Datacenter > Permissions > API Tokens*, or CLI on the node):

```
pveum user token add automation@pve mytoken --privsep 1
# grant the token a role on a path, e.g.:
pveum acl modify / --tokens 'automation@pve!mytoken' --roles PVEVMAdmin
```

Header format:

```
Authorization: PVEAPIToken=USER@REALM!TOKENNAME=SECRET-UUID
```

> Privilege separation (`--privsep 1`) means the token gets **only** the ACLs you grant it, independent of the
> user. Grant the least privilege the task needs.

## The helper script

[`scripts/pve-api.sh`](../../scripts/pve-api.sh) wraps token auth + curl and **defaults to GET**. Configure
via env (never hard-code): `PVE_HOST`, `PVE_PORT`, `PVE_TOKEN_ID`, `PVE_TOKEN_SECRET`, `PVE_VERIFY_SSL`.

Store the token secret in the OS secret store (see `homelab-core:homelab-secrets`), not a dotfile, and inject
it per-command — e.g. `homelab-secret run PVE_TOKEN_SECRET -- scripts/pve-api.sh GET /version`. Never print the
token value.

```bash
scripts/pve-api.sh GET /version
scripts/pve-api.sh GET /nodes
scripts/pve-api.sh GET /nodes/pve1/qemu
scripts/pve-api.sh GET /cluster/resources | jq '.data[] | {id,type,status}'
```

Writes (POST/PUT/DELETE) must be **approved first** per `homelab-safety`. Only then:

```bash
scripts/pve-api.sh POST /nodes/pve1/qemu/100/status/start
```

## Common endpoints (see reference for the full map)

| Purpose | Method + path |
| --- | --- |
| Version | `GET /version` |
| Nodes | `GET /nodes` ; node status `GET /nodes/{node}/status` |
| All resources (VMs/CTs/storage) | `GET /cluster/resources` |
| List VMs | `GET /nodes/{node}/qemu` |
| VM status / config | `GET /nodes/{node}/qemu/{vmid}/status/current` ; `.../config` |
| List LXC | `GET /nodes/{node}/lxc` |
| Storage | `GET /nodes/{node}/storage` ; content `.../storage/{store}/content` |
| Tasks | `GET /nodes/{node}/tasks` ; status `.../tasks/{upid}/status` |

Full list: [`reference/api-endpoints.md`](../../reference/api-endpoints.md).

## Background tasks (UPID)

Many write operations return a **task ID (UPID)** and run asynchronously. Don't assume success from the POST
response — poll the task:

```bash
# the POST returns {"data":"UPID:pve1:..."} ; poll it:
scripts/pve-api.sh GET /nodes/pve1/tasks/UPID:pve1:.../status | jq '.data.status,.data.exitstatus'
```

`status: stopped` + `exitstatus: OK` means success; anything else, read the task log
(`.../tasks/{upid}/log`) and report it.

## Gotchas

- **TLS:** homelab nodes often use self-signed certs → set `PVE_VERIFY_SSL=0` knowingly, or trust the CA.
- **Node-scoped paths:** most operations require the correct `{node}` — discover it from `/cluster/resources`.
- **Version drift:** field/endpoint shapes can change between 8.x and 9.x — verify against the API viewer for
  the running version (`homelab-research`).
- **Permissions:** a 403 usually means the token lacks an ACL on that path — grant the minimal role needed.
