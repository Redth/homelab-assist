# nginx-proxy-manager label routing (reference)

Driven by **`Redth/npm-docker-sync`** (https://github.com/Redth/npm-docker-sync). **This is the maintainer's
own tool — its README is canonical; verify exact label keys/format there**, as they may evolve. The shapes
below illustrate the pattern.

## How it works

`npm-docker-sync` runs as a service with access to the Docker socket. It reads `npm.*` labels on running
containers and calls the NPM API to create/update matching **proxy hosts**, so routing is declared in compose
alongside the service instead of clicked into the NPM UI.

## Sync service configuration (env — never commit)

- `NPM_URL` — base URL of the NPM admin/API
- `NPM_EMAIL` / `NPM_PASSWORD` — NPM login for the API
- `NPM_CONTAINER_NAME` — NPM container name, used to resolve upstream networking
- Docker socket mounted so it can read labels

## Example service with routing labels

```yaml
services:
  jellyfin:
    image: jellyfin/jellyfin
    networks: [proxy]            # must share a network NPM can reach (see docker-on-proxmox)
    expose: ["8096"]            # port often auto-detected from EXPOSE
    labels:
      - "npm.proxy.domains=jellyfin.example.com"
      - "npm.proxy.ssl_force=true"
      - "npm.proxy.block_exploits=true"

networks:
  proxy:
    external: true              # the shared network NPM is attached to
```

Typical label keys (confirm in the repo):

| Label | Meaning |
| --- | --- |
| `npm.proxy.domains` | hostname(s) to route to this container |
| `npm.proxy.ssl_force` | force HTTPS / request a cert |
| `npm.proxy.block_exploits` | enable NPM "block common exploits" |
| (port override) | explicit upstream port if EXPOSE can't be auto-detected |

## Reachability requirement

NPM forwards to the container's **host:port on a network NPM can reach**. The single most common routing bug is
the upstream being on a Docker network NPM isn't attached to (so the proxy host exists but 502s). Put the
service and NPM on a shared network, or use a reachable IP — and on a Proxmox-hosted Docker host, confirm the
network/VLAN actually connects (see `docker-on-proxmox`).

## Verify after changes

1. Proxy host created/updated in NPM.
2. Cert issued (if `ssl_force`) — ACME challenge succeeded.
3. `https://<domain>` reaches the service (200/expected), not a 502 (upstream unreachable) or 404 (wrong host).
