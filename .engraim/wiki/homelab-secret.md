# homelab-secret

Cross-platform credential helper shipped in [[homelab-assist]] `plugins/homelab-core/scripts/` — stores/
retrieves secrets in the OS-native store so nothing lives in plaintext. See [[Homelab conventions]].

- `homelab-secret` (POSIX sh): macOS **Keychain** (`security`), Linux **libsecret** (`secret-tool`) or `pass`.
  Verified round-trip (set/get/run/has/delete) against the real macOS Keychain.
- `homelab-secret.ps1` (Windows): native **Credential Vault** (WinRT `PasswordVault`, no module to install).
- Namespace/service = `homelab-assist`; the KEY you store = the env var its consumer expects (e.g.
  `NPM_PASSWORD`, `DOCKHAND_TOKEN`, `PVE_TOKEN_SECRET`).
- Subcommands: `set` (hidden prompt), `get`, `has`, `run KEY1,KEY2 -- cmd` (inject into child env + exec),
  `delete`. The [[Homelab MCP servers|generic MCP launcher]] auto-resolves a server's `SECRETS` list from it
  (existing env values win).

## Related
- [[homelab-assist]]
- [[Homelab conventions]]
- [[Homelab MCP servers]]
