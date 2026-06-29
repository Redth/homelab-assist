---
name: homelab-safety
description: The change-approval and SSH-confirmation policy for ALL homelab operations (Proxmox, Docker, and any other infrastructure). Load and apply this before performing any action that changes a homelab system or that requires SSH/host access. Other homelab skills defer to this policy.
---

# Homelab safety & change-approval policy

This is the shared safety contract for every homelab plugin. **Any skill that changes a system, or that
needs SSH/host access, must apply this policy.** When in doubt, treat the action as more dangerous, not less.

## Classify the operation first

| Class | Examples | What to do |
| --- | --- | --- |
| **Read-only** | list VMs/containers/stacks, show status, get logs, read configs, check versions, get backup list | Run freely. No confirmation needed. |
| **Significant change** | start/stop/restart, deploy/update a stack, edit a config, resize disk, change network, pull images, apply updates | **Summarize → confirm** (see below) before acting. |
| **Destructive** | delete VM/CT/stack/volume/snapshot, wipe storage, force-remove, restore-over-existing, anything irreversible or data-losing | **Summarize → confirm**, and explicitly call out what is irreversible and what data is at risk. |
| **SSH / host-level** | any drop to SSH, editing host files (`/etc/pve/...`, `/etc/network/interfaces`, modprobe, compose files on disk) | **Always confirm the SSH drop first** (see SSH gate), then apply the change-approval gate to the change itself. |

## The change-approval gate (batched, high-level)

Do **not** ask for per-command approval. Instead, before executing a batch of significant/destructive work:

1. **Summarize in plain language** what you're about to do and why:
   - The target(s): which node / VM / CT / stack / host.
   - The concrete actions, grouped (e.g. "stop stack `media`, edit its compose to add a volume, redeploy").
   - The blast radius: what depends on this, expected downtime, what could break.
   - **Reversibility:** is there a snapshot/backup? Can this be undone? What's the rollback?
2. **Ask the user to approve, refine, or reject.** Offer to answer questions first.
3. Only after approval, execute the batch. If something unexpected happens mid-batch (an error, a surprising
   diff, a state that contradicts your summary), **stop and re-confirm** rather than pushing through.

A good summary lets the user understand the change without reading every command. Prefer a short bulleted
plan over a wall of shell.

## The SSH-confirmation gate

The homelab plugins are **API-first**. SSH/host access is a fallback only for things the API genuinely can't
do (e.g. Proxmox GPU passthrough, `lxc.idmap`, hookscripts, raw networking; editing compose files directly).

Before using SSH **at all**:

1. State **why** the API can't do this and SSH is required.
2. Show **exactly** what you intend to run or edit on the host (commands / file diffs).
3. Get explicit approval for the SSH drop.
4. Prefer **read-first**: inspect the current file/state over SSH and show it before changing anything.
5. **Back up before edit:** copy the file (e.g. `cp x x.bak.<timestamp>`) before modifying host configs, and
   tell the user the backup path so a rollback is possible.

If a task can be done by either API or SSH, **choose the API** and don't ask for SSH.

This gate covers *getting permission* to use SSH. For *how to carry out* SSH work safely — host-identity
confirmation, backup/rollback, validate-before-apply, the never-without-confirmation dangerous-command list,
lockout avoidance, and secret handling — load the [`homelab-ssh`](../homelab-ssh/SKILL.md) skill and follow it
for the whole SSH session.

> We don't build MCP servers that just wrap SSH — that adds packaging and a second trust boundary without
> reducing SSH's risk. SSH lives in guardrailed skills (`homelab-ssh` + the host-level skills); custom MCP
> servers are reserved for real APIs (e.g. the `docker` plugin's `npm` server).

## Always

- **Verify before you trust.** If a system's actual state contradicts what the user or a doc said, surface
  that and pause — don't proceed on the stale assumption.
- **Check the version** of the system before acting on anything non-obvious, and research version-correct
  guidance (see the `homelab-research` skill). Homelab software changes fast; stale answers cause outages.
- **Never invent credentials or endpoints, and never expose them.** Read credentials from the OS secret store
  via the [`homelab-secrets`](../homelab-secrets/SKILL.md) skill — not plaintext dotfiles, the repo, or the
  command line — and never print a secret value back into the conversation.
- **Snapshot/backup before risky changes** when the platform supports it, and say so in your summary.
