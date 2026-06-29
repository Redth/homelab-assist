---
name: homelab-ssh
description: The execution discipline and guardrails for doing ANYTHING over SSH on a homelab host (Proxmox nodes, Docker hosts, NAS, etc.). SSH is root-level power with no undo, so load this whenever a task will run shell commands or edit files on a host over SSH. Covers host-identity confirmation, read-before-write, backup/rollback, validate-before-apply, the never-without-confirmation dangerous-command list, lockout avoidance, scripting hygiene, and secret handling. Works together with homelab-safety (which governs approval) — this skill governs how the SSH work is actually carried out.
---

# SSH execution discipline

SSH gives you a root-capable shell on a real machine. There is no undo, no preview, and a single bad command
can lose data or take the host (and everything it runs) offline. Treat every SSH session as high-consequence.

This skill is the **how** of SSH work. It assumes you've already cleared the **whether** with the
[`homelab-core:homelab-safety`](../homelab-safety/SKILL.md) gate: SSH is genuinely required (the API can't do
it), and the user approved the SSH drop and the change. Prefer APIs over SSH every time you have the choice.

## Pre-flight (before the first command)

1. **Confirm it must be SSH.** State why the API/MCP can't do this. If either can, use it instead.
2. **Confirm the host.** Verify you're on the intended machine before acting — `hostname`, `hostname -I` /
   `ip a`, and for Proxmox `pveversion`. Name the host in your plan. Acting on the wrong node is a top cause
   of incidents.
3. **Secure a way back in** for anything touching networking, firewall, SSH itself, auth, or the bootloader:
   confirm console/IPMI/iKVM access exists *first*, so a mistake can't permanently lock you out. If there's
   no out-of-band access, say so and treat such changes as especially dangerous.
4. **Know the rollback** before you start. If you can't describe how to undo it, you're not ready to do it.

## The core loop: read → back up → show → apply → verify

- **Read before write.** Inspect current state and show it before changing anything: `cat`/`sed -n`, `qm config`,
  `pct config`, `ip a`, `docker inspect`, `systemctl status`. Never edit a file you haven't displayed.
- **Back up before edit.** Copy any file before modifying it and tell the user the backup path:
  `cp -a /etc/network/interfaces /etc/network/interfaces.bak.$(date +%Y%m%d-%H%M%S)`. Know the restore command.
- **Show exact commands / diffs**, grouped into a plain-language plan, and get approval via `homelab-safety`.
  Re-confirm if reality differs from your plan mid-way (an unexpected diff, error, or state).
- **Validate before apply** using the platform's own checker / dry-run wherever one exists (table below).
- **Apply the least-drastic way.** Prefer a reversible reload over a reboot; change one risky thing at a time.
- **Verify after.** Run a read-only check that the change did what you intended, and report it. Never assume
  success because a command exited 0.

### Validate-before-apply cheatsheet

| Domain | Check / dry-run before applying |
| --- | --- |
| Proxmox guest config | `qm config <id>` / `pct config <id>` to confirm the resulting config; snapshot first |
| Networking (ifupdown2) | apply with `ifreload -a` (reversible) rather than rebooting; keep console open |
| nginx / NPM | `nginx -t` before reload |
| docker compose | `docker compose config` to validate before `up` |
| systemd units | `systemd-analyze verify <unit>`; `systemctl status` after |
| sudoers | `visudo -c` (never hand-edit `/etc/sudoers` without it) |
| fstab | `mount -a` test in a safe window; a bad fstab can block boot |
| cron | install to a tempfile and `crontab tempfile`, not blind edits |

## Never run without explicit, specific confirmation

These are destructive or lockout-prone. Do **not** run them as part of a batch the user approved at a high
level — each needs its own explicit "yes, do exactly this to exactly that", and a backup/rollback in hand:

- **Data destruction:** `rm -rf`, `rm` with globs/wildcards, `find ... -delete`, `truncate`, `shred`.
- **Disk/filesystem:** `dd`, `mkfs.*`, `wipefs`, `parted`/`sgdisk`/`fdisk` writes, `zpool destroy`, `lvremove`,
  `vgremove`.
- **Overwrites:** `>` redirection onto an existing file (back up first, or use `tee -a`); `mv` over an existing
  file; `cp` over config without a backup.
- **Broad permission/ownership changes:** `chmod -R` / `chown -R` on `/`, `/etc`, home, or storage roots.
- **Network/firewall/SSH lockout:** `iptables -F` / flushing rules remotely, `ufw`/`nft` resets, stopping or
  disabling `networking`/`sshd`, editing `/etc/network/interfaces`, `/etc/ssh/sshd_config`, or the bootloader
  **without** confirmed out-of-band access.
- **Availability:** `reboot`/`shutdown`/`poweroff` of a host without first migrating/stopping its guests or
  warning the user of downtime; `kill -9` of critical processes; `systemctl stop` of storage/cluster services.
- **Supply-chain / blind execution:** piping remote content into a shell (`curl … | sh`, `wget … | bash`).
  Download, inspect, then run — never pipe-to-shell.
- **Cascading package changes:** `apt remove`/`autoremove` that could pull critical packages; check what would
  be removed first.

If the user's goal truly requires one of these, isolate it, explain the exact effect and the rollback, and get
a direct yes for that specific command.

## Scripting & command hygiene

- Any non-trivial script: start with `set -euo pipefail`; quote every `"$variable"`; avoid unquoted globs.
- Prefer **idempotent** operations (check-then-act) so a re-run is safe.
- **Never put secrets on the command line** — they leak into shell history and `ps`. Pass via environment, a
  protected file, or stdin. Don't echo tokens/passwords; redact them in any output you show. Pull credentials
  (SSH key passphrases, host passwords if unavoidable, tokens) from the OS secret store via the
  [`homelab-secrets`](../homelab-secrets/SKILL.md) skill — and prefer SSH **keys + ssh-agent** over passwords.
- Don't write secrets into world-readable files; set restrictive permissions (`chmod 600`) when you must.
- Avoid long-lived root shells; prefer a normal user with `sudo <specific command>` where the host allows it.
- Keep a record of what you ran, so the session is auditable and reversible.

## When unsure

If you're not confident a command is safe on this host/version, **stop**: research the version-correct method
([`homelab-core:homelab-research`](../homelab-research/SKILL.md)), propose a read-only verification step, or
ask the user. On anything destructive, uncertainty means don't.

## Why no SSH-wrapping MCP server

We deliberately do **not** build MCP servers that merely shell out over SSH — that adds packaging and a second
trust boundary without removing any of SSH's risk. SSH work lives in guardrailed skills like this one instead.
Custom MCP servers are reserved for real APIs/protocols (e.g. the `docker` plugin's `npm` server over the NPM
REST API). See `docs/DESIGN.md`.
