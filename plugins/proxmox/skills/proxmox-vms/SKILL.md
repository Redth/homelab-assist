---
name: proxmox-vms
description: Manage Proxmox QEMU/KVM virtual machines via the REST API — list, inspect, start/stop/reboot, create, clone, migrate, snapshot/rollback, resize disks, and edit VM config. Load this for any virtual-machine task on Proxmox. For GPU/PCIe passthrough or other host-file-level VM config, use proxmox-host-config instead.
---

# Proxmox VMs (QEMU/KVM)

Manage VMs through the REST API (see [`proxmox-api`](../proxmox-api/SKILL.md) for auth + the `pve-api.sh`
helper). Obey the `homelab-core:homelab-safety` skill: reads are free; any
start/stop/create/delete/config-change needs a summarized plan + approval first.

## Inspect (read-only)

```bash
scripts/pve-api.sh GET /nodes/{node}/qemu                              # list VMs on a node
scripts/pve-api.sh GET /nodes/{node}/qemu/{vmid}/status/current        # power state, uptime, HA
scripts/pve-api.sh GET /nodes/{node}/qemu/{vmid}/config                # full config (disks, net, cpu, mem)
scripts/pve-api.sh GET /cluster/resources | jq '.data[]|select(.type=="qemu")'
```

## Lifecycle (write — confirm first)

```bash
scripts/pve-api.sh POST /nodes/{node}/qemu/{vmid}/status/start
scripts/pve-api.sh POST /nodes/{node}/qemu/{vmid}/status/shutdown     # graceful (ACPI)
scripts/pve-api.sh POST /nodes/{node}/qemu/{vmid}/status/stop         # hard stop (data risk if mid-write)
scripts/pve-api.sh POST /nodes/{node}/qemu/{vmid}/status/reboot
```

Prefer `shutdown` (graceful) over `stop` (hard power-off). These return a UPID — poll the task for success
(see `proxmox-api`).

## Snapshots (do this BEFORE risky changes)

```bash
scripts/pve-api.sh GET  /nodes/{node}/qemu/{vmid}/snapshot
scripts/pve-api.sh POST /nodes/{node}/qemu/{vmid}/snapshot --data 'snapname=pre-change&vmstate=0'
scripts/pve-api.sh POST /nodes/{node}/qemu/{vmid}/snapshot/{snap}/rollback   # destructive: reverts disk
scripts/pve-api.sh DELETE /nodes/{node}/qemu/{vmid}/snapshot/{snap}
```

Rollback **discards** changes since the snapshot — treat as destructive and confirm explicitly.

## Create / clone

```bash
# create (params vary widely — verify fields against the API viewer for the running version)
scripts/pve-api.sh POST /nodes/{node}/qemu --data 'vmid=120&name=web&memory=4096&cores=2&net0=virtio,bridge=vmbr0&scsihw=virtio-scsi-single'
# clone an existing VM/template
scripts/pve-api.sh POST /nodes/{node}/qemu/{vmid}/clone --data 'newid=121&name=web2&full=1'
```

## Config changes

`POST /nodes/{node}/qemu/{vmid}/config` sets parameters (memory, cores, disks, net, `onboot`, cloud-init
fields like `ciuser`, `ipconfig0`, etc.). Editing config while running may require a reboot to take effect.

```bash
scripts/pve-api.sh POST /nodes/{node}/qemu/{vmid}/config --data 'memory=8192&cores=4'
```

> **GPU/PCIe passthrough (`hostpci0`), serial/VFIO, args, and other host-level bits** are best done via the
> host config file with the surrounding kernel/module setup — use
> [`proxmox-host-config`](../proxmox-host-config/SKILL.md), not the API, and confirm the SSH drop.

## Migrate

```bash
scripts/pve-api.sh POST /nodes/{node}/qemu/{vmid}/migrate --data 'target={targetnode}&online=1'
```

Online migration needs shared storage or storage migration; check cluster + storage first.

## Disk resize (grow only; confirm)

```bash
scripts/pve-api.sh PUT /nodes/{node}/qemu/{vmid}/resize --data 'disk=scsi0&size=+10G'
```

Growing is safe-ish; the guest still must extend its filesystem. **Shrinking is not supported** and risks
data loss — don't.

## Before you act

- Check version (`proxmox-overview`) and verify field names against the API viewer if unsure
  (`homelab-research`).
- Snapshot before risky config changes.
- Summarize the plan (which VM, what changes, downtime, rollback) and get approval (`homelab-safety`).
