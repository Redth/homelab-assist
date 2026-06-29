---
name: homelab-secrets
description: How to store and use homelab credentials securely via the OS-native secret store (macOS Keychain, Windows Credential Vault, Linux libsecret/pass) instead of plaintext env vars, dotfiles, or committed files — and how to handle SSH auth (keys over passwords, ssh-agent, passphrases in the keychain). Load this whenever a task needs a token, password, or SSH credential: setting one up, retrieving one to run a command or MCP server, or advising the user where to keep it. Pairs with homelab-ssh and homelab-safety.
---

# Homelab secrets & credential storage

Homelab work needs credentials — Proxmox API tokens, NPM/Dockhand/Portainer passwords, SSH keys. **None of
them should live in plaintext** in the repo, in shell profiles (`.zshrc`/`.bashrc`), in committed `.env`
files, or pasted into the chat. Keep them in the **OS-native secret store** and inject them at the moment a
command or server needs them.

## Cardinal rules

1. **Never print a secret value** into the conversation, logs, or a file you show the user. Resolve secrets
   directly into a command's environment; don't echo them.
2. **Never put a secret on a command line** (`--password abc`) — it leaks into shell history and `ps`. Use the
   secret store, env injection, or stdin.
3. **Never commit secrets.** The repo `.gitignore` blocks `.env`/keys; keep it that way.
4. **Least privilege + rotation.** Use scoped tokens (e.g. a Proxmox API token with `--privsep` and only the
   roles a task needs — see `proxmox:proxmox-api`), and rotate/delete when no longer needed.

## The `homelab-secret` helper (ships with this plugin)

Use it to store and retrieve credentials in the OS store without touching plaintext files.

- macOS / Linux: [`scripts/homelab-secret`](../../scripts/homelab-secret) (Keychain, or libsecret/`pass`).
- Windows: [`scripts/homelab-secret.ps1`](../../scripts/homelab-secret.ps1) (Credential Vault).

Put it on your `PATH` once (e.g. symlink the unix script into `~/.local/bin/homelab-secret`). Secrets are
namespaced under the service **`homelab-assist`**, and the **KEY you choose should match the environment
variable the consumer expects** (so retrieval is mechanical):

| Consumer | Keys to store |
| --- | --- |
| `npm` MCP server (docker plugin) | `NPM_PASSWORD` (and optionally `NPM_EMAIL`, `NPM_URL`) |
| Proxmox API / `pve-api.sh` | `PVE_TOKEN_SECRET` (+ `PVE_TOKEN_ID`, `PVE_HOST`) |
| Dockhand MCP | `DOCKHAND_PASSWORD` |
| Portainer MCP | `PORTAINER_API_KEY` |

Commands:

```sh
homelab-secret set NPM_PASSWORD            # prompts hidden, stores in the OS vault
homelab-secret has NPM_PASSWORD            # exit 0 if present (no output)
homelab-secret get NPM_PASSWORD            # prints to stdout — pipe directly, never to chat
homelab-secret run NPM_PASSWORD -- npm-mcp # exports the key(s) into the child env, then execs
homelab-secret delete NPM_PASSWORD
```

(Windows: `homelab-secret.ps1 set NPM_PASSWORD`, etc.)

## Using secrets at runtime (without exposing them)

- **One command:** inject just-in-time, don't export globally:
  ```sh
  PVE_TOKEN_SECRET="$(homelab-secret get PVE_TOKEN_SECRET)" \
    plugins/proxmox/scripts/pve-api.sh GET /version
  ```
  or, cleaner, `homelab-secret run PVE_TOKEN_SECRET -- pve-api.sh GET /version`.
- **MCP servers:** the bundled `npm` server's launcher will **auto-resolve** missing `NPM_*` values from the
  store if `homelab-secret` is on your `PATH` (so you never have to export them in a dotfile). For reused
  community servers (Dockhand/Portainer), wrap the launch in `homelab-secret run <KEY> -- <server>` or set the
  vars in the session that starts Claude Code from the store.
- **A whole session:** if you must, `export NPM_PASSWORD="$(homelab-secret get NPM_PASSWORD)"` in the current
  shell only — never persist it to a profile file.

When you (the agent) retrieve a secret to run something, do it in a single command that pipes/injects the value
into the consumer. Do not display the retrieved value back to the user.

## SSH credentials specifically

SSH is the highest-power credential — handle it well (this complements `homelab-ssh`):

- **Prefer keys over passwords.** Use an SSH **key pair**; don't store/enter host passwords. Avoid `sshpass`
  with inline passwords entirely.
- **Protect the private key with a passphrase**, and load it via the agent so you type it once:
  - macOS: `ssh-add --apple-use-keychain ~/.ssh/id_ed25519` stores the passphrase in Keychain; add
    `AddKeysToAgent yes` and `UseKeychain yes` to `~/.ssh/config`.
  - Linux: `ssh-add` with `ssh-agent` (often via the desktop keyring); or `keychain`.
  - Windows: the OpenSSH `ssh-agent` service (`Get-Service ssh-agent`; `ssh-add`).
- **Use `~/.ssh/config`** for per-host `HostName`/`User`/`IdentityFile`/`Port` so commands carry no inline
  credentials and you target the right host by alias.
- **Scope keys per purpose** and remove authorized keys you no longer use. Consider a dedicated key for
  homelab automation.

## Backends by OS (what the helper uses)

| OS | Store | Set | Get |
| --- | --- | --- | --- |
| macOS | Keychain (`security`) | `security add-generic-password -U -s homelab-assist -a KEY -w` | `security find-generic-password -s homelab-assist -a KEY -w` |
| Windows | Credential Vault (WinRT `PasswordVault`) | via `homelab-secret.ps1 set` | via `homelab-secret.ps1 get` |
| Linux (desktop) | libsecret / GNOME Keyring (`secret-tool`) | `secret-tool store service homelab-assist account KEY` | `secret-tool lookup service homelab-assist account KEY` |
| Linux (headless) | `pass` (GPG) | `pass insert homelab-assist/KEY` | `pass show homelab-assist/KEY` |

Cross-platform alternative if you prefer one tool everywhere: the PowerShell
`Microsoft.PowerShell.SecretManagement` module (with `SecretStore`) runs on Windows/macOS/Linux.

## If no secret store is available (headless Linux without libsecret/pass)

Don't fall back to plaintext. Options, best first: install `pass` (GPG-encrypted, ideal for servers); use
`age`/`sops` to keep an encrypted secrets file decrypted only at runtime; or, at minimum, an `.env` file
`chmod 600` and **git-ignored** — and tell the user it's the weakest option. Never commit it.
