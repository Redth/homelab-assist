---
name: proxmox-storage-backups
description: Understand and manage Proxmox storage and backups via the REST API — list storage and content, understand storage types (dir/LVM/ZFS/NFS/CIFS/PBS), run vzdump backups, schedule backup jobs, and restore VMs/LXC from archives or Proxmox Backup Server. Load this for storage inspection, backup creation, or restore tasks on Proxmox.
---

# Proxmox storage & backups

Manage via the REST API (see [`proxmox-api`](../proxmox-api/SKILL.md)). Obey
the `homelab-core:homelab-safety` skill — **restore is destructive** when it
overwrites an existing guest.

## Understand storage (read-only)

```bash
scripts/pve-api.sh GET /storage                                       # cluster-wide storage definitions
scripts/pve-api.sh GET /nodes/{node}/storage                          # status incl. used/avail per store
scripts/pve-api.sh GET /nodes/{node}/storage/{store}/content          # volumes, backups, ISOs, templates
```

Storage types and what they hold:

| Type | Typical content | Notes |
| --- | --- | --- |
| `dir` | anything (images, backups, ISO, vztmpl) | simple directory on a filesystem |
| `lvm` / `lvmthin` | VM/CT disks | block; thin supports snapshots |
| `zfspool` | VM/CT disks | snapshots, compression, send/recv |
| `nfs` / `cifs` | backups, ISO, images | network shares; check connectivity if "inactive" |
| `pbs` | backups (dedup, incremental) | Proxmox Backup Server — preferred for real backups |

The `content` field on a storage def restricts what it can store (e.g. only `backup`, only `images`). A
backup will fail if the target storage doesn't allow `backup` content.

## Backups (vzdump)

```bash
# one-off backup of a guest (confirm first)
scripts/pve-api.sh POST /nodes/{node}/vzdump --data 'vmid=100&storage=pbs&mode=snapshot&compress=zstd'
```

- `mode`: `snapshot` (live, preferred), `suspend`, or `stop`. `snapshot` needs snapshot-capable storage.
- Backups run async → poll the returned UPID task for success (`proxmox-api`).
- **Scheduled jobs** live under `/cluster/backup` (`GET` to list, `POST` to create — confirm first).

```bash
scripts/pve-api.sh GET /cluster/backup
```

## Restore (DESTRUCTIVE when overwriting)

Restoring into an **existing** VMID overwrites that guest. Treat as destructive: confirm explicitly, call out
that current data is replaced, and prefer restoring to a **new** VMID when validating a backup.

```bash
# restore a backup archive into a (new) VMID
scripts/pve-api.sh POST /nodes/{node}/qemu --data 'vmid=130&archive=pbs:backup/vm/100/<...>&storage=local-lvm'
# LXC restore uses the lxc create endpoint with ostemplate=<archive> + restore=1
scripts/pve-api.sh POST /nodes/{node}/lxc --data 'vmid=230&ostemplate=<archive>&restore=1&storage=local-lvm'
```

Archive identifiers come from the storage `content` listing (filter `content=backup`). Verify the exact
parameter names/shape against the API viewer for the running version (`homelab-research`) — restore params
are version-sensitive.

## Before you act

- Confirm free space on the target storage (`GET /nodes/{node}/storage`).
- For restores: confirm whether you're overwriting; prefer a new VMID to validate first.
- Summarize the plan (source archive, target, overwrite?, downtime) and get approval (`homelab-safety`).
