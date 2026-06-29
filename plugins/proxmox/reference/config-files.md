# Proxmox host config files (reference)

Where host-level configuration lives — the things the API can't manage. Editing these requires SSH (confirm
the drop), read-first, and a backup before edit. See the `proxmox-host-config` skill.

## Guest configs (pmxcfs — propagate cluster-wide)

- `/etc/pve/qemu-server/<vmid>.conf` — VM config. Relevant host-level keys:
  - `hostpci0: 0000:01:00,pcie=1,x-vga=1` — PCIe/GPU passthrough
  - `args: ...` — raw QEMU args
  - `hookscript: local:snippets/<file>` — lifecycle hook
- `/etc/pve/lxc/<ctid>.conf` — LXC config. Relevant keys:
  - `unprivileged: 1`
  - `features: nesting=1,keyctl=1,fuse=1` — needed for Docker-in-LXC, etc.
  - `lxc.idmap: ...` — UID/GID remapping for unprivileged bind mounts
  - `lxc.hook.pre-start: ...`, other raw `lxc.*` keys
  - `mp0: <storage|hostpath>,mp=/path` — mount points

> `/etc/pve` is the Proxmox cluster filesystem (pmxcfs). It's a FUSE mount backed by the cluster DB; edits
> sync to all nodes. Don't treat it like an ordinary directory.

## Kernel / modules (per-node, host filesystem)

- `/etc/modules` — modules loaded at boot. For passthrough: `vfio`, `vfio_iommu_type1`, `vfio_pci`.
  (`vfio_virqfd` was removed on PVE 9.x / newer kernels — do not add it.)
- `/etc/modprobe.d/*.conf` — module options, `blacklist`, `softdep` ordering, `options vfio-pci ids=...`.
- Kernel cmdline — **bootloader-dependent**:
  - GRUB installs: `/etc/default/grub` (`GRUB_CMDLINE_LINUX_DEFAULT`), then `update-grub`.
  - systemd-boot installs (ZFS-on-root, newer): `/etc/kernel/cmdline`, then `proxmox-boot-tool refresh`.
  - **Check which** the node uses before editing.

## Networking (per-node)

- `/etc/network/interfaces` — bridges (`vmbr0`), bonds, VLAN-aware bridges (`bridge-vlan-aware yes`,
  `bridge-vids 2-4094`). Apply with `ifreload -a` (Proxmox uses ifupdown2). A bad edit can lock you out —
  keep console/IPMI access.

## Subuid / subgid (for LXC idmap)

- `/etc/subuid`, `/etc/subgid` — host UID/GID ranges available for container remapping; referenced by
  `lxc.idmap` setups.

## Hookscripts

- `/var/lib/vz/snippets/<file>` — executable scripts referenced by `hookscript:` in guest configs (e.g. GPU
  bind/unbind around VM start/stop). Must be executable; snippets storage must allow the `snippets` content
  type.

## Discovery commands (read-only)

- `lspci -nnk` — PCI devices + driver in use (verify `vfio-pci` after passthrough setup)
- `find /sys/kernel/iommu_groups/ -type l` — IOMMU group membership (isolation check)
- `lsblk`, `blkid`, `zpool status`, `pvesm status` — storage layout
- `ip -d link`, `bridge vlan show` — network/VLAN state
- `pveversion -v` — versions of all components
