# Homelab conventions

Cross-cutting rules baked into [[homelab-assist]] `homelab-core`, applied by every infra skill.

## Safety & change approval (`homelab-safety`)
- Classify ops: **read-only** (run freely) / **significant** (start/stop/deploy/config edit) / **destructive**
  (delete/wipe/`down -v`/restore-over-existing) / **SSH/host-level**.
- Significant+destructive need a **batched, plain-language summary → user approval** first (not per-command).
  Destructive calls out irreversibility + data at risk. Snapshot/backup before risky changes when possible.

## API-first, SSH last (`homelab-ssh`)
- Prefer control-plane APIs (Proxmox REST, Dockhand/Portainer REST) over SSH always.
- SSH is a gated fallback: confirm the SSH drop first; read→back up→show diff→validate→apply→verify; keep a
  way back in for network/boot/SSH/auth changes; never-without-explicit-confirmation list (rm -rf, dd, mkfs,
  firewall flush, `curl|sh`, etc.). **No MCP server merely wraps SSH** (see [[Homelab MCP servers]]).

## Secrets (`homelab-secrets` + [[homelab-secret]])
- Credentials live in the **OS-native secret store** (macOS Keychain, Windows Credential Vault, Linux
  libsecret/`pass`), never plaintext dotfiles/repo/command-line. Never print a secret value.
- `plugins/homelab-core/scripts/homelab-secret` (sh) + `.ps1` (Windows). MCP launchers auto-resolve a server's
  `SECRETS` env vars from the store (env wins).

## Research (`homelab-research`)
- Check the running **version first** (`pveversion`, docker/Dockhand versions); homelab software changes fast
  and search results go stale. Prefer canonical/`llms.txt` sources; verify on the live system read-only before
  acting.

## Related
- [[homelab-assist]]
- [[Homelab MCP servers]]
- [[homelab-secret]]
- [[Redth homelab environment]]
