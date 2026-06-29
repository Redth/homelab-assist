---
name: npm-reverse-proxy
description: Set up and manage domain routing for homelab services using nginx-proxy-manager (NPM) driven by Docker container labels via npm-docker-sync. Load this when the user wants to expose a container/stack at a domain, debug why a service isn't routing, or understand the label-based proxy-host automation pattern. Covers the npm.* label scheme, how sync maps containers to NPM proxy hosts, and SSL/forced-host details.
---

# NPM reverse-proxy routing via container labels

This homelab routes domains to containers using **nginx-proxy-manager** (NPM) as the reverse proxy, with
**`Redth/npm-docker-sync`** (https://github.com/Redth/npm-docker-sync) watching Docker container **labels** and
auto-creating/updating the matching NPM **proxy hosts**. Obey
the `homelab-core:homelab-safety` skill. This is the maintainer's own tool —
treat its repo/README as canonical and verify the exact label keys there (`homelab-research`), since they may
evolve.

## Direct NPM control: the bundled `npm` MCP server

This plugin ships its own MCP server (built in this repo, `src/NpmMcp`, distributed as a Native AOT binary —
no runtime needed). When it's configured (set `NPM_URL`, `NPM_EMAIL`, `NPM_PASSWORD`, and `NPM_VERIFY_SSL=0`
for self-signed certs — see the plugin `.mcp.json`), prefer its tools for inspecting and changing NPM
directly:

- `npm_list_proxy_hosts`, `npm_get_proxy_host` — see what's routed (read).
- `npm_debug_routing <domain>` — **the fastest way to debug "why isn't this routing"**: it finds the matching
  proxy/redirect host and reports upstream host:port, enabled/SSL state, and cert, with a triage checklist.
- `npm_list_certificates` / `npm_get_certificate` — find a `certificate_id`, spot expiring certs.
- `npm_list_redirection_hosts`, `npm_get_redirection_host` — inspect redirects.
- `npm_create_proxy_host`, `npm_update_proxy_host`, `npm_enable/disable/delete_proxy_host` — changes; these
  are significant/destructive, so apply the `homelab-core:homelab-safety` gate (summarize + confirm) first.

Note the two complementary approaches: **npm-docker-sync** keeps routing declarative in the container labels
(the steady-state pattern below), while the **`npm` MCP tools** let you inspect and make direct/ad-hoc NPM
changes and debug routing. For label-managed hosts, prefer fixing the labels + re-syncing over hand-editing
via the API, to avoid drift.

## The label pattern

```
container (with npm.* labels)  ──►  npm-docker-sync (reads labels, calls NPM API)  ──►  NPM proxy host  ──►  service reachable at https://<domain>
```

To expose a service you usually **don't** click around the NPM UI — you add labels to the container/service in
its compose file, and sync reconciles NPM to match. This keeps routing **declarative and in the compose
definition** alongside the service.

## Label scheme (verify exact keys against the npm-docker-sync README)

Typical labels (confirm names/format in the repo — this is the shape, not a frozen spec):

```yaml
services:
  myapp:
    image: ...
    labels:
      - "npm.proxy.domains=app.example.com"      # one or more hostnames to route
      - "npm.proxy.ssl_force=true"               # force HTTPS / request a cert
      - "npm.proxy.block_exploits=true"          # NPM "block common exploits"
      # port is often auto-detected from EXPOSE / container; an explicit override label may exist
```

`npm-docker-sync` is configured with how to reach NPM: `NPM_URL`, `NPM_EMAIL`, `NPM_PASSWORD`, and the NPM
container name (`NPM_CONTAINER_NAME`) so it can resolve the upstream. It needs Docker socket access to read
labels. **Never commit those credentials.**

## To add routing for a service (significant change)

1. Confirm the desired hostname(s) and that DNS for them points at NPM's host/IP.
2. Add the `npm.*` labels to the service in its compose/stack (via the control plane — Dockhand/Portainer/
   Dockge as appropriate). Show the diff.
3. Redeploy the stack so the labels take effect; sync will create/update the NPM proxy host.
4. Verify: the proxy host appears in NPM, the cert issued (if forced), and `https://<domain>` reaches the
   service. Don't assume — check.

## Debugging "it isn't routing"

Work the chain end to end:

- **DNS:** does the hostname resolve to NPM's IP? (split-horizon/local DNS is a common gotcha)
- **Labels:** are the `npm.*` labels present and correctly formatted on the running container? (`docker inspect`)
- **Sync:** is `npm-docker-sync` running, can it see the Docker socket, and are its NPM creds valid? Check its
  logs.
- **NPM proxy host:** did it get created? Is the **upstream host:port** correct and reachable from the NPM
  container (same Docker network / correct IP)? Forwarding to the wrong network is common — see
  [`docker-on-proxmox`](../docker-on-proxmox/SKILL.md) for network/VLAN reachability.
- **Cert:** for forced SSL, did the ACME/Let's Encrypt challenge succeed (port 80 reachable, DNS public if
  using HTTP-01)?
- **Upstream health:** is the target container actually up and serving on the expected port?

## Reference

[`reference/npm-labels.md`](../../reference/npm-labels.md) for the label list and an example service. Always
reconcile against the npm-docker-sync README for current label keys.
