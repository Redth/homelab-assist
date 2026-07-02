# Redth homelab environment

The actual homelab that [[homelab-assist]] targets (owner Redth, jondick@gmail.com; dev machine is macOS
arm64).

- **Hypervisor: Proxmox VE.** Docker hosts often run as Proxmox VMs or LXCs (privileged or unprivileged).
  Cares about GPU passthrough, complex storage mounts, VLAN setups.
- **Docker control plane: Dockhand preferred** (`Finsys/dockhand`) — one Dockhand server managing **several
  Hawser agents** (each Hawser host = a Dockhand "environment"). Also uses Dockge and Portainer, but Dockhand
  is default. Heavy compose-stack user. See [[Dockhand REST API]].
- **Reverse proxy: nginx-proxy-manager**, driven by container labels via Redth's own tool
  **`Redth/npm-docker-sync`** (`npm.*` labels auto-create NPM proxy hosts for domain routing).
- Prefers APIs over SSH; wants explicit confirmation before SSH and a high-level summary before
  significant/destructive changes. See [[Homelab conventions]].

## Related
- [[homelab-assist]]
- [[Dockhand REST API]]
- [[Homelab conventions]]
