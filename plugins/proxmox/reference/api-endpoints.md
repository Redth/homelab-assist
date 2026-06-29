# Proxmox VE API endpoint map (reference)

Common endpoints under `https://<host>:8006/api2/json`. **Verify exact parameters against the API viewer for
the running version** — https://pve.proxmox.com/pve-docs/api-viewer/ or `https://<host>:8006/pve-docs/api-viewer/`.
This is a quick map, not a contract.

## Discovery / cluster

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/version` | PVE version (check first) |
| GET | `/nodes` | nodes in the cluster |
| GET | `/nodes/{node}/status` | CPU/mem/uptime/load for a node |
| GET | `/cluster/resources` | everything (qemu/lxc/storage/node) in one call |
| GET | `/cluster/status` | quorum / cluster health |
| GET | `/nodes/{node}/tasks` | recent tasks on a node |
| GET | `/nodes/{node}/tasks/{upid}/status` | poll an async task |
| GET | `/nodes/{node}/tasks/{upid}/log` | task log output |

## VMs (qemu)

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/nodes/{node}/qemu` | list VMs |
| GET | `/nodes/{node}/qemu/{vmid}/status/current` | power state |
| GET | `/nodes/{node}/qemu/{vmid}/config` | config |
| POST | `/nodes/{node}/qemu` | create |
| POST | `/nodes/{node}/qemu/{vmid}/clone` | clone |
| POST | `/nodes/{node}/qemu/{vmid}/status/{start\|shutdown\|stop\|reboot}` | lifecycle |
| POST | `/nodes/{node}/qemu/{vmid}/config` | set config params |
| PUT | `/nodes/{node}/qemu/{vmid}/resize` | grow a disk |
| GET/POST | `/nodes/{node}/qemu/{vmid}/snapshot` | list/create snapshots |
| POST | `/nodes/{node}/qemu/{vmid}/snapshot/{snap}/rollback` | rollback (destructive) |
| POST | `/nodes/{node}/qemu/{vmid}/migrate` | migrate |
| DELETE | `/nodes/{node}/qemu/{vmid}` | delete VM (destructive) |

## LXC

Same shape as qemu, swap `qemu` → `lxc` (e.g. `/nodes/{node}/lxc/{vmid}/status/start`,
`/nodes/{node}/lxc/{vmid}/config`, `.../snapshot`, clone, migrate, delete).

## Storage & backups

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/storage` | storage definitions (cluster) |
| GET | `/nodes/{node}/storage` | per-node status (used/avail) |
| GET | `/nodes/{node}/storage/{store}/content` | volumes/backups/ISO/templates (filter `content=backup`) |
| POST | `/nodes/{node}/vzdump` | run a backup |
| GET/POST | `/cluster/backup` | scheduled backup jobs |

## Updates

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/nodes/{node}/apt/update` | available updates |
| GET | `/nodes/{node}/apt/repositories` | configured repos / status |

## Access / tokens

| Method | Path | Purpose |
| --- | --- | --- |
| POST | `/access/ticket` | password/ticket auth (prefer tokens instead) |
| GET | `/access/users` | users |
| POST | `/access/users/{userid}/token/{tokenid}` | create API token |
| PUT | `/access/acl` | grant roles on a path |

## Auth header (token)

```
Authorization: PVEAPIToken=USER@REALM!TOKENNAME=SECRET-UUID
```

Tokens with `--privsep 1` only have the ACLs you explicitly grant. A 403 = missing ACL on that path.
