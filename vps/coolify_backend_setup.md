# Coolify Backend Setup — Test Deployment (dev-api.ardh.co.in)

**Server:** `200.234.37.191` (srv1903336)
**Date:** Sep 18, 2026
**Status:** ✅ Complete and verified working

Goal: deploy the backend to a subdomain via Coolify to validate the setup, without touching the live `ardh.co.in` / `api.ardh.co.in` / `ardh-frontend` / `ardh-api` / `ardh-db` stack in any way. This was done before the frontend test deployment (see `coolify_frontend_setup.md`).

---

## Repo changes (backend repo)

- **`docker-compose.yml`** — parameterized so a second, isolated stack can run alongside prod on the same Docker host:
  - `container_name` for both services now `${API_CONTAINER_NAME:-ardh-api}` / `${DB_CONTAINER_NAME:-ardh-db}`
  - `ports` now `${API_HOST_PORT:-127.0.0.1:8080}:8080`
  - `BaseURL` now `${BASE_URL:-https://api.ardh.co.in}`
  - All defaults match current prod exactly — prod's own deployment is unaffected unless these vars are explicitly set.
  - Added `AdminSettings__BootstrapEmail` / `AdminSettings__BootstrapPassword` env mappings (see seed-data section below).
- **`ApplicationDbContextInitializer.cs`** — stripped the entire demo dataset (buildings, tenants, income/expense records, etc., ~1200 lines) and the `SEED_MODE=reset/wipe/none` env var branching. Kept: EF migrations, the legacy-permissions repair (real data migration, not demo data), and a minimal bootstrap that creates **one** admin user only if the database is completely empty.
- **`AppSettings.cs`** — added `AdminSettings.BootstrapEmail` / `BootstrapPassword`, defaulting to obvious placeholders (`admin@example.com` / `ChangeMe123!`) for local dev only.
- **`deploy/reset-db.sh`** — deleted (only existed to drive `SEED_MODE`).
- **`deploy.env.example`** — added `BOOTSTRAP_ADMIN_EMAIL` / `BOOTSTRAP_ADMIN_PASSWORD` and the test-deploy override vars (`BASE_URL`, `API_HOST_PORT`, `API_CONTAINER_NAME`, `DB_CONTAINER_NAME`).
- **`README.md` / `POSTMAN_AND_FRONTEND_CHANGES.md` / `postman/Ardh_Postman_Collection.json`** — corrected to stop documenting the old seeded demo accounts (`admin@admin.com`/`manager@gmail.com`/etc.), now point at the bootstrap env vars instead.

---

## Security fix along the way (worth remembering)

First pass at the admin bootstrap hardcoded a real email/password directly into `ApplicationDbContextInitializer.cs`. Caught before it reached GitHub — committing a real credential to git history means it's there forever, readable by anyone with repo access, even after later "fixing" it. Refactored to `BOOTSTRAP_ADMIN_EMAIL` / `BOOTSTRAP_ADMIN_PASSWORD` env vars instead (same pattern as `ADMIN_PANEL_PASSWORD`), with only a non-real placeholder compiled into source for local-dev fallback. **Rule going forward: no real credential ever goes into a `.cs`/`.md`/`.json` file, only into env vars set on the server or in Coolify's dashboard.**

---

## Git / repo routing (same gotcha as frontend)

Three remotes on this repo too:

| Remote | URL | Role |
|---|---|---|
| `origin` | `github.com/ArunRoy404/ARDH-Backend` | Main dev remote, branch `roy` |
| `org` | `github.com/TECHREION/ARDH-Backend` | Org mirror |
| `deploy` | `github.com/ROY-0404/ARDH-Backend` | **This is what Coolify's GitHub App is connected to** |

Coolify pulls from `ROY-0404/ARDH-Backend`, branch **`main`** — same rule as frontend: confirm the commit you expect is actually on `deploy`'s `main` before relying on a Coolify rebuild, don't assume `origin` and `deploy` are in sync.

---

## Coolify resource

- **Project:** `ARDH-Test` → **Environment:** `production`
- **Resource name:** `a-r-d-h--backend:main-td8e4wtlefrrgsvhzmv1o1w0`
- **Source:** Private GitHub App → `ROY-0404/ARDH-Backend`, branch `main`
- **Build pack:** Docker Compose, location `/docker-compose.yml`

### Networking
- Coolify **overrides `container_name` entirely** with its own auto-generated unique names (`db-td8e4wtlefrrgsvhzmv1o1w0-...`, `api-td8e4wtlefrrgsvhzmv1o1w0-...`) regardless of what `DB_CONTAINER_NAME`/`API_CONTAINER_NAME` resolve to — actually safer than what we asked for, guaranteed no collision with `ardh-api`/`ardh-db` either way.
- `ports` **does** respect `API_HOST_PORT` — resolved to `127.0.0.1:8081:8080` via the `.env` file Coolify writes on the server (confirmed via `docker ps`: `127.0.0.1:8081->8080/tcp`).
- Domains/FQDN/sslip.io auto-domain: **left unused**, same reasoning as frontend — Coolify's Traefik proxy isn't fronting 80/443 on this server, host Nginx handles public routing instead.

### Environment Variables (names only — real values live only in Coolify's dashboard, never in this repo)
```
MSSQL_SA_PASSWORD
BASE_URL                    = https://dev-api.ardh.co.in
IDENTITY_KEY
MAIL_SMTP_HOST / PORT / USE_SSL / USERNAME / PASSWORD
MAIL_FROM
ADMIN_PANEL_PASSWORD
BOOTSTRAP_ADMIN_EMAIL
BOOTSTRAP_ADMIN_PASSWORD
API_HOST_PORT               = 127.0.0.1:8081
API_CONTAINER_NAME          = ardh-api-test   (cosmetic only, see note above)
DB_CONTAINER_NAME           = ardh-db-test    (cosmetic only, see note above)
```

---

## The bug we hit: 3 vars Coolify didn't auto-detect

**Symptom:** After clicking "Load Compose", Coolify's Environment Variables screen auto-populated 12 variables — but not `API_HOST_PORT`, `API_CONTAINER_NAME`, or `DB_CONTAINER_NAME`.

**Root cause:** Coolify's compose-variable auto-detection only scans `${VAR}` references inside each service's `environment:` block. Our three missing vars are used in `ports:` and `container_name:` instead, which it doesn't scan — so they never got a UI field.

**Fix:** manually added all three via **+ Add** in the Environment Variables screen. Confirmed via **Show Deployable Compose** that the rendered file still referenced them correctly, and confirmed post-deploy via `docker ps` that the port bound to `8081` as expected (container naming turned out to be moot either way — see Networking note above).

---

## Host Nginx vhost: `dev-api`

File: `/etc/nginx/sites-available/dev-api`, symlinked into `/etc/nginx/sites-enabled/`. Copied from the existing `ardh-api` vhost, then edited:

```nginx
server {
    server_name dev-api.ardh.co.in;

    client_max_body_size 25m;

    location / {
        proxy_pass http://127.0.0.1:8081;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

**Gotcha hit:** copying `ardh-api`'s file brought over its Certbot-managed HTTP→HTTPS redirect block too, which still said `server_name api.ardh.co.in;` and `if ($host = api.ardh.co.in)` — a leftover duplicate `server_name` that would've conflicted with the real `ardh-api` vhost on port 80. Caught and fixed both occurrences before running Certbot.

Certbot (`sudo certbot --nginx -d dev-api.ardh.co.in`) then rewrote the file in place to add the real HTTPS block + redirect.

---

## DNS

- `dev-api.ardh.co.in` → `A` → `200.234.37.191` (added via Hostinger DNS panel)

---

## Final verified state

| Piece | Value |
|---|---|
| Backend container | `127.0.0.1:8081 → 8080` (Coolify-managed, `api-td8e4wtlefrrgsvhzmv1o1w0-...`) |
| DB container | internal only, `1433` not host-mapped (Coolify-managed, `db-td8e4wtlefrrgsvhzmv1o1w0-...`, healthy) |
| Public URL | `https://dev-api.ardh.co.in` (Let's Encrypt SSL, auto-renews) |
| Live prod | `ardh-api` (`127.0.0.1:8080`), `ardh-db` — confirmed running, untouched, throughout |

### How it was verified
1. `docker ps` — confirmed new containers on `8081`, prod's `ardh-api`/`ardh-db` still up on `8080` unchanged.
2. `curl -i http://127.0.0.1:8081` → `404`/Kestrel — confirmed the .NET app itself is alive (no root route is expected/normal).
3. `curl -i -X POST https://dev-api.ardh.co.in/api/auth/sign-in ...` → `200 OK`, JWT cookie issued for the bootstrap admin, permissions correct.
4. Later, cross-verified via the frontend test deployment: `docker logs -f` on this exact container while using `https://dev.ardh.co.in` in a browser showed real requests landing here, against the dev DB, not prod's.

---

## Open items / not done yet

- **DB migration**: copy a backup of the live prod database (`ardh-db`) into this test stack's DB (`db-td8e4wtlefrrgsvhzmv1o1w0-...`), replacing the bootstrap-only empty database with real (test-safe) data. Plan: `deploy/backup-db.sh` on prod (read-only, non-destructive) → `docker cp` the `.bak` into the test DB container → `RESTORE DATABASE ... WITH REPLACE` inside that container only. Not started yet.
- Cutover to live domains (`ardh.co.in`, `api.ardh.co.in`) — **on hold, requires client approval**, per the migration plan in `ardh-vps-current-state.md`.
- Coolify's own Traefik proxy remains "Exited" / not fronting 80/443 — intentional, host Nginx stays the single entry point for now.
