#!/usr/bin/env bash
# pve-api.sh — minimal Proxmox VE REST API helper using API-token auth.
#
# READ-FIRST by design: defaults to GET. Any write (POST/PUT/DELETE) must be approved by the user per the
# homelab-safety policy BEFORE you call this script with such a method.
#
# Auth uses a Proxmox API token (no password, no CSRF needed). Create one in the PVE UI under
# Datacenter > Permissions > API Tokens, or:
#   pveum user token add <user>@<realm> <tokenid> --privsep 1
#
# Configure via environment (never hard-code secrets):
#   PVE_HOST            e.g. pve.lan or 10.0.0.2
#   PVE_PORT            default 8006
#   PVE_TOKEN_ID        e.g. root@pam!automation   (USER@REALM!TOKENNAME)
#   PVE_TOKEN_SECRET    the token UUID secret
#   PVE_VERIFY_SSL      "1" to verify TLS (default), "0" to skip (self-signed homelab certs)
#
# Keep PVE_TOKEN_SECRET in the OS secret store, not a dotfile, and inject it per-call:
#   homelab-secret run PVE_TOKEN_SECRET -- pve-api.sh GET /version
# (see the homelab-core 'homelab-secrets' skill).
#
# Usage:
#   pve-api.sh GET  /nodes
#   pve-api.sh GET  /nodes/pve1/qemu
#   pve-api.sh GET  /version
#   pve-api.sh POST /nodes/pve1/qemu/100/status/start
#   pve-api.sh POST /nodes/pve1/lxc/200/snapshot --data 'snapname=pre-change'
#
# Notes:
#   - Path is relative to /api2/json. Leading slash optional.
#   - Extra args after the path are passed to curl (e.g. --data 'k=v' for form params).
#   - Output is raw JSON; pipe to `jq` to inspect.

set -euo pipefail

method="${1:-GET}"
path="${2:-/version}"
shift 2 || true

: "${PVE_HOST:?Set PVE_HOST (e.g. pve.lan)}"
: "${PVE_TOKEN_ID:?Set PVE_TOKEN_ID (USER@REALM!TOKENNAME)}"
: "${PVE_TOKEN_SECRET:?Set PVE_TOKEN_SECRET (token UUID)}"
port="${PVE_PORT:-8006}"

# Normalize path to start with a single slash.
case "$path" in
  /*) : ;;
  *) path="/$path" ;;
esac

insecure=()
if [ "${PVE_VERIFY_SSL:-1}" = "0" ]; then
  insecure=(--insecure)
fi

curl -fsS "${insecure[@]}" \
  -X "$method" \
  -H "Authorization: PVEAPIToken=${PVE_TOKEN_ID}=${PVE_TOKEN_SECRET}" \
  "https://${PVE_HOST}:${port}/api2/json${path}" \
  "$@"
