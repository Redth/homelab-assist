---
name: proxmox-host-config
description: Host-level Proxmox configuration that the REST API cannot do and requires SSH/file edits — GPU and PCIe passthrough, VFIO/IOMMU/kernel-module setup, unprivileged-LXC lxc.idmap UID/GID remapping, LXC features (nesting/keyctl/fuse), hookscripts, and advanced /etc/network/interfaces. Load ONLY when an API-based approach is impossible. Every action here requires confirming the SSH drop first and backing up files before editing.
---

# Proxmox host-level configuration (SSH required)

This skill covers the things the API **cannot** do, which live in host files. **This is the fallback, not the
default.** We intentionally do this over SSH with guardrails rather than via a custom MCP server (an
SSH-wrapping server would add risk and packaging without removing SSH's danger).

**Before and during any work here, follow two homelab-core skills:**

- `homelab-core:homelab-safety` — clear the SSH-confirmation gate: state why the API can't do it, show exactly
  what you'll run/edit, get explicit approval for the SSH drop and the change.
- `homelab-core:homelab-ssh` — the full SSH execution discipline: **confirm the host** (`hostname`,
  `pveversion`), **secure a way back in** before networking/boot changes, **read → back up → show → apply →
  verify**, validate-before-apply, and the dangerous-command list. Apply it to the whole session.

Proxmox-specific reminders on top of `homelab-ssh`:

- `/etc/pve` is the **pmxcfs cluster filesystem** — edits propagate to all nodes; treat with extra care.
- Validate the resulting guest config with `qm config <id>` / `pct config <id>` after editing the `.conf`.
- Many changes need a **guest or host reboot** — say so in your plan, and migrate/stop guests before rebooting
  a node.
- Networking edits (`/etc/network/interfaces`) can cut off the node — keep console/IPMI access and apply with
  `ifreload -a`, not a blind reboot.
- Research the **version-correct** approach first (`homelab-core:homelab-research`) — passthrough and kernel
  specifics change between PVE/kernel versions. See
  [`reference/gpu-passthrough.md`](../../reference/gpu-passthrough.md),
  [`reference/config-files.md`](../../reference/config-files.md),
  [`reference/version-notes.md`](../../reference/version-notes.md).

## Config file locations

| File | Purpose |
| --- | --- |
| `/etc/pve/qemu-server/<vmid>.conf` | VM config incl. `hostpci0`, `args`, `hookscript` |
| `/etc/pve/lxc/<ctid>.conf` | LXC config incl. `lxc.idmap`, `features`, raw `lxc.*`, `lxc.hook.*` |
| `/etc/modules` | kernel modules loaded at boot (vfio, etc.) |
| `/etc/modprobe.d/*.conf` | module options, blacklists, `softdep` ordering |
| `/etc/default/grub` or `/etc/kernel/cmdline` | kernel cmdline (IOMMU flags) — bootloader-dependent |
| `/etc/network/interfaces` | bridges, bonds, VLAN-aware bridges |
| `/var/lib/vz/snippets/` | hookscripts referenced from guest configs |

`/etc/pve` is the cluster filesystem (pmxcfs) — edits propagate cluster-wide; treat carefully.

## GPU / PCIe passthrough (high-care)

Full walkthrough in [`reference/gpu-passthrough.md`](../../reference/gpu-passthrough.md). The moving parts:

1. **Firmware:** enable IOMMU (Intel VT-d / AMD-Vi) in BIOS/UEFI.
2. **Kernel cmdline:** add `intel_iommu=on iommu=pt` (or `amd_iommu=on iommu=pt`) via the correct bootloader
   for this install (GRUB vs systemd-boot — **check which** this node uses; don't assume).
3. **Modules:** ensure `vfio`, `vfio_iommu_type1`, `vfio_pci` in `/etc/modules` (note: `vfio_virqfd` was
   removed on 9.x — do not add it there).
4. **Bind device to vfio / isolate driver:** `/etc/modprobe.d/vfio.conf` with the device IDs and/or
   `softdep` lines (e.g. `softdep amdgpu pre: vfio-pci`).
5. **Assign to the VM:** add `hostpci0: 0000:01:00,pcie=1,x-vga=1` (IDs/flags vary) to
   `/etc/pve/qemu-server/<vmid>.conf`.
6. Reboot host (steps 2-4) and VM (step 5); verify with `lspci -nnk` (driver in use = `vfio-pci`).

Discover devices/IDs read-only first: `lspci -nnk`, `find /sys/kernel/iommu_groups/ -type l`. Verify IOMMU
group isolation before trusting passthrough. AMD reset bugs may need `vendor-reset` — research per-GPU.

## Unprivileged LXC bind mounts & idmap

Bind-mounting host storage into an **unprivileged** container gives wrong ownership unless you remap UIDs.
Two common approaches (research current best practice for the version):

- **idmap remap** in `/etc/pve/lxc/<ctid>.conf` (`lxc.idmap` entries) plus matching host
  `/etc/subuid` / `/etc/subgid` ranges, so a container UID maps to a chosen host UID owning the data.
- Or set ownership on the host to the default mapped range (container root → host `100000`).

This is fiddly and easy to get wrong — show the planned `lxc.idmap` block and the resulting host↔container UID
mapping in your summary before applying.

## LXC features for Docker-in-LXC

Running Docker inside an LXC typically needs `features: nesting=1,keyctl=1` (and sometimes `fuse=1`) in
`/etc/pve/lxc/<ctid>.conf`. See `docker:docker-on-proxmox` for the full Docker-host-in-LXC picture
(privileged vs unprivileged trade-offs, storage, VLAN).

## Hookscripts

A `hookscript: local:snippets/<file>.pl` line in a guest config runs at lifecycle phases (pre-start,
post-stop) — used e.g. to bind/unbind a GPU. The script lives in `/var/lib/vz/snippets/` and must be
executable. Show the script and the config reference before installing.

## Networking (advanced)

Basic NIC assignment is API-doable; **VLAN-aware bridges, bonds, and trunk setup** live in
`/etc/network/interfaces`. A bad edit can cut off the node — always back up, and prefer applying via
`ifreload -a` with a way back in (console/IPMI) rather than a blind reboot. See `docker:docker-on-proxmox` for
how this ties to container VLANs.

## Always

Read-first, back up, show diffs, confirm the SSH drop and the change, research the version-correct method, and
note required reboots. When uncertain on a destructive passthrough/network change, propose a read-only
verification step first.
