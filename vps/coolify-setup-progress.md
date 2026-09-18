# Coolify Setup — Progress Log

**Server:** `200.234.37.191` (srv1903336)

---

## What's Installed

Coolify v4.3.21, installed via official script. Runs as its own Docker stack, isolated from existing containers.

### Coolify containers
| Container | Purpose | Port |
|---|---|---|
| `coolify` | Main app/dashboard | `8000` (host) → `8080` (internal) |
| `coolify-db` | Postgres (Coolify's own data) | `5432` (internal only) |
| `coolify-redis` | Redis (queues/cache) | `6379` (internal only) |
| `coolify-realtime` | Websockets | `6001-6002` |
| `coolify-sentinel` | Server monitoring agent | internal |

**Dashboard URL:** `http://200.234.37.191:8000`

---

## Firewall Changes

- Opened `8000/tcp` (ufw) — needed for dashboard access
- Ports `22`, `80`, `443` were already open (pre-existing)

---

## Server Registration

- Added as **"This machine"** (localhost) — Coolify manages the same VPS it's installed on
- Server status: Ready
- **Proxy: Exited** — expected, not an issue. Coolify's built-in proxy (Traefik) wants ports 80/443, which Nginx already holds. Left as-is on purpose. Will revisit if/when we let Coolify handle a domain directly.

---

## Project Structure

- Project: **`ARDH-Test`**
- Environment: **`production`** (default name, not renamed yet)
- Resources so far: **0** (in progress)

---

## Git

- GitHub App connected (Sources → GitHub)

---

## Existing Infrastructure (untouched, unaffected)

- Nginx — still on 80/443, serving `ardh.co.in` and `api.ardh.co.in`
- `ardh-api` container — `127.0.0.1:8080`, unchanged
- `ardh-db` container — MSSQL, unchanged
- Full details in `ardh-vps-current-state.md`

---

## Status: In Progress

Currently figuring out the right resource type in Coolify to deploy the backend (Dockerfile + Compose from Git) — plain "Docker Compose" paste-in isn't the right fit since it doesn't pull from GitHub directly. Next: find the correct Git-based deployment option.
