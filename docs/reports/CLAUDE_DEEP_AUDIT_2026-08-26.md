# ARDH Backend — Deep Audit (2026-08-26)

- **Scope:** Full read-only re-review of Web/Application/Infrastructure/Deployment, done independently of (and cross-checked against) the prior `CLAUDE_AUDIT_REPORT.md` (2026-08-24), `ARDH_DEEP_ANALYSIS.md` (2026-08-15), and `PROJECT_ANALYSIS.md` (2026-08-07).
- **Method:** three parallel deep-dive passes (Auth/Security, Business logic/Services, Infrastructure/Deployment), each instructed to verify prior findings still hold and independently hunt for anything missed. All findings below were spot-checked against actual source.
- **Status:** findings only, nothing fixed in this pass except verification.

Previously-reported items (role-casing bug, `CreatedBy = Guid.Empty` in bulk upload, in-memory pagination, `-b` flag missing in backup scripts, no Docker `USER`, no resource limits, etc.) are **not repeated in full** here — see the prior reports. Their current status is noted briefly where relevant. Everything below is either **new** or **materially worse than previously described**.

---

## Critical

| # | Area | Issue | File | Why it matters |
|---|------|-------|------|-----------------|
| 1 | Logging | `LoggingMiddleware` logs the **entire raw request body** of every `/api/*` call at `Information` level, unconditionally, in every environment (dev, docker, production). | [LoggingMiddleware.cs:41-48](../../src/CleanArchitecture/Web/Middlewares/LoggingMiddleware.cs#L41-L48), wired in `HostingExtensions.cs` | Every sign-in password, password-reset OTP, new-password value, and admin re-auth password passes through this log line in plaintext. This is broader and more severe than the previously-known single OTP log line in `AuthService.cs:198` — it's *every* sensitive field in *every* request body. |
| 2 | Data integrity | `GenericRepository.FirstOrDefaultAsync` (both overloads) unconditionally calls `.IgnoreQueryFilters()`. | [GenericRepository.cs:91-115](../../src/CleanArchitecture/Application/Repositories/GenericRepository.cs#L91-L115) | This is the workhorse method used everywhere to fetch a single row. Only some callers compensate with an explicit `&& !x.IsDeleted`; most nested/related-entity lookups (building/owner/vendor/user references inside Apartment, Expense, Maintenance, AMC, Auth flows, etc.) don't. Net effect: soft-deleted parents are silently resurrected in reads, and `Update`/`Delete` methods that reuse this method can operate on already-soft-deleted rows (see High #5 below). This single root cause underlies several other findings. |
| 3 | Export | Every XLSX/CSV export writes free-text user fields directly into `ClosedXML` cell values without escaping a leading `=`/`+`/`-`/`@`. | [XlsxHelper.cs:75](../../src/CleanArchitecture/Application/Common/Utilities/XlsxHelper.cs#L75), used by every `ExportToXlsx`/`ExportToCsv` across Apartment/Tenant/Income/Expense/Equipment/Maintenance/Owner/Report services, and the bulk-upload "processed" report | Classic CSV/formula-injection: a tenant/vendor `Notes` field of `=HYPERLINK("http://evil","x")` becomes a live, executable formula the moment staff open the export in Excel. |
| 4 | Concurrency | Apartment occupancy check (`CurrentTenantId.HasValue`) and the write that sets it happen in separate steps, with no DB constraint tying one apartment to one active tenant. | [TenantService.cs:283-293](../../src/CleanArchitecture/Application/Services/TenantService.cs#L283-L293) (Create), `:391-398` (Update) | Two concurrent move-in requests for the same vacant flat both pass validation; one tenant silently ends up orphaned (billable, listed as "Active", but not reflected as the apartment's actual occupant). No unique index backs `CurrentTenantId` or `(BuildingId, FlatNumber)` occupancy state. |
| 5 | Auth | No rate limiting or lockout on `sign-in`/`verify-otp`/`resend-otp`/`forgot-password`. (Re-confirmed still true — `AddRateLimiter`/`UseRateLimiter` doesn't appear anywhere in the solution.) | Auth endpoints generally | 6-digit OTP (900,000 values) in a 10-minute window is brute-forceable without any throttling in front of it. |

---

## High

| # | Area | Issue | File | Why it matters |
|---|------|-------|------|-----------------|
| 6 | Auth | Admin re-auth password for hard-delete endpoints (~12 controllers) is accepted via query string `?password=`, even though a header alternative (`X-Admin-Password`) already exists in the same code. | e.g. [UserController.cs:91-99](../../src/CleanArchitecture/Web/Controller/UserController.cs#L91-L99) | Query strings land in access logs, proxy logs, and browser history — the shared admin password leaks through a channel the code itself already knows to avoid. |
| 7 | Auth | Seeded admin password falls back to the hardcoded literal `"adminpassword"` if `AdminSettings__Password` isn't set, and that literal is documented in `.env.example`. | `ApplicationDbContextInitializer.cs` (seed) | A misconfigured deployment silently ships with a publicly-known admin password gating every destructive action. |
| 8 | Dependencies | `Microsoft.AspNetCore.Authentication.JwtBearer` pinned to `8.0.0` resolves `Microsoft.IdentityModel.JsonWebTokens`/`System.IdentityModel.Tokens.Jwt` to `7.0.3` — inside the range fixed by CVE-2024-21319 (JWT spoofing, fixed in 7.1.2 / 6.35.1). | [Directory.Packages.props:35](../../Directory.Packages.props#L35), resolved in `obj/project.assets.json` | A concrete, named CVE in the JWT validation stack — not previously flagged (prior reports only caught the unrelated AutoMapper advisory). |
| 9 | Data integrity | Soft-deleting a parent (Building/Vendor/Equipment/Owner) does **not** cascade — child rows (Apartments, AmcContracts, MaintenanceRequests, ExpenseRecords) stay active and fully usable. | `BuildingService.Delete`, `VendorService.Delete`, `EquipmentService.Delete`, `OwnerService.Delete` | Contrast with `DeletedHistoryService.DeletePermanently`, which *does* cascade — proving cascade was the intent, just never implemented for the soft-delete path. Deleted buildings keep taking rent payments, deleted vendors keep getting assigned maintenance, etc. |
| 10 | Data integrity | `DeletedHistoryService.Restore` flips `IsDeleted = false` on a target row without checking whether its parent (Building/Vendor/Equipment) is still soft-deleted. | [DeletedHistoryService.cs:126-170](../../src/CleanArchitecture/Application/Services/DeletedHistoryService.cs#L126-L170) | Restoring an Apartment whose Building is still deleted produces an "active" record that resolves to "Unknown Building" everywhere it's displayed. |
| 11 | Data integrity | Because of Critical #2, `Update`/`Delete` in `ApartmentService`, `TenantService`, `BuildingService`, `VendorService`, `EquipmentService`, `OwnerService`, `AmcContractService`, `MaintenanceRequestService` operate on soft-deleted rows if the caller knows/guesses the GUID — none of them re-check `!IsDeleted` before writing. | Multiple, e.g. `ApartmentService.cs:346/413`, `TenantService.cs:353/473` | Defeats the soft-delete/restore-approval workflow — a deleted record can be silently edited back into a valid-looking state. |
| 12 | Concurrency | Duplicate-prevention (income/expense/tanker-delivery/move-out-record) is check-then-act with no DB uniqueness backing it. | `IncomeRecordService.cs:645-676`, `ExpenseRecordService.cs:631-698`, `TenantMoveOutService.cs:25-29` | Two concurrent submits (double-click, retry-on-timeout) both pass the existence check before either commits — duplicate rent/expense/move-out rows. |
| 13 | Notifications | Lease/AMC expiry dedup keys **only on notification Title text** (e.g. `"Lease Expiring: {FullName}"`), with no date or entity ID in the key. | [NotificationService.cs:199-254](../../src/CleanArchitecture/Application/Services/NotificationService.cs#L199-L254) | Worse than previously documented ("possible duplicates") — this actually causes **permanent false negatives**: two tenants with the same name suppress each other's alerts, and a tenant who renews and approaches expiry again a year later never gets a second alert, since the old title still matches forever. |
| 14 | Maintenance | Closing/reassigning one maintenance request flips linked equipment back to `"Operational"` without checking whether another `Open`/`InProgress` request still references the same equipment. | `MaintenanceRequestService.cs:549-632` | Equipment can show "Operational" while still genuinely under active maintenance from a second ticket. |
| 15 | Income | Hand-built PDF receipt hardcodes byte offsets (`/Length 1000`, fixed `xref` table) regardless of actual variable-length content, and doesn't escape `(`/`)` in interpolated strings. | [IncomeRecordService.cs:478-558](../../src/CleanArchitecture/Application/Services/IncomeRecordService.cs#L478-L558) | Produces a structurally invalid PDF for virtually any real record (offsets never match); a `Notes` value containing a literal `(` or `)` corrupts the PDF content stream outright. |
| 16 | Bulk upload | Name-based lookups for Owner/Vendor/Equipment keep only the *first* match on a normalized name; none of those three entities enforce name uniqueness on create. | [BulkUploadService.cs:758-817](../../src/CleanArchitecture/Application/Services/BulkUploadService.cs#L758-L817) | Two vendors/owners/equipment sharing a name → a bulk row silently attaches income/expense/maintenance to the wrong one, with no error surfaced. |
| 17 | Infra | Destructive migration drops `equipment.amc_expiry_date/amc_vendor_id/last_service_date/next_service_date` with no data-copy step first. | [20260806184532_RefactorEquipmentModule.cs:22-36](../../src/CleanArchitecture/Infrastructure/Migrations/20260806184532_RefactorEquipmentModule.cs#L22-L36) | Safe against the seed dataset, but if ever applied to an environment with real equipment/AMC data already populated, that data is unrecoverable — `Down()` restores columns with defaults, not original values. |

---

## Medium

| # | Area | Issue | File |
|---|------|-------|------|
| 18 | Auth | OTP never invalidated after successful use — same code remains valid/replayable for the rest of its 10-minute window. | `AuthService.ResetPassword`/`VerifyOtp` |
| 19 | API | Swagger UI/JSON reachable unauthenticated in every environment, including docker/production. | Swagger extension wiring |
| 20 | Upload | No file-size limit configured anywhere on upload endpoints — potential disk-fill DoS. | Upload controller/config |
| 21 | Bulk upload | Unbounded EF change-tracker growth — `ClearChangeTracker()` only called on row failure, never after success, across a job that shares one scoped `DbContext` for the whole file. | `BulkUploadService.cs:234` |
| 22 | Bulk upload | Resolved local file path built from caller-supplied `FileUrl` with only `TrimStart('/')` — no `..`-traversal containment check before the file read. | `BulkUploadService.ResolveLocalFilePath:401-435` |
| 23 | Bulk upload | A row can be reported `Failed` in the processed file even though its underlying entity already committed, if a later post-commit step (activity/notification) throws — re-running the file then fails again on a duplicate-key error, confusing the operator. | `BulkUploadService.ProcessAsync:226-238` |
| 24 | Infra | Two migrations add `unique: true` indexes (`apartments.nestaway_id`, `amc_contracts.contract_number`) with no pre-flight duplicate-detection step — a DB with existing duplicates fails the deploy mid-migration. | `20260815092013_AddUniqueNestawayIdIndex.cs`, `20260806192646_AddContractNumberUniqueIndexToAmcContracts.cs` |
| 25 | Docker | `api` service has no `healthcheck:` (only `db` does) — a hung-but-running API process (deadlock, exhausted connection pool) won't be detected or restarted. | `docker-compose.yml` |
| 26 | Ops | `reset-db.sh` sleeps a fixed 20s between recreating the container with `SEED_MODE` and force-recreating it without — if seeding takes longer, the second recreate can kill the process mid-write. | `deploy/reset-db.sh:26-31` |
| 27 | Auth | Password policy is only a 6-character minimum, no complexity requirement. | Validators |

Previously-flagged Medium/Low items re-confirmed unchanged: CORS hardcoded (not env-driven), `MSSQL_PID: Express` 10GB cap, bare `catch{}` swallowing the `bulk_uploads.progress_percentage` schema patch, no Docker resource limits, no Docker `USER` instruction, backup/restore scripts missing `sqlcmd -b`.

---

## Low

| # | Area | Issue | File |
|---|------|-------|------|
| 28 | Dashboard | Monthly income/expense aggregation buckets by `DateTime.UtcNow.Month/Year` with no local-timezone conversion — a payment near a month boundary can land in the wrong month's totals for a business operating well ahead of UTC (e.g. IST). | `DashboardService.cs:36-50` |
| 29 | Hygiene | `GithubHealthCheck`/`TwilioHealthCheck` ping unrelated third-party status pages, unused/unregistered — leftover template cruft. | `Infrastructure/ExternalServices/HealthCheck/` |
| 30 | Observability | No correlation/trace IDs on errors; pure `ILogger` console logging, no Serilog/OpenTelemetry — a client-reported error can't be tied to a specific server log line except by timestamp. | `GlobalExceptionMiddleware.cs` |
| 31 | Dependencies | `FluentValidation.AspNetCore` (discontinued upstream) pinned at a stale `11.3.0` against core `FluentValidation 11.9.0`. | `Directory.Packages.props` |
| 32 | Config | `AllowedHosts: "*"` in every appsettings file, alongside the hardcoded CORS list — low risk behind Nginx, but same category of host-trust config worth tightening together. | `appsettings*.json` |

---

## Explicitly checked and found clean (don't waste time re-auditing these)

- OTP generation uses `RandomNumberGenerator.GetInt32` (CSPRNG) — **not** weak, despite what a casual read might suggest.
- All 11 `IsDeleted` entities have a matching `HasQueryFilter` registered in `ApplicationDbContext` — the filter *list* is complete; the bypass (Critical #2) is entirely at the repository call-site layer, not a DbContext gap.
- FK-column indexes (building_id, apartment_id, vendor_id, equipment_id, etc.) all exist via EF's default convention.
- `.dockerignore` correctly excludes `.env`/`.env.example` from the build context — secrets are not baked into the image.
- `ResetDatabaseAsync`'s FK-safe delete ordering was checked against every configured relationship — no ordering bug.

---

## Suggested priority if you want fixes next

1. **Critical #1** (stop logging full request bodies with secrets) and **#5** (rate limiting on auth/OTP) — both are cheap, mechanical fixes with outsized security payoff.
2. **Critical #2** (soft-delete filter bypass) — one place to fix (`GenericRepository.FirstOrDefaultAsync`) that resolves several downstream High findings (#9, #10, #11) at the root instead of patching each service individually.
3. **Critical #3** (export formula injection) — one shared helper (`XlsxHelper`), fixes every export path at once.
4. **High #8** (JWT CVE) — likely a one-line package bump.

Everything else is real but lower blast-radius; happy to start on any of these on request.
