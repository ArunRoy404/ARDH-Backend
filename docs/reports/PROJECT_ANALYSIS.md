# 🔬 ARDH Backend — Deep Analysis Report

Generated: 2026-08-07 · .NET 8.0 · ASP.NET Core Web API · SQL Server (`ARDHDB`)

---

## 1. Executive Summary

The **ARDH Property Management** backend is a Clean-Architecture ASP.NET Core 8 API that manages
buildings, apartments, tenants, owners, vendors, equipment, AMC contracts, maintenance requests,
income, expenses, notifications, dashboard analytics and deleted-history restore.

A full endpoint audit (excluding `/api/reports`) was executed against a live, freshly-seeded
database. **190/190 checks passed** after two genuine bugs were found and fixed:

| # | Bug | Impact | Fix |
| :- | :--- | :--- | :--- |
| 1 | **Invalid GUID constants in the seeder** (`o1f3b822…`, `t1f3b822…`, `v1a3b822…`, `m1f3b822…` — letters `o/t/v/m` are not hex) | App **crashed on startup** with `Guid string should only contain hexadecimal characters` whenever it had to seed a fresh DB | Replaced the 7 invalid constants with valid IDs matching the live DB / Postman collection |
| 2 | **`ResetDatabaseAsync()` honored global query filters** (`RemoveRange(_context.X)`) | `SEED_MODE=reset` failed with `FK_tenants_apartments_apartment_id` when soft-deleted rows existed — reset could not wipe a dirty DB | Added `.IgnoreQueryFilters()` to every `RemoveRange` so soft-deleted rows are physically removed |

Additionally, the live server had been running a **stale build** (binary built 12:26, source edited
12:38), which is why the API still worked while the committed code was broken. The project was
rebuilt, the DB was reset + re-seeded with the fixed code, and the server now runs the fixed binary.

---

## 2. Project Structure

```
CleanArchitecture.sln
├── src/
│   ├── CleanArchitecture.Shared/        # DTOs, request/response models, enums, ApiResponse
│   └── CleanArchitecture/
│       ├── Domain/                      # Entities (User, Building, Owner, Apartment, Tenant,
│       │                                #   Vendor, Equipment, AmcContract, MaintenanceRequest,
│       │                                #   IncomeRecord, ExpenseRecord, Notification, Activity,
│       │                                #   DeletedHistory, TenantMoveOutRecord, Setting, …)
│       ├── Application/                 # Services, repositories, IUnitOfWork, utilities
│       │   └── Services/                #   (17 domain services + AuthService)
│       │   └── Repositories/            #   Generic + per-entity repositories
│       ├── Infrastructure/              # EF Core DbContext, migrations, configurations,
│       │   │                            #   ApplicationDbContextInitializer (seeding/reset)
│       │   ├── Data/
│       │   │   ├── Configurations/      #    Entity-to-table mappings (snake_case tables)
│       │   │   └── Migrations/          #    16+ migrations (InitialArdhSchema → latest)
│       │   ├── SchemaFilter/            #    Swagger schema helpers
│       │   └── ExternalServices/        #    Health checks
│       └── Web/                         # Controllers, filters, middleware, extensions
│           ├── Controller/              #   20 controllers (BaseController + 19)
│           ├── Filters/                 #   ResponseWrapperFilter, PermissionAuthorizationFilter
│           ├── Middlewares/             #   GlobalException, Logging, Performance
│           ├── Validations/             #   FluentValidation validators per request model
│           └── Extensions/              #   Auth (JWT+cookie), Swagger, CORS, HealthChecks, MVC
├── Ardh_Postman_Collection.json         # 19 folders, ~80 endpoint examples
├── run / run.bat                        # launchers
└── DATABASE_GUIDE.md                    # ← new: seeding / migration / reset / add-user guide
```

---

## 3. Architecture & Key Design Points

### 3.1 Clean Architecture layering
- **Domain** — pure entities, no framework dependencies.
- **Application** — business logic via `IUnitOfWork` + per-domain services (`BuildingService`,
  `TenantService`, `IncomeRecordService`, …) and repositories. All services share
  `ICurrentUser`, `INotificationService`, `IActivityService`.
- **Infrastructure** — EF Core `ApplicationDbContext`, snake_case table mappings, migrations,
  and the `ApplicationDbContextInitializer` responsible for auto-migrate + seed + reset.
- **Web** — controllers are thin: validate → service → `Ok()`.

### 3.2 Authentication (hybrid JWT + HttpOnly cookie)
- `POST /api/auth/sign-in` validates BCrypt hash, generates a JWT (`TokenService`) with claims
  `nameid/name/email/role/permissions/remember_me`, and sets an **HttpOnly cookie `token_key`**
  (Secure under HTTPS, SameSite Lax under HTTP).
- The JWT **is not returned in the JSON body** — `UserSignInResponse` only carries `Message`
  (the README still documents the old behavior; the cookie-only flow is what actually ships).
- `JwtBearerEvents.OnMessageReceived` falls back to the cookie when no `Authorization` header is
  present → curl/Postman can just send the cookie jar.
- Token lifetime: 24 h (or 30 days with `rememberMe`).

### 3.3 Authorization — `PermissionAuthorizationFilter`
A global action filter that maps URL prefixes to permission requirements:

| Routes | Required permission |
| :--- | :--- |
| `/api/users`, `/api/settings`, `/api/deleted-history` | `admin` |
| `/api/buildings`, `/api/owners`, `/api/apartments`, `/api/tenants` | `properties` |
| `/api/vendors`, `/api/equipment`, `/api/amc-contracts`, `/api/maintenance` | `operations` |
| `/api/income`, `/api/reports` | `finance` |
| `/api/expenses` | `finance` **or** `operations` |
| `/api/upload` | `admin` or `properties` |
| `/api/notifications`, `/api/activities`, `/api/dashboard` | `dashboard` |

Role shortcuts: `admin` role bypasses everything; `property_manager` gets properties + operations +
dashboard; `viewer` role is read-only (mutating HTTP verbs blocked). ✅ Verified live with
manager/accountant logins (see §5).

### 3.4 Response envelope (`ResponseWrapperFilter`)
Every `2xx` becomes `{ success, message, data }` (auto-extracts `message` from anonymous objects),
and every non-2xx becomes `{ success:false, message, errors[] }` with normalized error details.
Binary results (PDF/CSV/upload) bypass the wrapper.

### 3.5 Seeding (`ApplicationDbContextInitializer`)
- Runs on every startup: `MigrateAsync()` → (optional `SEED_MODE=reset` wipe) → 16 seed methods.
- All seed rows use **fixed GUIDs** so Postman examples always work. **43 `Guid.Parse` constants** —
  all validated as proper hex GUIDs after the fix.
- Idempotent: every `SeedX()` early-returns if the table already has rows.

---

## 4. Modules & Endpoint Inventory (excluding `/api/reports`)

| Module | Base route | Endpoints | Notes |
| :--- | :--- | :--- | :--- |
| Auth | `/api/auth` | sign-in, forgot-password, verify-otp, reset-password, resend-otp, logout, profile | OTP randomly generated; logged to console + emailed via SMTP |
| Users | `/api/users` | GET list, GET by id, POST, PUT, DELETE(soft), PATCH toggle-status | DELETE requires admin password |
| Buildings | `/api/buildings` | GET list, GET by id, GET stats, POST, PUT, DELETE | stats endpoint |
| Settings | `/api/settings` | GET, GET public (anon), PUT, PUT password | password change requires current |
| Deleted History | `/api/deleted-history` | GET list, GET by id, POST restore, DELETE permanent | permanent delete requires admin pw |
| Upload | `/api/upload` | image, document, id-proof, DELETE file | local storage |
| Owners | `/api/owners` | CRUD + filters + CSV export | uniqueness on name/phone/email/id/account |
| Apartments | `/api/apartments` | CRUD + filters (status=Occupied/Vacant) + CSV export | flat-number unique per building |
| Tenants | `/api/tenants` | CRUD + move-out records (CRUD) + CSV export | apartment must be vacant to move in |
| Vendors | `/api/vendors` | CRUD | vendorType free text |
| Equipment | `/api/equipment` | CRUD + PATCH status + CSV export | status free text |
| AMC Contracts | `/api/amc-contracts` | CRUD + stats | unique amcCode & contractNumber |
| Maintenance | `/api/maintenance` | CRUD + stats + PATCH status + PATCH assign + CSV export | recurrence fields |
| Income | `/api/income` | CRUD + PATCH status + PDF receipt + CSV export | apartment must be occupied; duplicate guard |
| Expenses | `/api/expenses` | CRUD + PATCH status + CSV export | tanker validation; duplicate guard |

**CSV export endpoints (7 total):** `GET /api/{owners|apartments|tenants|equipment|maintenance|income|expenses}/download-csv` —
each returns `File(bytes, "text/csv", "<module>.csv")` with the same query filters as its list
endpoint. Owners/Apartments/Tenants require `properties` permission; Equipment/Maintenance
require `operations`; Income/Expenses require `finance` (or `finance|operations` for expenses).
CSV columns are the flat, human-readable projection of each ViewModel (IDs + resolved names).
(Added 2026-08-07 — see §7.)
| Notifications | `/api/notifications` | list, count, mark-read, read-all, delete, clear-all | per-user recipients |
| Activities | `/api/activities` | list (paged, optional building filter) | read-only |
| Dashboard | `/api/dashboard` | stats, occupancy, expense-breakdown, recent-payments, open-maintenance | optional buildingId |

---

## 5. Endpoint Verification — Results

**Methodology:** a Python harness exercised every non-report endpoint against the live API
(`http://localhost:5240`) using the freshly seeded DB. Each endpoint was tested with:

- valid happy-path payloads (create → locate → update → patch status → soft delete),
- duplicate/invalid payloads (expecting 400 validation errors),
- unauthenticated requests (expecting 401),
- cross-role permission checks (manager / accountant logins),
- edge cases (future expense dates, occupied-apartment move-in, non-hex GUIDs, wrong admin password).

### 5.1 Final clean run

```
TOTAL: 190   PASS: 190   FAIL: 0
```

### 5.2 What was verified (highlights)

- **Auth:** sign-in OK/wrong-password/unknown-user; profile with & without cookie; forgot/verify/
  reset/resend OTP (incl. mismatched confirm-password rejection).
- **CRUD (all 11 data modules):** create (200), duplicate-create rejected (400), invalid enum/value
  rejected (400), get-by-id (200), nonexistent id (400/404), update (200), status-patch (200),
  delete with correct admin password (200), delete with wrong/missing password (400).
- **Income:** PDF receipt download (200, `application/pdf`), CSV export (200, `text/csv`);
  duplicate entry (same apartment+type+amount+month) correctly rejected; income for a *vacant*
  apartment correctly rejected — the move-out flow marks the flat vacant, and the business rule
  fires as designed.
- **Expenses:** duplicate + future-date rejection; water-tank field validation.
- **Tenants:** move-in only into vacant flat; move-out create/get/update/delete.
- **Notifications:** list/count/mark-read/read-all/delete/clear-all (per-user scoping).
- **Dashboard:** stats, occupancy, expense-breakdown, recent-payments, open-maintenance.
- **Upload:** PNG upload (200 + URL), invalid extension rejected (400), file delete (200).
- **Permissions:** manager CANNOT access `/api/users|settings|deleted-history` (403) but CAN access
  buildings/income/equipment/maintenance/dashboard; accountant CAN access income/expenses/dashboard
  (seeded with `dashboard,finance`) but NOT buildings/equipment (403).
- **Security:** every protected endpoint returns 401 without a token; `settings/public` is
  anonymous as designed.

### 5.3 Incidents found during the audit (all resolved)

1. **Seeder crash on fresh DB** — invalid GUID constants. Fixed (7 IDs) + verified by an
   in-memory smoke run and a real `SEED_MODE=reset` run. All 43 constants are now valid hex GUIDs.
2. **Reset crash on dirty DB** — `ResetDatabaseAsync` deleted only non-soft-deleted rows due to
   global query filters, causing FK conflicts. Fixed with `.IgnoreQueryFilters()`.
3. **Stale running binary** — the server on :5240 was an old build; rebuilt and restarted.

### 5.4 ⚠️ Known gap — Postman collection still uses the old IDs

You chose to fix the **seeder only** (not the Postman collection). The seeder now uses valid IDs
matching the live DB (`b3f3b822`/Rahul, `a3f3b822`/Arjun, `f7a3b822`/Sunil, `f7c7b822`/BESCOM,
`c1/c2/c3f3b822`/maintenance), **but `Ardh_Postman_Collection.json` still contains the old,
invalid GUIDs** (`o1f3b822…`, `t1f3b822…`, `v1a3b822…`, `v4c7b822…`, `m1/m2/m3f3b822…`) in the
Owners, Tenants, Vendors and Maintenance folders. Those specific Postman examples will return
400/404 against a freshly seeded DB until the collection is updated to the new IDs.

### 5.5 Observations / minor notes (no action required)

- `PUT /api/income/{id}`, `PUT /api/expenses/{id}` and `PUT /api/maintenance/{id}` require the
  `id` field **in the body** (validation: “Id is required”) — the controller does not copy it from
  the route (unlike users/buildings/owners which set `request.Id = id`). Include `id` in the body
  for those three modules. (Postman examples already do this.)
- `UserSignInResponse` returns only `message`; the token is delivered exclusively via the
  `token_key` cookie. Clients must rely on the cookie (browser) or read it from `Set-Cookie`.
- Upload endpoint returns `415` (not 401) for an unauthenticated request sent without a valid
  multipart body — expected ASP.NET media-type behavior; a properly-formed multipart request
  without a token returns 401.
- The README documents the old “token in JSON body” sign-in; consider updating it to the
  cookie-based flow.

---

## 6. Database Schema (snake_case tables)

`users, forgot_password, settings, buildings, owners, apartments, tenants,
tenant_move_out_records, vendors, equipment, amc_contracts, maintenance_requests,
income_records, expense_records, notifications, notification_recipients, activities,
deleted_histories`

- Global soft-delete via `IsDeleted` + EF query filters on 11 entities.
- Audit columns `CreatedAt/UpdatedAt/CreatedBy/UpdatedBy` on most entities.
- Unique constraints: `buildings.building_name`, `apartments` (building+flat), `tenants` email/id,
  `amc_contracts.amc_code` + `contract_number`, income duplicate rule enforced in service.

---

## 7. Final State

- ✅ Seeder fixed — fresh and reset databases seed successfully with valid, Postman-matching IDs.
- ✅ `SEED_MODE=reset` now wipes soft-deleted rows too and re-seeds cleanly.
- ✅ All 190 endpoint checks pass against the fixed build.
- ✅ Live server restarted on `http://localhost:5240` running the fixed code with a pristine DB.
- 📄 `DATABASE_GUIDE.md` — 4-part terminal guide (seed / migrate / wipe / add user).

## 8. CSV Export Feature (added 2026-08-07)

Seven modules now expose a `GET /api/<module>/download-csv` endpoint that streams a `text/csv`
file with the same filters as their list endpoint:

| Module | Endpoint | File | Permission |
| :--- | :--- | :--- | :--- |
| Owners | `/api/owners/download-csv` | `owners.csv` | properties |
| Apartments | `/api/apartments/download-csv` | `apartments.csv` | properties |
| Tenants | `/api/tenants/download-csv` | `tenants.csv` | properties |
| Equipment | `/api/equipment/download-csv` | `equipment.csv` | operations |
| Maintenance | `/api/maintenance/download-csv` | `maintenance_requests.csv` | operations |
| Income | `/api/income/download-csv` | `income_records.csv` | finance |
| Expenses | `/api/expenses/download-csv` | `expense_records.csv` | finance or operations |

Implementation mirrors the existing Income/Expenses pattern: each service exposes
`ExportToCsv(...)` returning `byte[]` (UTF-8, quoted fields, `"` escaped), each controller has a
`[HttpGet("download-csv")]` action, and every field is quoted so commas/newlines inside values
are safe. CSV exports are **GET** requests, so the read-only `viewer` role can download them.

All 7 endpoints were verified live against a freshly seeded DB (200 + `text/csv` + attachment
header; filtered variants with search/status/type also pass; unauthenticated → 401).

The Postman collection was updated with **5 new requests** (O-06, AP-06, T-06, E-07, M-09) each
with full query-parameter docs + success (200 CSV) and error (403 permission) response examples,
and the existing I-08/EX-07 CSV examples were corrected to show real CSV responses.
