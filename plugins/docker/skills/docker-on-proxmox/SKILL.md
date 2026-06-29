---
name: docker-on-proxmox
description: Understand and troubleshoot a Docker host running inside Proxmox — as a VM or as a privileged/unprivileged LXC — and how VLAN networking, macvlan/ipvlan, VLAN-aware bridges, and passed-through/bind-mounted storage interact across the Proxmox and Docker layers. Load this when a Docker networking or storage problem may originate at the Proxmox layer, or when planning how to host Docker on Proxmox.
---

# Docker on Proxmox: networking & storage across layers

In this homelab the Docker host is usually a **Proxmox VM or LXC**. Many "Docker" problems (no network,
permission denied on a mount, VLAN not working) actually originate in the **Proxmox layer**. This skill helps
you reason across both. It bridges to the proxmox plugin — for host-file edits there, use
`proxmox:proxmox-host-config` (and confirm the SSH drop). Obey
the `homelab-core:homelab-safety` skill.

## VM vs LXC for the Docker host

| | VM | Privileged LXC | Unprivileged LXC |
| --- | --- | --- | --- |
| Docker support | Cleanest, most isolated | Works; needs `nesting=1,keyctl=1` (+`fuse=1`) | Works but more friction (idmap, some storage drivers) |
| Security | Strong isolation | Weak (container root ≈ host root) | Better than privileged |
| Storage passthrough | PCI/virtio disk; NFS/SMB inside guest | Easy host bind mounts | Bind mounts need `lxc.idmap` UID remap |
| Overhead | Higher | Low | Low |

Common homelab choice: a **privileged LXC** with `features: nesting=1,keyctl=1` for low overhead, **or** a VM
for stronger isolation. Know which you have — it changes how storage and networking behave. (Features/idmap
live in `/etc/pve/lxc/<ctid>.conf` → `proxmox:proxmox-host-config`.)

## Networking: how the layers stack

```
physical NIC ─► Proxmox bridge (vmbr0, VLAN-aware) ─► VM/LXC vNIC (optionally tagged: tag=<vlan>) ─► Docker networks (bridge / macvlan / ipvlan)
```

- **VLAN-aware bridge** (Proxmox): set on `vmbr0` (`bridge-vlan-aware yes`, `bridge-vids 2-4094`) so it acts as
  a trunk. Then a guest NIC with `tag=<vlan>` lands on that VLAN. Bridge/trunk config is host-level
  (`/etc/network/interfaces` → `proxmox:proxmox-host-config`).
- **Guest on a specific VLAN:** set the VM/LXC `netX` with `tag=<vlan>` (API-settable). The guest sees an
  untagged interface on that VLAN.
- **Docker macvlan/ipvlan:** to give containers IPs directly on a LAN/VLAN, use a `macvlan`/`ipvlan` Docker
  network bound to the guest's interface (or a tagged sub-interface for 802.1Q). Caveats:
  - **macvlan host↔container** on the same parent interface can't talk to each other by design (needs a
    macvlan shim or a different approach).
  - Inside an **LXC**, macvlan/ipvlan can be restricted (especially unprivileged) — a VM is often simpler for
    heavy macvlan use.
  - Reference: https://docs.docker.com/engine/network/drivers/macvlan/.

### Debugging "container has no network / wrong VLAN"

Walk the stack top-down, read-only at each layer:
1. Docker network: `docker network inspect <net>` — driver, parent, subnet/gateway correct?
2. Guest interface: is it up, correct IP/VLAN? (`ip a`, `ip -d link`)
3. Guest NIC `tag=`: does the Proxmox `netX` config put it on the intended VLAN?
4. Proxmox bridge: is it VLAN-aware and is the VLAN in `bridge-vids`? (`bridge vlan show` on the node)
5. Upstream switch: is the port a trunk carrying that VLAN? (out of scope to change, but verify expectations)

## Storage: how the layers stack

- **VM:** pass storage as a virtio disk, or mount **NFS/SMB inside the guest**. Bind-mount permissions are
  normal Linux ownership inside the VM.
- **Privileged LXC:** host bind mounts (`mp0=/host/path,mp=/data`) map ownership 1:1 — easy, but privileged.
- **Unprivileged LXC:** container UID 0 = host UID 100000+, so a host bind mount shows up **owned by nobody /
  wrong UID** inside the container → fix with `lxc.idmap` remapping (host-level →
  `proxmox:proxmox-host-config`) or set host ownership to the mapped range. This is the usual cause of
  "permission denied" on a Docker volume that's a bind mount in an unprivileged LXC.
- **NFS/SMB into LXC:** can be fiddly (especially unprivileged); mounting on the host and bind-mounting in, or
  mounting inside a privileged container, are common patterns — research the current best practice.

### Debugging "permission denied on a volume"

1. Is it a **named volume** or a **bind mount**? (named volumes avoid host-UID issues)
2. If bind mount: privileged or unprivileged LXC? (`unprivileged: 1` in the CT config)
3. Unprivileged → check `lxc.idmap` and the host-side ownership of the path vs the container UID that needs it.
4. Confirm the in-container service's UID/GID (`PUID/PGID` for linuxserver images) matches the mapped owner.

## Putting it together

When something's broken, decide **which layer** before changing anything: Docker (network/volume config),
guest OS (interface/mount), or Proxmox (bridge/VLAN/idmap). Make host-level Proxmox changes via
`proxmox:proxmox-host-config` with the SSH-confirmation gate; make Docker changes via the relevant control
plane (`dockhand`/`portainer`/`dockge`). Summarize cross-layer changes clearly so the user sees the full
picture before approving.
