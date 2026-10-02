# Production Deployment Log

Tracks every deployment to the **live production backend** (`ardh-api` / `ardh-db` containers on the VPS, `200.234.37.191`, serving `api.ardh.co.in`). This is separate from the Coolify test deployment (`dev-api.ardh.co.in`) — see `coolify_backend_setup.md` for that.

Purpose: so the *next* deployment has full context without having to reconstruct it from memory or chat history — what was deployed, what backup exists to roll back to, whether anything was changed directly in the DB outside of code/migrations, and how it was verified.

## Process for every deployment

1. **Before touching anything:** take a fresh backup on the VPS and record it here immediately (even before the deploy itself), so it's on record even if the deploy goes wrong:
   ```bash
   DATE=$(date +%Y-%m-%d)
   mkdir -p backups
   N=1
   while [ -f "backups/ArdhDb-${DATE}-${N}.bak" ]; do N=$((N+1)); done
   FILE="ArdhDb-${DATE}-${N}.bak"
   docker exec ardh-db bash -c "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -C -Q \"BACKUP DATABASE ArdhDb TO DISK = N'/var/opt/mssql/backup/${FILE}' WITH FORMAT, INIT\""
   docker cp ardh-db:/var/opt/mssql/backup/${FILE} backups/${FILE}
   ```
2. Push the reviewed code to `deploy:main` (or whichever remote/branch the VPS tracks).
3. On the VPS: `cd /root/ARDH-Backend && git pull` — **confirm it says `Fast-forward` or shows a real diff, not an error.** (See the 2026-09-22 entry below for what happens if you skip this check.)
4. `docker compose build api && docker compose up -d api`
5. `docker logs -f ardh-api` — confirm clean startup, no leftover errors, migrations apply if expected.
6. Verify: `docker ps` (container fresh/healthy), `curl https://api.ardh.co.in/healthz`, and at least one authenticated smoke-test request against a route that changed.
7. Fill in the rest of the entry below.

## Entry template

```
### YYYY-MM-DD — <short description>
- **Backup taken:** `ArdhDb-YYYY-MM-DD-N.bak` (on VPS at `~/backups/`)
- **Commits deployed:** `<short-sha>..<short-sha>` on `main`
- **Migrations applied:** <list, or "none pending">
- **Manual DB changes (outside of code/migrations):** <describe with row counts, or "none">
- **Verification:** <what was actually checked>
- **Issues encountered:** <none, or describe + how it was resolved>
```

---

## Entries (newest first)

### 2026-10-02 — Dashboard range-filter endpoints
- **Backup taken:** `ArdhDb-2026-10-02-1.bak`
- **Commits deployed:** `250ae67..e7c1fc7` on `main` (fast-forward, clean)
- **Changed:** Removed `monthlyIncome`/`monthlyExpense` from `GET /api/dashboard/stats`; added `GET /api/dashboard/income-stat` and `expense-stat`; added `startDate`/`endDate` range params to `occupancy` and `expense-breakdown`. Postman collection updated to match.
- **Migrations applied:** None — this change has no schema impact.
- **Manual DB changes:** None.
- **Verification:** `docker ps` showed `ardh-api` freshly started and healthy; `ardh-db` untouched (9-day uptime, unaffected); `/healthz` → `Healthy`; `GET /api/dashboard/stats` → `401` (confirms route live and auth-gated, not a 404). Full authenticated click-through on the live site was **not** done — only infra-level verification.
- **Issues encountered:** None this time. (See below for the prior attempt's issue, which this deploy's clean `git pull` output confirms is now resolved.)

### (date unknown — between 2026-09-22 and 2026-10-02) — Date-only column fix, seed/bootstrap removal, and SAR fix actually reached prod
- **What:** Commit `250ae67` on `main` (the base for the 2026-10-02 deploy above) already contained: the `FixDateOnlyColumns` EF migration (converts `AmcContract.StartDate/EndDate`, `Equipment.InstallDate/WarrantyExpiryDate`, `ExpenseRecord.ExpenseDate`, `IncomeRecord.PaymentDate`, `MaintenanceRequest.ScheduledDate/StartDate/LastCompletedDate` from `datetime2` to `date`), the `MaintenanceRequestService.cs` fix (`DateTime.UtcNow` → `DateTime.UtcNow.Date` for `LastCompletedDate`), and the full removal of all seed/bootstrap/admin-default code.
- **Migrations applied:** `FixDateOnlyColumns` (confirmed already applied as of the 2026-10-02 check — `git pull`/EF log showed "No migrations were applied, the database is already up to date" for this migration, meaning it ran successfully at some point before that check).
- **Note:** This conversation's record of *how* this deploy actually succeeded is incomplete — the 2026-09-22 attempt below failed (ran stale code), and the next time this was checked (2026-10-02), `main` was already at `250ae67` with the migration applied. The gap was resolved outside what's visible here. **If you need the exact steps/timing, check shell history on the VPS or ask whoever ran it.**

### 2026-09-22 — Prod deploy attempt #1 — **FAILED, ran stale code** (cautionary entry, keep for reference)
- **Backup taken:** `ArdhDb-2026-09-22-1.bak`
- **Manual DB changes:** Fixed historical "SAR" currency text directly in prod data (pre-existing rows written before the SAR→INR code fix): `UPDATE` on `activities.Description` (31 rows), `notifications.Detail` (31 rows), `deleted_history.EntityTitle` (13 rows) — replaced literal `' SAR'` substring with `' INR'`. Verified 0 remaining via preview query before and after. This data fix is independent of the code deploy and was **not** affected by the failure below.
- **What went wrong:** `git pull` on the VPS **aborted** with `error: Your local changes to the following files would be overwritten by merge: deploy/reset-db.sh` — the working tree was never actually updated. `docker compose build && up` then ran anyway, rebuilding the **same old code** from the stale working tree. This was only caught by noticing the startup log still contained the old `SeedSettings()` / `EXISTS (SELECT 1 FROM settings)` query pattern, which had already been deleted from source.
- **Lesson:** Always confirm `git pull` actually says `Fast-forward` (or shows a real file diff) before rebuilding — a failed/aborted pull followed by `docker compose build` will silently rebuild stale code with no error at the build step. Check the startup log for something recognizable from the commit you expect to be live, not just "no errors."
- **Resolution:** Not resolved within this conversation at the time — see the entry above; it was working again by 2026-10-02.

### 2026-09-18 — Initial Coolify test-subdomain deployment
- See `coolify_backend_setup.md` for full detail (separate stack: `dev-api.ardh.co.in`, isolated containers, does not affect prod `ardh-api`/`ardh-db`).
