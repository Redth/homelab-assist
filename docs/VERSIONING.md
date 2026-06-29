# Versioning

This marketplace is a living, growing thing. We use a **two-phase** versioning strategy.

## Phase 1 — active development (current): commit-SHA versioning

- **Omit** the `version` field from `plugin.json` and from the plugin's entry in `marketplace.json`.
- Claude Code then versions the plugin by **git commit SHA**: every push to the tracked branch is treated as
  a new version, and users who installed get updates automatically.
- Best while a plugin's surface is changing rapidly. No release ceremony, fast iteration.

## Phase 2 — stabilized plugins: explicit semver

Once a plugin's skills are stable enough that users depend on them, promote it:

1. Add `"version": "MAJOR.MINOR.PATCH"` to that plugin's `plugin.json` (and optionally mirror it in the
   marketplace entry).
2. Start a `CHANGELOG.md` in the plugin directory (Keep a Changelog format).
3. From then on, **bump the version on every meaningful change** — if you push code without bumping, users
   pinned to explicit versions won't receive the update.

Semver meaning for a plugin:

- **MAJOR** — breaking change to how a skill behaves, renamed/removed skills, or a required-config change.
- **MINOR** — new skills, new capabilities, new optional config.
- **PATCH** — doc fixes, prompt tweaks, bug fixes that don't change the contract.

Plugins version **independently** — `proxmox` can be at `1.3.0` while `docker` is still commit-SHA. The
marketplace itself carries a coarse `version` for cataloguing only.

## Release channels (optional, later)

If we want a stable vs. bleeding-edge split, publish two marketplace entries pointing at different branches
(`ref: stable` vs `ref: main`). Not needed yet — revisit when there are external consumers.

## Checklist when promoting a plugin to semver

- [ ] Add `version` to `plugins/<name>/.claude-plugin/plugin.json`
- [ ] Create `plugins/<name>/CHANGELOG.md`
- [ ] Tag the release in git (`<plugin>-vX.Y.Z`)
- [ ] Note the promotion in the repo `README` plugin table
