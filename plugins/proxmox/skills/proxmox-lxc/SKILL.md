---
name: proxmox-lxc
description: Manage Proxmox LXC containers via the REST API — list, inspect, start/stop, create, clone, migrate, snapshot, and basic config. Covers the privileged-vs-unprivileged distinction and where simple mount points end and host-file edits begin. Load this for LXC/container tasks on Proxmox. For lxc.idmap, bind-mount UID remapping, nesting/keyctl tweaks, or hookscripts, use proxmox-host-config.
---

# Proxmox LXC containers

Manage LXC (system containers) through the REST API (see [`proxmox-api`](../proxmox-api/SKILL.md)). Obey
the `homelab-core:homelab-safety` skill. Note: a "Docker host LXC" is a
common homelab pattern — for how Docker interacts with LXC privilege/storage/VLAN, see
`docker:docker-on-proxmox`.

## Privileged vs unprivileged — know which you have

- **Unprivileged (default, recommended):** container root (uid 0) is mapped to an unprivileged host uid
  (typically `100000+`). Safer, but **bind mounts and device access need UID/GID remapping** (`lxc.idmap`) —
  a host-file edit, not an API call → `proxmox-host-config`.
- **Privileged:** container root == host root. Simpler mounts/devices, but a container escape = host
  compromise. Avoid unless a workload truly needs it (and understand the risk).

Check: `GET /nodes/{node}/lxc/{vmid}/config` → look for `unprivileged: 1`.

## Inspect (read-only)

```bash
scripts/pve-api.sh GET /nodes/{node}/lxc                              # list containers
scripts/pve-api.sh GET /nodes/{node}/lxc/{vmid}/status/current        # state
scripts/pve-api.sh GET /nodes/{node}/lxc/{vmid}/config                # config incl. mp0..mpN, net, features
```

## Lifecycle (write — confirm first)

```bash
scripts/pve-api.sh POST /nodes/{node}/lxc/{vmid}/status/start
scripts/pve-api.sh POST /nodes/{node}/lxc/{vmid}/status/shutdown
scripts/pve-api.sh POST /nodes/{node}/lxc/{vmid}/status/stop
scripts/pve-api.sh POST /nodes/{node}/lxc/{vmid}/status/reboot
```

## Snapshots

```bash
scripts/pve-api.sh POST /nodes/{node}/lxc/{vmid}/snapshot --data 'snapname=pre-change'
scripts/pve-api.sh POST /nodes/{node}/lxc/{vmid}/snapshot/{snap}/rollback   # destructive
```

## Create / clone

```bash
scripts/pve-api.sh POST /nodes/{node}/lxc --data 'vmid=210&hostname=svc&ostemplate=local:vztmpl/debian-12-standard_amd64.tar.zst&memory=2048&cores=2&rootfs=local-lvm:8&net0=name=eth0,bridge=vmbr0,ip=dhcp&unprivileged=1'
scripts/pve-api.sh POST /nodes/{node}/lxc/{vmid}/clone --data 'newid=211&hostname=svc2'
```

Verify template names (`GET /nodes/{node}/storage/{store}/content`) and field shapes against the API viewer
for the running version.

## Mount points — where API ends and host edits begin

- **Simple mount points** can be set via config: `mp0=local-lvm:8,mp=/data` (a new volume) or a bind mount
  `mp0=/host/path,mp=/data`. The API can set these (`POST .../config`).
- **But** on an **unprivileged** container, a host bind mount usually has the wrong ownership inside the
  container unless you set up `lxc.idmap` UID/GID remapping — that lives in `/etc/pve/lxc/{vmid}.conf` and is
  a **host-file edit**. Likewise `features` like `nesting=1`, `keyctl=1`, `fuse=1` (needed for Docker-in-LXC)
  and any `lxc.*` raw keys or hookscripts. → use [`proxmox-host-config`](../proxmox-host-config/SKILL.md) and
  confirm the SSH drop.

## Network / VLAN

Basic NIC config (`net0=name=eth0,bridge=vmbr0,tag=<vlan>,ip=dhcp`) is settable via the API. The underlying
**VLAN-aware bridge** and `/etc/network/interfaces` are host-level (`proxmox-host-config`). For the full
Docker-host-in-LXC VLAN/storage picture, see `docker:docker-on-proxmox`.

## Before you act

Check version, verify fields, snapshot before risky changes, summarize + get approval. For anything touching
`lxc.idmap`, `features`, raw `lxc.*`, or hookscripts, route to `proxmox-host-config`.
