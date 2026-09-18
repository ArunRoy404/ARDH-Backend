# ARDH VPS — Current Infrastructure (as of Sep 18, 2026)

**Server:** `200.234.37.191` (hostname `srv1903336`)
**OS:** Ubuntu 24.04.4 LTS
**Access:** root via SSH, Hostinger panel access

---

## System Resources

| Resource | Total | Used | Available |
|---|---|---|---|
| RAM | 7.8 GiB | 1.5 GiB | 6.3 GiB |
| Swap | 0 B | 0 B | 0 B |
| Disk (`/`) | 96 GB | 4.8 GB | 92 GB |

Plenty of headroom to run Coolify + Docker alongside the existing stack.

**Docker:** already installed — v29.7.2

---

## Network / Firewall (ufw)

Currently open:
- 22 (SSH)
- 80 (HTTP)
- 443 (HTTPS)

Port 8000 (Coolify dashboard) will need to be opened when Coolify is installed.

---

## Web Server: Nginx

Running as a systemd service (`nginx.service`), active, bound to host ports 80/443 (both IPv4 and IPv6). This is the **sole entry point** for all traffic — nothing else is exposed directly to the internet.

### Sites enabled
- `ardh-frontend` → serves `ardh.co.in` / `www.ardh.co.in`
- `ardh-api` → serves `api.ardh.co.in`
- `default`

### `ardh-frontend` config
- Domain: `ardh.co.in`, `www.ardh.co.in`
- Root: `/var/www/ardh-frontend` (static files, `index.html` entry)
- SPA fallback: `try_files $uri /index.html;`
- `/api/` path proxied to `127.0.0.1:8080` (backend)
- SSL via Certbot (Let's Encrypt), auto-managed
- HTTP → HTTPS redirect enforced

### `ardh-api` config
- Domain: `api.ardh.co.in`
- Proxies all traffic to `127.0.0.1:8080`
- SSL via Certbot, HTTP → HTTPS redirect enforced
- Standard proxy headers set (X-Real-IP, X-Forwarded-For/Proto, Upgrade/Connection for websockets)

---

## Frontend Deployment (current)

- **Method:** manual — local `npm run build` → `tar` → `scp` → SSH extract into `/var/www/ardh-frontend`, ownership set to `www-data:www-data`
- **Not containerized** — plain static files served directly by host Nginx
- Last deployed: Sep 17, 2026 15:00 UTC
- No build/deploy automation on the server side; all done via the local `.bat` script

---

## Backend Deployment (current)

**Fully containerized** via Docker Compose, defined at `/root/ARDH-Backend/`:
- `docker-compose.yml`
- `Dockerfile`

### Containers

| Name | Image | Status | Ports | Restart Policy |
|---|---|---|---|---|
| `ardh-api` | `ardh-backend-api` (built locally from Dockerfile) | Up 5 days | `127.0.0.1:8080→8080` (localhost only) | `unless-stopped` |
| `ardh-db` | `mcr.microsoft.com/mssql/server:2022-latest` | Up 5 days (healthy) | internal only (1433, not host-mapped) | `unless-stopped` |

Both containers survive a VPS reboot automatically (`unless-stopped` policy).

### Compose file structure
- **`db` service:** MSSQL 2022 Express edition, SA password via env var (`MSSQL_SA_PASSWORD`), data persisted in named volume `mssql-data`, healthcheck via `sqlcmd`
- **`api` service:** builds from local `Dockerfile`, depends on `db` being healthy before starting, connects to DB via Docker's internal DNS (`Server=db;...`), config driven entirely by environment variables (mail SMTP, identity key, admin password, base URL, seed mode), uploads persisted in named volume `api-uploads`, bound **only** to `127.0.0.1:8080` — never exposed directly to the internet, Nginx is the sole proxy in front of it

### Environment variables required (backend)
- `MSSQL_SA_PASSWORD`
- `IDENTITY_KEY`
- `MAIL_SMTP_HOST`, `MAIL_SMTP_PORT`, `MAIL_SMTP_USE_SSL`, `MAIL_SMTP_USERNAME`, `MAIL_SMTP_PASSWORD`, `MAIL_FROM`
- `ADMIN_PANEL_PASSWORD`
- `SEED_MODE` (optional, only for one-off maintenance)

---

## Assessment for Coolify Migration

- Backend is already well-structured for Coolify import (existing Dockerfile + compose file, env-var driven config) — should require little to no rewriting
- Frontend will need a Dockerfile written (currently plain static files, no container) — straightforward static build + Nginx-in-container setup
- No conflicts expected from installing Coolify itself: its dashboard defaults to port 8000 (currently free), and it runs in its own isolated Docker network — won't interact with `ardh-api` / `ardh-db` unless explicitly configured to
- Real port/domain conflict only arises **later**, when Coolify's own proxy would need to take over 80/443 for the live domains — this is the planned final cutover step, done only after test deployments (on subdomains) are verified and client has approved

---

## Migration Plan (agreed)

1. Install Coolify on the same VPS — isolated, no interference with current live traffic
2. Deploy frontend + backend to **test/dev subdomains** via Coolify, verify fully working
3. **(On hold — requires client approval)** Cut over: point main domains to Coolify-managed deployments, retire the old Nginx-served frontend and the old `ardh-api`/`ardh-db` containers
