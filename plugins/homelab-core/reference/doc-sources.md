# Canonical documentation sources

The curated, trusted source list for homelab research. **Always scope to the running version.** Prefer
`llms.txt` / AI-optimized docs and official API specs over blogs and forums. Keep this file updated as better
sources appear or projects publish `llms.txt`.

## Proxmox VE

- **Docs index (versioned):** https://pve.proxmox.com/pve-docs/
- **API viewer (exact endpoints/fields):** https://pve.proxmox.com/pve-docs/api-viewer/
- **On-node docs (matches your version):** `https://<your-pve-host>:8006/pve-docs/`
- **Admin guide chapters:** user mgmt/tokens `chapter-pveum.html`; backup `chapter-vzdump.html`
- **Wiki — PCI(e) passthrough:** https://pve.proxmox.com/wiki/PCI(e)_Passthrough
- **Wiki — Unprivileged LXC:** https://pve.proxmox.com/wiki/Unprivileged_LXC_containers
- **Wiki — Upgrade & known issues:** https://pve.proxmox.com/wiki/Upgrade_from_8_to_9 ; https://pve.proxmox.com/wiki/Roadmap
- **Version check:** `pveversion -v` on the node, or API `GET /version`.
- `llms.txt`: none known (gap). Use the API viewer + admin guide as the canonical machine-readable source.

## Docker

- **Engine API:** https://docs.docker.com/reference/api/engine/
- **Compose spec / file reference:** https://docs.docker.com/reference/compose-file/
- **macvlan / ipvlan networking:** https://docs.docker.com/engine/network/drivers/macvlan/
- **`llms.txt` (use first):** https://docs.docker.com/llms.txt
- **MCP catalog/toolkit:** https://docs.docker.com/ai/mcp-catalog-and-toolkit/
- **Version check:** `docker version`, `docker compose version`.

## Dockhand + Hawser

- **Dockhand repo:** https://github.com/Finsys/dockhand
- **Dockhand site/docs:** https://dockhand.pro/ ; https://finsys-dockhand.mintlify.app/
- **Hawser repo (Go agent):** https://github.com/Finsys/hawser
- **MCP server:** https://github.com/strausmann/mcp-dockhand (`ghcr.io/strausmann/mcp-dockhand:latest`)
- Note: formal OpenAPI spec was still in progress as of early 2026 — verify current state; the MCP server is
  the most reliable programmatic surface.

## Portainer

- **Repo:** https://github.com/portainer/portainer
- **API docs (version-aware):** https://docs.portainer.io/api/docs ; https://api-docs.portainer.io/
- **Official MCP:** https://github.com/portainer/portainer-mcp (match version to your Portainer, e.g. `~=2.42`)

## Dockge

- **Repo:** https://github.com/louislam/dockge (Socket.IO internal API; file-based stacks in `/opt/stacks`)
- No official MCP server exists.

## Nginx Proxy Manager + label sync

- **NPM repo:** https://github.com/NginxProxyManager/nginx-proxy-manager
- **npm-docker-sync (maintainer's own label→NPM tool):** https://github.com/Redth/npm-docker-sync

## Proxmox + Docker networking/storage references

- VLAN-aware bridges, macvlan/ipvlan, privileged vs unprivileged LXC for Docker, storage passthrough — start
  from the Proxmox wiki + Docker macvlan docs above; the `docker:docker-on-proxmox` skill summarizes the
  current patterns and links back here.

## llms.txt — general

- Standard explainer & adopter list: https://llmstxt.org/ — check whether a project has added an `llms.txt`
  before falling back to HTML docs.
