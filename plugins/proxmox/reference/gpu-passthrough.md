# GPU / PCIe passthrough on Proxmox (reference)

A deep-dive checklist. **Passthrough is version- and hardware-specific** — always confirm the current method
for the running PVE/kernel version and your exact GPU against the official wiki before applying
(`homelab-research`):

- https://pve.proxmox.com/wiki/PCI(e)_Passthrough
- https://pve.proxmox.com/wiki/PCI_Passthrough

All host edits require the SSH-confirmation gate + read-first + backup (see `proxmox-host-config`).

## Mental model

You're (1) telling the kernel to enable the IOMMU and isolate the device, (2) binding the device to the
`vfio-pci` stub driver instead of its normal driver, then (3) handing it to a VM. Each step is a different
file; most need a reboot.

## 1. Firmware

Enable IOMMU in BIOS/UEFI: **Intel** = VT-d, **AMD** = AMD-Vi / IOMMU. Also enable above-4G decoding / Resizable
BAR where relevant. On some modern AMD boards, newer kernels enforce IOMMU `RESV_DIRECT`; if passthrough
fails there, the board's UEFI option for it may need adjusting — research per-board.

## 2. Kernel cmdline (IOMMU)

Add to the cmdline (via the correct bootloader — see `config-files.md`):

- Intel: `intel_iommu=on iommu=pt`
- AMD: `amd_iommu=on iommu=pt`

Apply: GRUB → `update-grub`; systemd-boot → `proxmox-boot-tool refresh`. Reboot. Verify:
`dmesg | grep -e DMAR -e IOMMU` and `find /sys/kernel/iommu_groups/ -type l` (groups should be populated).

## 3. Load vfio modules

`/etc/modules` should contain:

```
vfio
vfio_iommu_type1
vfio_pci
```

(Do **not** add `vfio_virqfd` on PVE 9.x / recent kernels — it was merged/removed.) `update-initramfs -u -k all`
after changes, then reboot.

## 4. Isolate the device from its host driver

Find the device IDs: `lspci -nn` (e.g. `01:00.0 VGA ... [10de:2484]`, `01:00.1 Audio ... [10de:228b]`).

Options (in `/etc/modprobe.d/`):

- Bind by ID: `options vfio-pci ids=10de:2484,10de:228b` (include the GPU's audio function too).
- Prevent the host driver grabbing it first via `softdep`, e.g.:
  ```
  softdep nvidia pre: vfio-pci
  softdep amdgpu pre: vfio-pci
  softdep snd_hda_intel pre: vfio-pci
  ```
- Optionally blacklist the host GPU driver if the host doesn't need it.

`update-initramfs -u -k all`, reboot. Verify the GPU now shows `Kernel driver in use: vfio-pci` in
`lspci -nnk`.

## 5. IOMMU group isolation

The GPU (and its audio function) should be in their own IOMMU group, ideally not sharing with unrelated
critical devices. Check `find /sys/kernel/iommu_groups/ -type l`. If the group is too broad, ACS override is a
(risky, security-reducing) workaround — research before suggesting it; don't apply casually.

## 6. Assign to the VM

In `/etc/pve/qemu-server/<vmid>.conf`:

```
hostpci0: 0000:01:00,pcie=1,x-vga=1
```

- `0000:01:00` passes the whole function group (`.0` + `.1`); use `;` or multiple `hostpciN` for more.
- `pcie=1` requires a `q35` machine type.
- `x-vga=1` for a primary GPU (may need the VM's display set appropriately; some setups drop it).
- VM should use OVMF/UEFI (`bios: ovmf`) for modern GPUs.

Reboot the VM. Inside the guest, install the vendor driver and verify the GPU is recognized.

## AMD reset bug

Many AMD GPUs don't reset cleanly between VM restarts (the "reset bug"), needing a host reboot or the
`vendor-reset` kernel module (https://github.com/gnif/vendor-reset) plus a hookscript. Research whether your
specific card is affected before relying on restart-without-reboot.

## Verification summary (read-only)

- `dmesg | grep -i iommu` — IOMMU enabled
- `find /sys/kernel/iommu_groups/ -type l` — groups populated + device isolation
- `lspci -nnk` — device driver = `vfio-pci` on host
- In-guest: device present and driver loads

Report each check rather than assuming success.
