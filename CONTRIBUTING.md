# Contributing

This repo is a marketplace of Claude Code plugins for homelab management. Contributions that add new skills,
new target plugins, helper scripts, or MCP wiring are welcome.

## Repo layout

```
.claude-plugin/marketplace.json   # the catalog — every plugin must be listed here
plugins/<plugin>/
  .claude-plugin/plugin.json       # plugin manifest
  skills/<skill>/SKILL.md          # one folder per skill, YAML frontmatter required
  reference/*.md                   # version notes, endpoint maps, config-file locations
  scripts/*                        # helper scripts (read-only by default)
  .mcp.json                        # optional: references to community MCP servers
docs/                              # DESIGN, VERSIONING, this guide's deep-dives
```

## Writing a skill

A skill is a folder under `plugins/<plugin>/skills/` containing `SKILL.md`:

```markdown
---
name: proxmox-vms
description: Create, start, stop, migrate, snapshot, and inspect Proxmox QEMU VMs via the REST API. Use when the user wants to manage virtual machines on Proxmox.
---

# ...skill body...
```

Rules:

- **`description` is how Claude decides to load the skill** — make it specific and trigger-rich.
- **Defer to `homelab-core`.** Any skill that performs writes must route through the safety/approval gate
  described in `homelab-safety`. Any SSH usage must route through the SSH-confirmation gate. Don't restate the
  policy — reference it.
- **Be version-correct.** Tell the model to verify the running version and consult canonical docs
  (`homelab-research`) before acting on anything non-obvious. Don't bake in version-specific facts that rot;
  point at the source of truth.
- **API-first.** Reach for SSH only in the explicitly-host-level skills, with the documented confirmation.
- Keep large reference material in `reference/*.md` and link to it, so `SKILL.md` stays scannable.

## Adding a new target plugin

See the "Adding a new target plugin" section of [`docs/DESIGN.md`](docs/DESIGN.md).

## Validating before you push

- `jq . .claude-plugin/marketplace.json` and `jq . plugins/*/.claude-plugin/plugin.json` parse cleanly.
- Every `SKILL.md` has valid frontmatter with `name` + `description`.
- Manifest paths are relative and `./`-prefixed; no `../` escapes.
- Test locally: `/plugin marketplace add ./` then install and exercise the skill.

## Versioning

Follow [`docs/VERSIONING.md`](docs/VERSIONING.md): commit-SHA during active dev, explicit semver once stable.

## Secrets

Never commit credentials. Reference env vars / plugin user config. `.gitignore` blocks `.env`, keys, and
local secrets — keep it that way.
