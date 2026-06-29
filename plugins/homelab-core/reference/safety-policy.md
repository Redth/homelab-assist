# Safety policy (reference)

The authoritative, human-readable version of the change-approval and SSH-confirmation policy enforced by the
`homelab-safety` skill. Linked from the repo README so users understand what the AI will and won't do
unattended.

## Operation classes

- **Read-only** — runs without confirmation. Listing, status, logs, reading configs, version checks.
- **Significant change** — requires a batched plain-language summary + your approval. Start/stop/restart,
  deploy/update, config edits, disk/network changes, image pulls, applying updates.
- **Destructive** — same approval, plus an explicit call-out of irreversibility and data at risk. Deletes,
  wipes, force-removes, restore-over-existing.
- **SSH / host-level** — requires confirming the SSH drop itself *before* the change, with the exact
  commands/diffs shown, plus read-first inspection and a backup of any file before editing.

## What "approval" looks like

A high-level summary you can understand without reading every command:

> I'm going to: stop the `media` stack on host `docker-01`, edit its compose to add an NFS volume mount, and
> redeploy. Downtime ~30s for Jellyfin/Sonarr. The compose file is backed up first. Rollback = restore the
> backup and redeploy. OK to proceed?

You can approve, refine, ask questions, or reject. Approval covers the described batch — surprises mid-batch
trigger a re-confirm.

## Non-negotiables

1. API before SSH, always.
2. Confirm the SSH drop before using SSH.
3. Back up host files before editing them.
4. Snapshot/backup before risky changes when the platform supports it.
5. Verify state before trusting it; reconcile contradictions before proceeding.
6. Check the version and research version-correct guidance before non-obvious actions.
