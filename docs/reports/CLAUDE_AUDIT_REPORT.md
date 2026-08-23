# ARDH Backend — Claude Deep-Dive Audit Report

- **Generated:** 2026-08-24
- **Scope:** Full read-only review of the backend — Web (controllers, filters, auth), Application (services, validations), Infrastructure (EF Core, migrations, external services), and deployment config (Docker, scripts, appsettings).
- **Status:** Findings only. Nothing in this report has been fixed yet except the one item marked `[FIXED]` below — to be actioned later.

---

## Already fixed (this session)

**Role-casing bug in `[Authorize(Roles = "Admin")]`** — `UserRole` enum values are lowercase (`admin`, `property_manager`, `accountant`, `viewer`) and the JWT role claim is issued as lowercase, but 4 endpoints checked for capital `"Admin"`. ASP.NET Core's role check is case-sensitive against the default role claim type, so these always 403'd for everyone, including real admins.

Fixed in:
- [UserController.cs:16](../../src/CleanArchitecture/Web/Controller/UserController.cs#L16) — whole `/api/users` module
- [DeletedHistoryController.cs:14](../../src/CleanArchitecture/Web/Controller/DeletedHistoryController.cs#L14) — whole `/api/deleted-history` module
- [SettingController.cs:47](../../src/CleanArchitecture/Web/Controller/SettingController.cs#L47) — `PUT /api/settings`
- [SettingController.cs:63](../../src/CleanArchitecture/Web/Controller/SettingController.cs#L63) — `PUT /api/settings/password`

**Status:** Fixed and built locally (0 errors). **Not yet deployed to Hostinger** — see "Currently failing in production" below.

---

## Critical (security / data-loss risk)

| # | Area | Issue | File | Fix |
|---|------|-------|------|-----|
| 1 | Auth | Password-reset OTP is logged in cleartext at `Information` level, and no `appsettings*.json` sets a production log-level override, so it's emitted in prod/docker logs too. Anyone with container log access can take over any account via password reset. | [AuthService.cs:198](../../src/CleanArchitecture/Application/Services/AuthService.cs#L198) | Drop the OTP from the log line (or log at `Debug`); add `"Logging":{"LogLevel":{"Default":"Warning"}}` to `appsettings.docker.json`. |
| 2 | AuthZ design | No building-level data scoping anywhere — access is gated purely by module permission (`tenants`, `income`, etc.), never by which building the user is responsible for. Any user granted a module permission for one building can read/write that module's records for **every** building. | [PermissionAuthorizationFilter.cs:55](../../src/CleanArchitecture/Web/Filters/PermissionAuthorizationFilter.cs#L55) + every `Application/Services/*.cs` | Needs a building-assignment concept on `User` plus a query filter per service — real design work, not a quick patch. |
| 3 | Auth | No rate limiting on `sign-in`, `verify-otp`, `resend-otp`, `forgot-password` — the 6-digit OTP is brute-forceable within its 10-minute window; login has no lockout. | Auth endpoints generally | Add `Microsoft.AspNetCore.RateLimiting` (built into .NET 8) on the auth endpoints. |
| 4 | Ops | `backup-db.sh` / `restore-db.sh` call `sqlcmd` without `-b` (unlike the docker-compose healthcheck, which uses it correctly). Without `-b`, `sqlcmd` exits 0 even when the `BACKUP`/`RESTORE` statement itself fails — a failed backup can look successful and you won't find out until you need it. | [backup-db.sh:10](../../deploy/backup-db.sh#L10), [restore-db.sh:25](../../deploy/restore-db.sh#L25) | Add `-b` to both `sqlcmd` invocations. |

---

## High (correctness / reliability)

| # | Area | Issue | File | Fix |
|---|------|-------|------|-----|
| 5 | Bulk upload | Every record created via bulk upload gets `CreatedBy = Guid.Empty` — the background job runs in a fresh DI scope with no `HttpContext`, so `ICurrentUser.GetCurrentUserId()` resolves empty. Matches the still-open issue in `bulk-upload/BULK_UPLOAD_TEST_REPORT.md`. | [BulkUploadBackgroundService.cs:37-39](../../src/CleanArchitecture/Application/Services/BulkUploadBackgroundService.cs#L37-L39) | Pass `bulkUpload.CreatedBy` explicitly into the per-record create calls instead of relying on scoped `ICurrentUser`. |
| 6 | Bulk upload | Error unwrapping only checks one level deep (`InnerException`), so a deeper exception (e.g. from a post-save notification call) surfaces as a generic "unexpected error" even though the row's data actually saved — rows show "Failed" when they succeeded. | [BulkUploadService.cs:1142-1153](../../src/CleanArchitecture/Application/Services/BulkUploadService.cs#L1142-L1153) | Walk the full exception chain (including `AggregateException`) for a `UserFriendlyException`. |
| 7 | Systemic | Notification/activity calls run **after** the DB commit, unguarded. If one throws, the entity is already persisted but the caller sees a failure response — misleading for both bulk upload and normal single-record creates. | e.g. [TenantMoveOutService.cs:70-72](../../src/CleanArchitecture/Application/Services/TenantMoveOutService.cs#L70-L72), repeated pattern across services | Wrap post-commit notification/activity calls in try/catch, log-only, don't fail the request. |
| 8 | Scale | Paginated list endpoints call `GetAllAsync()` (full table, no filter) and paginate with `Skip/Take` **in memory** afterward. Fine at current demo-data size; every "page 1, size 10" request pulls the whole table as data grows. | [BuildingService.cs:24-43](../../src/CleanArchitecture/Application/Services/BuildingService.cs#L24-L43) + `ApartmentService`, `TenantService`, `MaintenanceRequestService`, `IncomeRecordService`, `ExpenseRecordService` | Push filtering/sorting/`Skip`/`Take` down to `IQueryable` against the DbContext. |
| 9 | Container | Dockerfile has no `USER` instruction — the API runs as root inside the container. | [Dockerfile](../../Dockerfile) | Add a non-root user, `USER app` before `ENTRYPOINT`. |

---

## Medium

| # | Area | Issue | File | Fix |
|---|------|-------|------|-----|
| 10 | Auth | OTP is never invalidated after successful use — replayable for the rest of its 10-minute window. | `AuthService.cs` | Mark it consumed on first successful verify. |
| 11 | Auth | No server-side JWT revocation — logout/password-change don't invalidate previously issued tokens (up to 30 days with `rememberMe`). | `TokenService.cs` | Token version/blocklist check, or shorten `rememberMe` expiry. |
| 12 | Config | `Identity.ExpiredTime` config is dead code — `TokenService` hardcodes 24h/30d regardless of configured value. | [TokenService.cs:32](../../src/CleanArchitecture/Application/Common/Utilities/TokenService.cs#L32) | Read the configured value instead of hardcoding. |
| 13 | Notifications | Lease/AMC expiry notification scan does a non-atomic exists-check-then-insert on every poll, no unique DB constraint — concurrent requests (two open tabs) can create duplicate notifications. | [NotificationService.cs:220-231](../../src/CleanArchitecture/Application/Services/NotificationService.cs#L220-L231) | Unique index on `(Type, Title)`, or move the scan to a serialized background job. |
| 14 | Dashboard | Maintenance priority sort buckets `Critical` together with `Low` (ternary only checks High/Medium, else = 3), so critical requests don't sort to the top of the "Open Maintenance" widget. | [DashboardService.cs:182](../../src/CleanArchitecture/Application/Services/DashboardService.cs#L182) | Add an explicit `Critical` case ranked first. |
| 15 | Maintenance | Recurrence dates drift — Monthly/Quarterly/Yearly approximated as fixed day-counts (`AddDays(30/90/365)`) instead of calendar math. | [MaintenanceRequestService.cs:744](../../src/CleanArchitecture/Application/Services/MaintenanceRequestService.cs#L744) | Use `AddMonths`/`AddYears` instead. |
| 16 | Docker | No resource limits on either container — a memory leak in the API or SQL Server's buffer pool can take down the whole VPS. | [docker-compose.yml](../../docker-compose.yml) | Add `mem_limit`/`cpus` to both services. |
| 17 | Database | `MSSQL_PID: Express` caps the DB at 10 GB, no monitoring/alerting in place. | [docker-compose.yml:9](../../docker-compose.yml#L9) | Track DB size; plan an edition upgrade path. |
| 18 | CORS | Allowed origins are baked into `appsettings.docker.json` instead of being env-var driven like every other secret — when the frontend moves to its final domain, CORS silently keeps rejecting it until someone edits source and rebuilds. | `appsettings.docker.json`, `CorsExtension.cs` | Make it env-overridable the same way `MAIL_SMTP_*` etc. are. |
| 19 | Startup | Schema-patch for `bulk_uploads.progress_percentage` swallows all exceptions silently via bare `catch {}`, and is redundant with a proper migration that already adds this column. | [ApplicationDbContextInitializer.cs:87-98](../../src/CleanArchitecture/Infrastructure/Data/ApplicationDbContextInitializer.cs#L87-L98) | Log instead of swallowing; consider removing the redundant raw-SQL block. |

---

## Low

| # | Area | Issue | Fix |
|---|------|-------|-----|
| 20 | Docs | Postman collection still describes delete-endpoint role requirement as capital `"Admin"`, inconsistent with the actual lowercase enforcement in code (now fixed). | Update Postman collection descriptions to lowercase `admin`. |

---

## Currently failing in production (as of this report)

**Yes — 4 endpoints are still broken on the live Hostinger server right now**, because the fix above is only applied locally and hasn't been pushed/deployed yet:

- `GET/POST/PUT/DELETE /api/users/*` — all methods, 403 for everyone including real admins
- `GET/POST/PUT/DELETE /api/deleted-history/*` — all methods, same 403
- `PUT /api/settings` — 403
- `PUT /api/settings/password` — 403

Everything else in this report is a **latent bug, security gap, or scaling concern** — none of it throws errors during normal day-to-day API usage today. A few only surface under specific conditions (e.g. #7/#13 need a downstream failure or concurrent requests to trigger; #8 only bites once the dataset grows).

**To stop the 4 currently-broken endpoints from failing in production**, deploy the already-fixed code:
```bash
git add -A
git commit -m "fix: correct role casing in Authorize attributes blocking admin access to users/settings/deleted-history"
git push
```
then on the server: `cd ~/ARDH-Backend && git pull && docker compose up -d --build`.
