# Proxmox version notes (reference)

**This file is a pointer, not a snapshot.** Proxmox changes between releases; do not trust any fact here
without confirming it against the official docs/wiki for the **running** version (`homelab-research`). Check
the version first: `pveversion -v` or API `GET /version`.

## Why this matters

Search results and old blog posts frequently describe behavior that changed. A correct 8.x procedure can be
wrong on 9.x (and vice-versa). Always scope guidance to the installed major.minor version.

## Known cross-version gotchas (verify currency before relying on these)

- **`vfio_virqfd` removed** — on PVE 9.x / recent kernels this module no longer exists; leaving it in
  `/etc/modules` causes a load error. Older guides still tell you to add it.
- **IOMMU reserved-memory enforcement** — newer kernels (6.10+) enforce `RESV_DIRECT`, which can break GPU
  passthrough on some AMD boards until a UEFI/firmware option is changed.
- **VNC proxy endpoint changes** — PVE 9.x changed VNC proxy behavior; external/3rd-party VNC clients that
  worked on 8.x may break.
- **Bootloader split** — GRUB vs systemd-boot determines where the kernel cmdline lives and how to apply it.
  Don't assume GRUB; check (`proxmox-boot-tool status` indicates systemd-boot/ZFS setups).
- **HA rules format** — upgraded clusters may migrate HA group rules to a new format automatically.
- **Mixed-version clusters** — a cluster may temporarily run mixed major versions during a rolling upgrade,
  but all nodes should converge promptly; don't run mixed long-term.

## Upgrade resources (read the version-specific one)

- Upgrade 8 → 9: https://pve.proxmox.com/wiki/Upgrade_from_8_to_9
- Roadmap / known issues: https://pve.proxmox.com/wiki/Roadmap
- Always run the official pre-upgrade checker (e.g. `pve8to9`) and clear all warnings before upgrading.

## How to get the authoritative answer

1. `pveversion -v` → note exact versions.
2. Open the **versioned** admin guide / API viewer (on-node `https://<host>:8006/pve-docs/` matches your
   install exactly).
3. Cross-check the wiki page for the specific feature, noting its last-updated date and version applicability.
4. Verify on the live system with read-only commands before changing anything.
