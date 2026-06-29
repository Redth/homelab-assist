---
name: homelab-research
description: How to research version-correct, canonical answers for homelab software (Proxmox, Docker, Dockhand, Portainer, nginx-proxy-manager, etc.) without being misled by stale search results. Load this whenever you are not fully confident about a configuration, command, API field, or behavior before acting on a homelab system.
---

# Researching homelab problems correctly

Homelab software moves fast and the web is full of **outdated** guides. A confidently-wrong answer here can
cause an outage or data loss. When you are not certain, slow down and research the **version-correct,
canonical** answer before acting.

## Rule 0 — establish the version first

Before trusting any guidance, find out what the system is actually running:

- Proxmox: `pveversion` (or `pveversion -v`), or API `GET /version`. PVE 8.x vs 9.x differ in real ways.
- Docker: `docker version`, `docker compose version`.
- Dockhand / Portainer / Dockge / NPM: check the app's version (UI footer, `/api` version endpoint, image
  tag). MCP/API clients are often version-matched (e.g. Portainer MCP `~=2.42` ↔ Portainer 2.42.x).

Then scope every search and every doc page to **that** version. A correct answer for 8.x can be wrong for 9.x.

## Rule 1 — prefer canonical, AI-optimized sources

Order of trust:

1. **Official versioned docs** for the exact version (see `reference/doc-sources.md`).
2. **`llms.txt` / AI-optimized docs** when the project publishes them (e.g. Docker:
   `https://docs.docker.com/llms.txt`). These are clean, current, token-efficient — fetch them first.
3. **Official API viewers / OpenAPI specs** for exact field names and shapes (Proxmox API viewer; Portainer
   api-docs).
4. **Project source / release notes / changelog** for "did this change recently?" questions.
5. **Forums / blogs / Reddit** only to find *leads*, then **verify against the canonical source**. Treat any
   undated or old post as suspect — check whether it predates the running version.

The curated, maintained source list lives in [`reference/doc-sources.md`](../../reference/doc-sources.md).
Consult it; keep it updated when you find a better source.

## Rule 2 — watch for version-rot signals

Be extra skeptical when you see:

- Commands or kernel modules that may have been removed/renamed (e.g. Proxmox `vfio_virqfd` is gone on 9.x).
- API fields or endpoints that moved (e.g. PVE 9.x VNC proxy changes).
- "Just set X" guides with no date and no version — verify before applying.
- Advice that contradicts the official docs — the official docs win for the running version.

## Rule 3 — verify on the actual system (read-only)

Confirm assumptions against the live system using **read-only** checks before changing anything:

- Does the field/endpoint exist on this version? (query it)
- Does the device/path/storage actually exist? (`lspci`, `lsblk`, list storage — read-only)
- What is the current config right now? (read it before editing)

If reality contradicts the doc/plan, **stop and reconcile** — don't push a change built on a wrong premise.

## Rule 4 — when still uncertain, say so

If after researching you're still not confident, tell the user what you found, what's ambiguous, and what the
safest next step is (often: a read-only test, a snapshot first, or asking them to confirm an assumption). Do
not guess on destructive operations.

## How this composes with safety

Research establishes *what* the correct action is; the [`homelab-safety`](../homelab-safety/SKILL.md) skill
governs *how* you get approval to perform it. Use both: research to be right, safety to be safe.
