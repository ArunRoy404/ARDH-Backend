# Coolify Backend Setup — Test Deployment (dev-api.ardh.co.in)

**Server:** `200.234.37.191` (srv1903336)
**Date:** Sep 18, 2026
**Status:** ✅ Complete and verified working

Goal: deploy the backend to a subdomain via Coolify to validate the setup, without touching the live `ardh.co.in` / `api.ardh.co.in` / `ardh-frontend` / `ardh-api` / `ardh-db` stack in any way. This was done before the frontend test deployment (see `coolify_frontend_setup.md`).

> **Update (Sep 22, 2026):** The admin-bootstrap mechanism described below (`AdminSettings.BootstrapEmail`/`BootstrapPassword`, `BOOTSTRAP_ADMIN_EMAIL`/`BOOTSTRAP_ADMIN_PASSWORD`, `ADMIN_PANEL_PASSWORD`, and the local-dev fallback added later) has since been **removed entirely**. Startup no longer auto-creates any user, admin account, or Settings row under any circumstance — the database is expected to already have its data via migration/restore, since production is now migrated from an existing real database rather than bootstrapped fresh. This section is kept as a historical record of that deployment, not as current behavior.

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

## Pre-restore safety: disabling test-environment email before touching real data

Before restoring a real prod DB backup into the test stack, we deliberately broke the test environment's SMTP so it could never send a real email to a real person. This mattered because two code paths send real emails using whatever data exists in the DB, regardless of which environment they run in:

1. **`ReminderBackgroundService`** — runs automatically every 24 hours (first run ~10s after container start), sends real emails to real staff users about pending maintenance, AMC contract expiry, and lease expiry. Every attempt is individually wrapped in try/catch (confirmed by reading the code), so a broken SMTP config just logs a warning per failed send — it does not crash the container or block the rest of the scan.
2. **Forgot-password OTP flow** (`AuthService.cs`) — sends a real OTP email on request, with **no** `ReceiveEmailNotifications` gate at all. Anyone triggering "forgot password" against a restored real user's email would get a real OTP in their real inbox.

**Fix applied:** Coolify → backend resource → Environment Variables → set `MAIL_SMTP_PASSWORD` to a blank/garbage value → **Deploy** to actually apply it (an env var edit alone does not propagate to the already-running container — Coolify shows a "Changes pending" banner until you redeploy, and redeploying recreates the container with a **new name suffix**, e.g. `api-td8e4wtlefrrgsvhzmv1o1w0-092131648559` → `-145009061413`; always re-check the current container name via `docker ps` after any redeploy rather than assuming the old one still applies).

**Confirmed safe, not just assumed:** there is no startup validation (`ValidateDataAnnotations`/`ValidateOnStart`) enforcing the `[Required]` attributes on `MailConfigurations` anywhere in the codebase, so a blank SMTP password does not crash the app at startup — it only fails at actual send-time, safely caught and logged.

**Reminder email dedup behavior (for when SMTP is re-enabled later):** each reminder type only writes an `EmailReminderLog` row **after a successful send** (`TrySendEmailAndLogAsync`/`LogEmailSentAsync`). While SMTP is broken, nothing gets marked sent, so the same due items get re-evaluated every scan without ever being marked complete. The moment SMTP works again, the next scan sends **one catch-up email per item still currently due at that moment** — bounded by what's still in its trigger window at restore-time, not multiplied by how many days it was broken, and anything that already rolled past its window during the outage is silently (and correctly) skipped rather than sent late. Already-successfully-sent reminders (their log rows came along with the restored DB) are never resent.

---

## Database restore: prod backup → test DB

**Goal:** validate the test deployment against real (but email-disabled) data, without ever writing to `ardh-db`.

**1. Backup prod** (read-only, non-destructive — `BACKUP DATABASE` runs online, doesn't lock or block writes):
```bash
cd /root/ARDH-Backend
./deploy/backup-db.sh
```
Produces `backups/ArdhDb-<timestamp>.bak` on the host via `docker cp` out of `ardh-db` — a pure read, nothing written back to prod.

**2. Copy the backup into the test DB container** (always re-check the container's current name first — see redeploy note above):
```bash
docker exec <db-container-name> mkdir -p /var/opt/mssql/backup
docker cp backups/ArdhDb-<timestamp>.bak <db-container-name>:/var/opt/mssql/backup/restore.bak
```

**Gotcha #1 — file permissions:** `docker cp` writes the file as root (it goes through the Docker daemon directly, not through an exec'd process), but the MSSQL server process inside the container runs as a non-root `mssql` user, which can't read a root-owned file. `RESTORE DATABASE` failed with `Msg 3201 ... Operating system error 5(Access is denied.)` until fixed:
```bash
docker exec -u root <db-container-name> chmod 644 /var/opt/mssql/backup/restore.bak
```

**Gotcha #2 — `sqlcmd` isn't on PATH:** same as the healthcheck/backup script already know — it's at `/opt/mssql-tools18/bin/sqlcmd`, not just `sqlcmd`.

**3. Restore** (targets the test container explicitly by name — `ardh-db` is never referenced):
```bash
docker exec <db-container-name> bash -c '/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "ALTER DATABASE ArdhDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE ArdhDb FROM DISK = N'"'"'/var/opt/mssql/backup/restore.bak'"'"' WITH REPLACE; ALTER DATABASE ArdhDb SET MULTI_USER;"'
```
Result: `RESTORE DATABASE successfully processed 1178 pages` — matched the backup exactly.

**4. Restart the test api container** so it reconnects cleanly to the restored data:
```bash
docker restart <api-container-name>
```

Note: this restore **overwrites the bootstrap-only admin** (`BOOTSTRAP_ADMIN_EMAIL`/`PASSWORD`) with whatever real users existed in the prod backup — after restoring, log in with a **real prod admin account**, not the bootstrap one.

---

## Final verified state

| Piece | Value |
|---|---|
| Backend container | `127.0.0.1:8081 → 8080` (Coolify-managed, container name regenerates on every redeploy) |
| DB container | internal only, `1433` not host-mapped (Coolify-managed, healthy), now holding a **real restored copy** of prod's data |
| Public URL | `https://dev-api.ardh.co.in` (Let's Encrypt SSL, auto-renews) |
| Test-environment email | Deliberately disabled (`MAIL_SMTP_PASSWORD` blanked) — no real emails can send |
| Live prod | `ardh-api` (`127.0.0.1:8080`), `ardh-db` — confirmed running, untouched, throughout, including through the DB restore |

### How it was verified
1. `docker ps` — confirmed new containers on `8081`, prod's `ardh-api`/`ardh-db` still up on `8080` unchanged.
2. `curl -i http://127.0.0.1:8081` → `404`/Kestrel — confirmed the .NET app itself is alive (no root route is expected/normal).
3. `curl -i -X POST https://dev-api.ardh.co.in/api/auth/sign-in ...` → `200 OK`, JWT cookie issued for the bootstrap admin, permissions correct.
4. Cross-verified via the frontend test deployment: `docker logs -f` on this exact container while using `https://dev.ardh.co.in` in a browser showed real requests landing here, against the dev DB, not prod's.
5. After the DB restore: `RESTORE DATABASE successfully processed 1178 pages`, matching the backup's page count exactly; manually inspected the restored data directly and confirmed it looks correct.
6. Final live re-check immediately after the restore: `docker ps | grep ardh-` showed **identical container IDs** for `ardh-api`/`ardh-db` as at the very start of this whole effort (only uptime incremented) — definitive proof nothing was ever restarted or disrupted, not just inferred from having been careful.

---

## Open items / not done yet

- Cutover to live domains (`ardh.co.in`, `api.ardh.co.in`) — **on hold, requires client approval**, per the migration plan in `ardh-vps-current-state.md`.
- Coolify's own Traefik proxy remains "Exited" / not fronting 80/443 — intentional, host Nginx stays the single entry point for now.
- Test-environment SMTP is intentionally still disabled. Before ever re-enabling it, re-read the "Pre-restore safety" section above — re-enabling will trigger a one-time catch-up batch of reminder emails to whatever real staff are currently due for one.
