# Plugin authoring deep-dive

Reference details for plugin/marketplace mechanics, distilled from the Claude Code docs. Verify against the
current docs (https://code.claude.com/docs/en/plugins) when in doubt — the schema gains fields over time.

## `marketplace.json`

Lives at `.claude-plugin/marketplace.json` in the repo root.

```json
{
  "name": "homelab-assist",
  "owner": { "name": "Redth", "email": "you@example.com" },
  "metadata": { "pluginRoot": "./plugins" },
  "plugins": [
    { "name": "proxmox", "source": "proxmox", "description": "...", "keywords": ["proxmox"] }
  ]
}
```

- `metadata.pluginRoot` lets `source` be a bare folder name (`"proxmox"`) instead of `"./plugins/proxmox"`.
- `source` may instead be an object for an external repo:
  `{ "source": "github", "repo": "owner/repo", "ref": "main" }`.
- Some names are reserved (e.g. `anthropic-marketplace`, `claude-*`). `homelab-assist` is fine.

## `plugin.json`

Lives at `plugins/<name>/.claude-plugin/plugin.json`.

```json
{
  "name": "proxmox",
  "description": "...",
  "author": { "name": "Redth" },
  "license": "MIT",
  "keywords": ["proxmox", "pve"],
  "skills": "./skills/",
  "mcpServers": "./.mcp.json"
}
```

- `name` (kebab-case) is required and namespaces everything: `/proxmox:proxmox-vms`.
- **Omit `version`** during active dev → commit-SHA versioning (see `docs/VERSIONING.md`).
- `skills` defaults to auto-discovery of `./skills/`; listing it explicitly is fine.
- Other optional keys: `commands`, `agents`, `hooks`, `displayName`. Add only when used.

## Skills

`plugins/<name>/skills/<skill>/SKILL.md` with YAML frontmatter:

```markdown
---
name: proxmox-vms
description: Trigger-rich one-liner Claude uses to decide when to load this skill.
---
```

Optional frontmatter: `allowed-tools` (restrict tools), `disable-model-invocation` (only run on explicit
`/` invoke). Supporting files/scripts can live in the skill folder and be referenced by relative path.

## MCP servers in a plugin

`plugins/<name>/.mcp.json`:

```json
{
  "mcpServers": {
    "proxmox": {
      "command": "uvx",
      "args": ["proxmox-mcp-plus"],
      "env": { "PROXMOX_HOST": "${PROXMOX_HOST}", "PROXMOX_TOKEN": "${PROXMOX_TOKEN}" }
    }
  }
}
```

- Transports: stdio (`command`/`args`), or `"type": "http"` with `url` + `headers`.
- Interpolation: `${CLAUDE_PLUGIN_ROOT}` (plugin install dir), `${user_config.KEY}` (from manifest
  `userConfig`), and `${ENV_VAR}` from the environment. **Confirm the exact set your Claude Code version
  supports** — this is the most version-sensitive area.
- Tool names get prefixed: `mcp__plugin_<plugin>_<server>__<tool>`.

## Install / test commands

```
/plugin marketplace add ./           # local
/plugin marketplace add Redth/homelab-assist
/plugin install proxmox@homelab-assist
/plugin marketplace update homelab-assist
/plugin list
```
