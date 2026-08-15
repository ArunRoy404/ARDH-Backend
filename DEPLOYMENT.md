# ARDH Backend — Live Deployment Guide

This documents the production deployment of the ARDH backend on Hostinger, what's
running where, and the exact steps for common maintenance tasks. Keep this file
up to date if the setup changes.

---

## 1. What's running

| Thing | Value |
| :--- | :--- |
| Server | Hostinger KVM 2 VPS (`srv1903336.hstgr.cloud`) |
| Public IP | `200.234.37.191` |
| API domain | `https://api.ardh.co.in` (Let's Encrypt SSL, auto-renews) |
| OS | Ubuntu (Docker + Nginx installed directly on the host) |
| Repo path on server | `~/ARDH-Backend` |
| Default admin login | `admin@gmail.com` / `P@ssw0rd` — **change this for real use** |

**Architecture:**

```
Internet → Nginx (host, port 80/443, SSL termination)
              → 127.0.0.1:8080 → Docker container "ardh-api" (.NET 8 API)
                                       → Docker container "ardh-db" (SQL Server 2022)
```

- `ardh-api` and `ardh-db` are managed by [docker-compose.yml](docker-compose.yml) at the repo root.
- The API container is **not** exposed to the internet directly — only Nginx is, and it
  reverse-proxies to `127.0.0.1:8080`. Same for the database: it has no public port at all.
- Two Docker volumes persist data across restarts/rebuilds: `mssql-data` (the database
  files) and `api-uploads` (files uploaded through the app, served at `/image`).
- Secrets (DB password, JWT key, mail API key, admin password) live in a `.env` file in
  `~/ARDH-Backend` **on the server only** — never committed to git. Template: [deploy.env.example](deploy.env.example).
- Nginx site config: `/etc/nginx/sites-available/ardh-api` (copied from
  [deploy/nginx-ardh-api.conf.example](deploy/nginx-ardh-api.conf.example), then edited by Certbot to add SSL).

---

## 2. Updating the backend (normal code changes)

On your **local machine**:
```bash
git add .
git commit -m "..."
git push
```

On the **server**:
```bash
cd ~/ARDH-Backend
git pull
docker compose up -d --build
```

This rebuilds only the `api` image and recreates that container — the database
container and its data are untouched. Any new EF Core migrations apply automatically
on startup, same as local dev.

**Verify:**
```bash
docker compose ps
docker compose logs -f api
```
`Ctrl+C` to stop tailing logs (containers keep running).

If you change `docker-compose.yml` or `.env` itself, the same `up -d --build` picks up
the new values.

---

## 3. Container cheatsheet

Run from `~/ARDH-Backend` on the server.

| Task | Command |
| :--- | :--- |
| See status | `docker compose ps` |
| Tail logs (API) | `docker compose logs -f api` |
| Tail logs (DB) | `docker compose logs -f db` |
| Restart API only | `docker compose restart api` |
| Stop everything | `docker compose down` (data is safe — volumes aren't deleted) |
| Start everything | `docker compose up -d` |
| Shell into API container | `docker compose exec api bash` |
| Shell into DB container | `docker compose exec db bash` |

`restart: unless-stopped` is set on both services, so they also survive a server reboot
automatically.

---

## 4. Database operations

All of these run **on the server**, from `~/ARDH-Backend`. The database has no local
`sqlcmd`/SSMS access from outside Docker — everything goes through the `db` container.

### 4.1 Ad-hoc SQL query
```bash
docker compose exec db bash -c 'sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT COUNT(*) FROM users"'
```
(The container already has `MSSQL_SA_PASSWORD` set in its own environment, so this
doesn't need your `.env` sourced into the shell.)

### 4.2 Backup
```bash
./deploy/backup-db.sh
```
Creates a timestamped `.bak` file in `./backups/` on the server (gitignored — these
never get committed). Copy it off the server periodically (e.g. `scp`) for real
disaster-recovery safety — a backup that only lives on the same VPS as the database
doesn't protect against losing the VPS.

### 4.3 Restore
```bash
./deploy/restore-db.sh backups/ArdhDb-20260815-190642.bak
```
Prompts for confirmation (this **replaces** all live data).

### 4.4 Full reset / wipe (demo data)
Mirrors the `SEED_MODE` behavior described in [DATABASE_GUIDE.md](DATABASE_GUIDE.md), adapted for Docker:
```bash
./deploy/reset-db.sh reset   # wipe everything, re-seed full demo dataset
./deploy/reset-db.sh wipe    # wipe everything, keep only the admin user
```
Prompts for confirmation. The script automatically clears the `SEED_MODE` flag
afterward — if you instead set `SEED_MODE` by hand and forget to unset it, **every**
future restart (including server reboots) will wipe the database again.

### 4.5 Adding a user
Prefer the API method (Part 4.1 in [DATABASE_GUIDE.md](DATABASE_GUIDE.md)) — sign in as
admin, `POST /api/users`. The raw-SQL method in that guide (Part 4.2) needs a BCrypt
hash generated via a throwaway .NET project; only bother with it if the API is
unreachable for some reason.

### 4.6 Migrations
No manual step needed — `Database.MigrateAsync()` runs automatically every time the
`api` container starts (see section 2). You only touch `dotnet ef` locally, when
creating a new migration during development; the server just needs the migration
files pulled in via `git pull` and picks them up on the next deploy.

---

## 5. SSL / domain maintenance

- Certificates auto-renew via a systemd timer Certbot installed; nothing to do normally.
- Check the timer exists: `systemctl list-timers | grep certbot`
- Test renewal without actually renewing: `sudo certbot renew --dry-run`
- Force a renewal: `sudo certbot renew --force-renewal`
- Certs live in `/etc/letsencrypt/live/api.ardh.co.in/`

To add another subdomain later (e.g. for the React frontend), see the "Later" section
of the original setup notes — copy `deploy/nginx-ardh-api.conf.example` as a template,
swap the domain, `certbot --nginx -d <new-domain>`.

---

## 6. Nginx cheatsheet

| Task | Command |
| :--- | :--- |
| Test config for syntax errors | `sudo nginx -t` |
| Reload after editing a config | `sudo systemctl reload nginx` |
| View the live site config | `cat /etc/nginx/sites-available/ardh-api` |
| Nginx error log | `sudo tail -f /var/log/nginx/error.log` |
| Nginx access log | `sudo tail -f /var/log/nginx/access.log` |

---

## 7. Troubleshooting

| Symptom | Check |
| :--- | :--- |
| `502 Bad Gateway` from the domain | `docker compose ps` — is `ardh-api` up? `docker compose logs api` for a crash. |
| API container keeps restarting | `docker compose logs api` — usually a bad/missing value in `.env`, or the DB not healthy yet. |
| `db` container unhealthy | `docker compose logs db` — often means `MSSQL_SA_PASSWORD` doesn't meet SQL Server's complexity rules. |
| DNS not resolving | `nslookup api.ardh.co.in` from your own machine, not the server. |
| Cert renewal worried you | `sudo certbot certificates` shows expiry dates for everything currently issued. |

---

## 8. Secrets reference

`.env` on the server (never in git) holds:

| Key | What it's for |
| :--- | :--- |
| `MSSQL_SA_PASSWORD` | SQL Server `sa` login password |
| `IDENTITY_KEY` | JWT signing key |
| `RESEND_API_KEY` | Transactional email (Resend) |
| `ADMIN_PANEL_PASSWORD` | Required for `X-Admin-Password`-gated admin endpoints |

Rotating any of these: edit `.env`, then `docker compose up -d --build` to pick up the
new values (existing user sessions signed with the old `IDENTITY_KEY` will be
invalidated, so a JWT key rotation logs everyone out).
