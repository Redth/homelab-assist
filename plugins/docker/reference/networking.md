# Docker + Proxmox networking & storage (reference)

Concept reference for the `docker-on-proxmox` skill. Verify version-specific behavior against the official
docs (`homelab-research`): Docker macvlan https://docs.docker.com/engine/network/drivers/macvlan/ ; Proxmox
networking/wiki.

## The layered network path

```
Switch (trunk port, tagged VLANs)
  └─ Proxmox node physical NIC
       └─ vmbr0  (VLAN-aware bridge: bridge-vlan-aware yes, bridge-vids 2-4094)   [/etc/network/interfaces]
            └─ guest netX  (tag=<vlan>)   [VM/LXC config — API-settable]
                 └─ guest OS interface (untagged, on that VLAN)
                      └─ Docker network (bridge | macvlan | ipvlan)   [compose]
                           └─ container
```

Change each layer in the right place:
- Bridge/trunk/VLAN-aware → host file `/etc/network/interfaces` → `proxmox:proxmox-host-config` (SSH gate).
- Guest NIC `tag=` → Proxmox API (`netX` on the VM/LXC).
- Docker network → compose / control plane.

## Docker network drivers for homelab

- **bridge** (default): NAT behind the host IP. Simple; good default for most stacks behind a reverse proxy.
- **macvlan**: each container gets its own MAC + IP directly on the LAN/VLAN. Good for things needing a real
  LAN presence (Pi-hole, etc.). Gotchas: host↔container on the same parent can't communicate; needs a parent
  interface (or 802.1Q sub-interface `eth0.<vlan>`); restricted inside unprivileged LXC.
- **ipvlan**: like macvlan but shares the host MAC (some switches/IPAM prefer this).

## VLANs

- For multiple VLANs into one guest, either give the guest multiple `netX` each `tag=`'d, or trunk into the
  guest and create 802.1Q sub-interfaces, then bind Docker macvlan/ipvlan networks to them.
- Verify on the node: `bridge vlan show`; in the guest: `ip -d link`.

## Storage paths

- **Named Docker volumes** — avoid host-UID problems; preferred when you don't need host-path access.
- **Bind mounts** — host path into container. On Proxmox:
  - VM: normal Linux ownership inside the guest.
  - Privileged LXC: 1:1 UID mapping; easy.
  - Unprivileged LXC: needs `lxc.idmap` UID remap (host-level) or host ownership set to the mapped range; the
    usual cause of "permission denied" on a bind-mounted Docker volume.
- **NFS/SMB**: mount inside a VM/privileged-LXC, or mount on the host and bind-mount in. Unprivileged LXC +
  network shares is fiddly — research current best practice.

## Quick cross-layer triage

| Symptom | Most likely layer |
| --- | --- |
| Container has no IP / wrong subnet | Docker network config |
| Container up but unreachable on LAN | guest VLAN `tag=` or Proxmox bridge VLAN |
| Whole guest off-network | Proxmox bridge / `/etc/network/interfaces` / switch trunk |
| "permission denied" on bind mount | unprivileged LXC idmap / host ownership |
| Reverse proxy 502 to a service | Docker network reachability between NPM and the upstream |
