# 🔬 ARDH Property Management — Deep Analysis (2026-08-15)

Comprehensive walkthrough of every layer, module, endpoint, request/response body, function,
and the Postman collection. Companion to the earlier `PROJECT_ANALYSIS.md` (endpoint audit) and
`DATABASE_GUIDE.md` (ops). Verified: `dotnet build CleanArchitecture.sln` → **0 errors**
(2 NuGet warnings, see §9.6).

---

## 1. Solution Layout

```
CleanArchitecture.sln
├── src/
│   ├── CleanArchitecture.Shared/            # DTOs, request/response models, enums, ApiResponse (no domain logic)
│   └── CleanArchitecture/                   # The API
│       ├── Domain/                          # Entities + authorization handlers + JSON converters + constants
│       ├── Application/                     # 23 services, 19 repositories, IUnitOfWork, exceptions, utilities
│       ├── Infrastructure/                  # EF Core DbContext, 26 migrations, 20 EF configurations,
│       │                                    #   ApplicationDbContextInitializer (seed/reset), health checks, Swagger schema filters
│       └── Web/                             # 21 controllers, filters, middleware, FluentValidation validators, extensions
├── Ardh_Postman_Collection.json             # 20 folders, 140 requests, baseUrl http://localhost:5240
├── README.md                                # API docs (partially out of date — sign-in token is cookie-only)
├── PROJECT_ANALYSIS.md                      # Prior endpoint audit (190/190 pass)
├── DATABASE_GUIDE.md                        # Seed / migrate / wipe / add-user guide
├── .env (gitignored) / .env.example         # Secrets via DotEnvExtension.LoadDotEnv()
├── run / run.bat                            # `dotnet run --project src/CleanArchitecture`
└── permanent_delete_warnings.json           # Frontend-facing destructive-action warning copy
```

Target: **.NET 8.0**, `Microsoft.NET.Sdk.Web`, LangVersion 12, central package versions
(`Directory.Packages.props`), nullable enabled. Stack: EF Core 8 (SQL Server), AutoMapper 12,
FluentValidation 11, Swashbuckle 6.4, BCrypt.Net-Next, Resend (email), CloudinaryDotNet (declared,
not used), HealthChecks.

---

## 2. Architecture & Request Lifecycle

### 2.1 Startup pipeline (`Program.cs` → `Web/Extensions/HostingExtensions.cs`)
1. `DotEnvExtension.LoadDotEnv()` loads the root `.env` into process env vars.
2. `builder.Configuration.Get<AppSettings>()` binds the full config object.
3. `ConfigureServices()` registers Infrastructure → Application → Web services.
4. `ConfigurePipelineAsync()`:
   `ApplicationDbContextInitializer.InitializeAsync()` (migrate + seed)
   → `GlobalExceptionMiddleware` → `UseExceptionHandler` → `LoggingMiddleware` → `PerformanceMiddleware`
   → HTTPS redirection → static file serving for `FileStorageSettings.Path` (`/image`) →
   CORS → Swagger → health checks → **Authentication → Authorization → MapControllers**.

### 2.2 Request filters (Web/Filters) — run in this order
1. **`ValidateModelFilter`** (SchemaFilter/ValidateModelFilter.cs) — 400 on invalid model state;
   wraps errors as `ErrorResponse` (FluentValidation + DataAnnotations).
2. **`PermissionAuthorizationFilter`** — URL-prefix → permission map (see §3.3).
3. **`ResponseWrapperFilter`** — wraps every `2xx` as `{ success, message, data }` and every
   non-2xx as `{ success:false, message, errors[] }`. Binary responses (PDF/CSV/files) and
   already-wrapped `ApiResponse<T>` pass through untouched.

### 2.3 Exception pipeline
`ExceptionResponseMapper.Map()` centralizes status-code/message/code mapping:
- `UserFriendlyException` (with `ErrorCode`) → 400/401/403(→Unauthorized code)/404/409/422/500.
- `DbUpdateException` / `SqlException` → unwrapped by `DbErrorResolver` (unique-constraint,
  FK-conflict, null, truncation).
- Anything else → 500 with a generic message.
Both `UseExceptionHandler` and `GlobalExceptionMiddleware` serialize the same `ApiErrorResponse`
shape (Success, Message, Errors[{Code, Message, ErrorMessage}]).

---

## 3. Authentication & Authorization

### 3.1 Hybrid JWT + HttpOnly cookie
- `POST /api/auth/sign-in` → BCrypt verify → `TokenService.GenerateToken()` (claims:
  `nameid`, `name`, `email`, `role`, `permissions`, `remember_me`) → **sets HttpOnly cookie
  `token_key`** (Secure + SameSite=None on HTTPS, SameSite=Lax on HTTP; 24 h, 30 d with rememberMe).
- **The JWT is NOT in the JSON body** — `UserSignInResponse` only carries `Message`. The README
  still documents the old token-in-body behavior (known doc drift).
- `JwtBearerEvents.OnMessageReceived` falls back to the `token_key` cookie when no
  `Authorization` header is present, so Postman/curl can work cookie-only.
- `GET /api/auth/refresh` is documented in the README but **does not exist** in code; the actual
  endpoints are sign-in, profile, forgot-password, verify-otp, reset-password, resend-otp, logout.

### 3.2 Password handling
- Users: BCrypt via `StringHelper.Hash()/Verify()`.
- OTP: 6-digit random (`GenerateRandom(100000, 1000000)`), stored on `ForgotPassword` with a
  10-minute validity window checked in `VerifyOtp`. Sent via Resend (`MailService`) and logged to
  console. The same code is used for forgot-password and resend-otp (each inserts a new row;
  verification always takes the *latest* row for the email).

### 3.3 Permission model (`PermissionAuthorizationFilter`)
| URL prefix | Required permission |
| :--- | :--- |
| `/api/users`, `/api/settings`, `/api/deleted-history` | `admin` |
| `/api/buildings`, `/api/owners`, `/api/apartments`, `/api/tenants` | `properties` |
| `/api/vendors`, `/api/equipment`, `/api/amc-contracts`, `/api/maintenance` | `operations` |
| `/api/income`, `/api/reports` | `finance` |
| `/api/expenses` | `finance` **or** `operations` |
| `/api/notifications`, `/api/activities`, `/api/dashboard` | `dashboard` |
| `/api/upload` | any authenticated user (but viewer cannot mutate) |

- **Open-read exception:** GET on buildings/owners/apartments/tenants/vendors/equipment/
  amc-contracts/maintenance/income/reports/expenses is allowed for *any* authenticated user;
  only mutating verbs are permission-gated.
- Role shortcuts: `admin` role bypasses everything; `property_manager` implies
  properties + operations + dashboard; `viewer` is read-only (POST/PUT/PATCH/DELETE → 403).
- Roles (enum): `admin | property_manager | accountant | viewer`.
  Permissions (enum): `dashboard, properties, finance, operations, admin`.
- `UserService.ResolvePermissions()` unions role defaults with requested permissions:
  admin → all; viewer → all read modules; property_manager → operations; accountant → finance.
- `BulkUploadController` re-implements the same module→permission map inline (apartments/tenants/
  owners → properties; maintenance/equipment → operations; income → finance; expenses → finance|operations).

---

## 4. Domain Layer (src/CleanArchitecture/Domain)

### 4.1 Entities (19)
| Entity | Table | Key fields | Notes |
| :--- | :--- | :--- | :--- |
| `User` | users | email, password_hash, role, permissions, is_active, last_login_at, refresh_token | soft-delete |
| `Building` | buildings | building_name, address/city/state/country, google_map_link, total_floors, parking_details, status(active/inactive), image_url | soft-delete |
| `Owner` | owners | full_name, phone, email, id_type(Aadhar/PanCard/Passport), id_number, bank_name, account_number, ifsc_code, status | soft-delete |
| `Apartment` | apartments | building_id, owner_id, nestaway_id, flat_number, floor, apartment_type, area_sqft, bedrooms, bathrooms, has_balcony, parking_slot, expected_rent, maintenance_charge, water_charge, notes, current_tenant_id | soft-delete |
| `Tenant` | tenants | building_id, apartment_id, full_name, phone, email, id_type, id_number, id_proof_attachment_url, move_in_date, lease dates, monthly_rent, security_deposit, emergency contact, status(Active/Moved Out) | soft-delete |
| `TenantMoveOutRecord` | tenant_move_out_records | tenant_id, apartment_id, move_out_date, pending_rent, damage_amount, refund_amount, id_number, handover_note, processed_by | hard-delete |
| `Vendor` | vendors | name, company_name, phone, email, vendor_type (free text), gst_number, address, status | soft-delete |
| `Equipment` | equipment | building_id, name, type, brand, model, serial_number, install_date, warranty_expiry_date, **status (free text: Operational/UnderMaintenance/…)** | soft-delete |
| `AmcContract` | amc_contracts | amc_code (unique), contract_number (unique), contract_title, contract_type, equipment_id, vendor_id, start/end date, contract_amount, payment_terms, service_frequency, coverage/exclusions/document_link/remarks, status(Active/Expiring/Expired/Cancelled) | soft-delete |
| `MaintenanceRequest` | maintenance_requests | title, description, category (free text), priority(Low/Medium/High/Critical), status(Open/InProgress/Complete/Cancelled), vendor_id, equipment_id, building_id, apartment_id, estimated_cost, annual_cost, scheduled_date, start_date, recurrence_frequency, receipt_attachment_url, notes | soft-delete |
| `IncomeRecord` | income_records | income_entity(ApartmentWise/GeneralOthers), income_type(Rent/Maintenance/SecurityDeposit/WaterCharge/Others), amount, building_id, apartment_id, payment_date, payment_method, transaction_reference, status(Paid/Pending/Overdue/Partial) | soft-delete |
| `ExpenseRecord` | expense_records | category(Utility/Operational/Maintenance/Tax/Capital), expense_head, specific_item, vendor_id, nature(Service/Material/Others), amount, entity(General/ApartmentSpecific/BuildingLevel), building_id, apartment_id, expense_date, payment_method (free text), status(Draft/PendingPayment/Paid), **water-tank fields** (tanker_number, time_of_delivery, delivery_driver_name, manager_in_attendance, liters_filled) | soft-delete |
| `Notification` | notifications | type (properties/operations/finance/admin), title, detail | fan-out |
| `NotificationRecipient` | notification_recipients | notification_id, user_id, is_read, read_at | per-user read state |
| `Activity` | activities | action_type, entity_type, entity_id, building_id, description | audit trail |
| `BulkUpload` | bulk_uploads | module, status(Processing/Finished/Failed), original/processed file URL, total/success/failed counts, global_error | job record |
| `ForgotPassword` | forgot_password | user_id, email, token, otp, datetime | OTP store |
| `Setting` | settings | company_name/email/phone/address, icon, fav, **admin_password (BCrypt)** | singleton |
| `DeletedHistory` | deleted_histories | entity_type, entity_id, entity_title, deleted_by/at, restored_by/at | soft-delete ledger |

All CRUD entities carry `CreatedAt/UpdatedAt/CreatedBy/UpdatedBy` + `IsDeleted`. **11 entities have
EF global query filters** (`!IsDeleted`) — soft deletes are invisible to normal queries; the
repository `AnyIncludingDeletedAsync`/`FirstOrDefaultAsync` use `IgnoreQueryFilters()` where needed.

### 4.2 Enums — two serialization strategies
- `JsonStringEnumConverter` (plain names, lower-case for UserRole/BuildingStatus; PascalCase
  otherwise): UserRole, UserPermission, BuildingStatus, OwnerIdType, OwnerStatus, TenantStatus
  (MovedOut → `"Moved Out"`), VendorStatus.
- `JsonPropertyNameEnumConverter<T>` (custom; supports spaces / hyphens): AmcContractType,
  AmcPaymentTerm, AmcServiceFrequency, AmcStatus, MaintenancePriority, MaintenanceStatus,
  MaintenanceRecurrenceFrequency, IncomeEntity/Type/PaymentMethod/Status, ExpenseCategory/
  Nature/Entity/Status. e.g. `UpiDigitalTransfer` ⇄ `"UPI / Digital Transfer"`, `BiWeekly` ⇄
  `"Bi-Weekly"`, `NonComprehensive` ⇄ `"Non-Comprehensive"`.
- `ErrorCode` is a numeric enum (Internal=0 … UnprocessableEntity=8) used by the exception mapper.

---

## 5. Application Layer (src/CleanArchitecture/Application)

### 5.1 Services (23, all transient) — full API surface
| Service | Methods (beyond standard GetPaginated/GetById/Create/Update/Delete) |
| :--- | :--- |
| `AuthService` | SignIn, ForgotPassword, VerifyOtp, ResetPassword, Logout, ResendOtp, GetProfile |
| `UserService` | + ToggleStatus; role→permission resolution |
| `BuildingService` | + GetStats (apt counts, occupancy, open maintenance) |
| `OwnerService` | + ExportToCsv |
| `ApartmentService` | + ExportToCsv; occupancy status = `CurrentTenantId != null` |
| `TenantService` | + ExportToCsv; move-in occupancy guard |
| `TenantMoveOutService` | CreateMoveOut, GetByTenantId, UpdateMoveOut, DeleteMoveOut |
| `VendorService` | uniqueness on email/phone/GST |
| `EquipmentService` | + UpdateStatus, ExportToCsv |
| `AmcContractService` | + GetStats (active/expiring-30d/expired/cancelled); unique amcCode + contractNumber incl. soft-deleted |
| `MaintenanceRequestService` | + UpdateStatus, Assign(vendor), GetStats, ExportToCsv; equipment status coupling |
| `IncomeRecordService` | + UpdateStatus, GenerateReceiptPdf (hand-built minimal PDF), ExportToCsv; duplicate guard |
| `ExpenseRecordService` | + UpdateStatus, ExportToCsv; duplicate + tanker guards |
| `NotificationService` | GetNotifications, GetCount, MarkAsRead, MarkAllAsRead, Delete, ClearAll, CreateNotificationInternal |
| `ActivityService` | GetPaginated, CreateActivity |
| `DashboardService` | GetStats, GetOccupancy, GetExpenseBreakdown, GetRecentPayments, GetOpenMaintenance |
| `ReportService` | GetIncomeReport, GetExpenseReport, GetReportStats, ExportReportToCsv (combined ledger) |
| `DeletedHistoryService` | GetPaginated, GetById (with EntityData snapshot), Restore, DeletePermanently |
| `SettingService` | Get, GetPublic (anonymous), Update (needs admin password), UpdatePassword |
| `BulkUploadService` | StartAsync, ProcessAsync, GetStatusAsync, GetStatusByIdAsync, GetTemplateAsync (+ `BulkUploadQueue` singleton + `BulkUploadBackgroundService` hosted worker) |
| `MailService` | SendEmailAsync via Resend |
| `CurrentTime` | UTC now (token expiry source) |

### 5.2 Key business rules (verified in code)
- **Buildings:** name unique (incl. soft-deleted); delete records DeletedHistory + activity + notification.
- **Apartments:** flat number unique per building (incl. soft-deleted); building/owner must exist; floor ≥ 0, area/rents non-negative (validators).
- **Tenants:** move-in only into a **vacant** flat (`CurrentTenantId == null`); email + ID number unique (incl. soft-deleted); on move-out the tenant → `MovedOut` and apartment `CurrentTenantId → null`.
- **Income:** amount > 0; apartment must be **occupied** for ApartmentWise; **duplicate guard** = same apartment + incomeType + amount + payment month → 400; paid/overdue entries trigger activity + finance notifications.
- **Expenses:** amount > 0; expense date cannot be in the future; **duplicate guard** = amount + expenseDate + expenseHead + specificItem + nature; **water-tank guard** = same tankerNumber + timeOfDelivery; `TimeOfDeliveryHelper.TryParse` accepts bare time (`13:31`, `01:31 PM`) or full ISO date-time.
- **Maintenance:** building/apartment/vendor/equipment existence checks; **equipment status coupling** — creating/updating to Open/InProgress flips the linked equipment to `UnderMaintenance`, completion/deletion flips back to `Operational`; `NextMaintenanceDate = StartDate + days(frequency)` (Daily 1, Weekly 7, BiWeekly 14, Monthly 30, BiMonthly 60, Quarterly 90, HalfYearly 182, Yearly 365, BiYearly 730) — a *read* projection, not persisted.
- **AMC:** amcCode + contractNumber unique across **all** rows incl. soft-deleted; endDate after startDate; FK restrict.
- **Settings:** general update and password change both verify the stored BCrypt `AdminPassword`.
- **Move-out:** one record per tenant; updates tenant status and frees the flat in a transaction.
- **Bulk upload:** 7 modules (apartments/tenants/owners/income/expenses/maintenance/equipment); CSV rows validated individually; per-row failures collected into a processed CSV; processed in background with `Processing → Finished | Failed`.

### 5.3 Repositories & persistence pattern
- `GenericRepository<T>`: Add/AddRange/Any/AnyIncludingDeleted/Count/GetById/ToPagination/
  FirstOrDefault (incl. a sort overload that **ignores query filters**)/GetAll/Update/Delete.
- `IUnitOfWork` exposes 19 typed repositories + `SaveChangesAsync` + two
  `ExecuteTransactionAsync` overloads (Action/Func<Task>); transactions commit or roll back and
  **re-throw UserFriendlyException untouched** so business messages survive.
- Repositories are mostly thin `GenericRepository<T>` subclasses; domain services do filtering in
  memory (`GetAllAsync()` then LINQ) rather than EF-projected queries — fine for demo scale, noted
  as a scaling concern (§9).

---

## 6. Infrastructure Layer (src/CleanArchitecture/Infrastructure)

### 6.1 DbContext
- 19 `DbSet`s; `OnModelCreating` applies all `Configurations/` (snake_case table + column names,
  string-converted enums, decimal(14,2), max lengths, unique indexes, `DeleteBehavior.Restrict`
  FKs) then adds the 11 soft-delete query filters.
- Unique indexes: `buildings.building_name`, `amc_contracts.amc_code`, `amc_contracts.contract_number`,
  `apartments` (building+flat via service check + index).
- 26 migrations: `InitialArdhSchema` (2026-07-12) → `AddBulkUploads` (2026-08-07). Notable:
  `RemoveSoftDeleteAuditProperties`, `AddIsDeletedSoftDelete`, `RefactorEquipmentModule`,
  `AddMaintenanceRecurrenceFields`, `RemoveTenantAndPeriodFromIncomeRecords`,
  `MakeExpensePaymentMethodFreeText`.

### 6.2 Seeding (`ApplicationDbContextInitializer`) — 1123 lines
- Runs at startup: `MigrateAsync()` (relational only) then seeds if tables are empty (idempotent).
- **Fixed canonical GUIDs** so Postman examples line up (see §8 — *partially*; §8.3 documents the
  remaining mismatches).
- Demo data: 3 users (admin/manager/accountant, all `P@ssw0rd`), 2 buildings, 2 owners, 5
  apartments, 2 tenants (+1 move-out), 3 vendors, 3 equipment, 1 AMC contract, 3 maintenance
  requests, 4 income, 3 expenses (incl. one water tanker), settings row, 3 notifications fanned
  out to users by permission type, 5 activities, 2 deleted-history rows.
- `SEED_MODE` env var: `reset` (wipe + reseed), `wipe` (wipe + admin + settings only), `none`
  (migrations only). `ResetDatabaseAsync` wipes in FK-safe order with `IgnoreQueryFilters()`.
- Settings `AdminPassword` seeded from `AdminSettings__Password` env (default `adminpassword`) —
  this is the password required for all X-Admin-Password-gated deletes.

---

## 7. Web Layer — Complete Endpoint Catalog (21 controllers, 140 Postman requests)

> All non-auth endpoints require a valid JWT (Bearer or `token_key` cookie). All 2xx bodies are
> wrapped as `{ success, message, data }`; errors as `{ success:false, message, errors[] }`.
> Pagination shape: `{ items[], totalCount, page, pageSize }`.

### 7.1 Auth — `/api/auth` (public except profile)
| # | Method & route | Request body | Success response |
| :-- | :--- | :--- | :--- |
| 1 | `POST /api/auth/sign-in` | `{ email, password, rememberMe }` | `{ success, message, data:null }` + **Set-Cookie token_key** |
| 2 | `POST /api/auth/forgot-password` | `{ email }` | `{ message: "Password reset OTP sent to email." }` |
| 3 | `POST /api/auth/verify-otp` | `{ email, otp }` | `{ message: "OTP verified successfully…" }` (400 if wrong/expired) |
| 4 | `POST /api/auth/reset-password` | `{ email, otp, newPassword, confirmNewPassword }` | `{ message: "Password has been reset successfully." }` |
| 5 | `POST /api/auth/resend-otp` | `{ email }` | `{ message: "Password reset OTP sent to email." }` |
| 6 | `DELETE /api/auth/logout` | — | `{ message: "Successfully logged out." }` |
| 7 | `GET /api/auth/profile` | — (Authorize) | `UserProfileResponse` (id, name, email, phone, role, address, avatarURL, city, isActive, permissions, lastLoginAt, createdAt, updatedAt) |

### 7.2 Users — `/api/users` (admin)
| # | Route | Body / query | Response |
| :-- | :--- | :--- | :--- |
| U-01 | `GET /api/users` | `page, pageSize, search, role, is_active` | `PaginatedList<UserViewModel>` |
| U-02 | `GET /api/users/{id}` | — | `UserViewModel` |
| U-03 | `POST /api/users` | `{ name, email, phone, password, confirmPassword, address, role, permissions?, avatarURL }` | `{ message }` |
| U-04 | `PUT /api/users/{id}` | `{ name, email, phone, address, role, isActive, permissions?, avatarURL }` (id copied from route) | `{ message }` |
| U-05 | `DELETE /api/users/{id}` | `?password=` or `X-Admin-Password` header | `{ message }` (400 invalid admin pw) |
| U-06 | `PATCH /api/users/{id}/toggle-status` | — | `{ message }` |

### 7.3 Buildings — `/api/buildings`
B-01 list (`search, status`), B-02 by id, B-03 create, B-04 update, B-05 delete (admin pw),
B-06 `GET /{id}/stats` → `{ totalApartments, totalOccupied, totalVacant, totalOpenMaintenance }`.

### 7.4 Owners — `/api/owners`
O-01 list (`search, status`), O-02 by id, O-03 create, O-04 update, O-05 delete, O-06
`GET /download-csv` → `owners.csv`.

### 7.5 Apartments — `/api/apartments`
AP-01 list (`search, buildingId, ownerId, apartmentType, status=Occupied|Vacant`), AP-02 by id,
AP-03 create, AP-04 update, AP-05 delete, AP-06 `download-csv`.

### 7.6 Tenants — `/api/tenants`
T-01 list (`search, buildingId, apartmentId, status`), T-02 by id, T-03 create (move-in),
T-04 update, T-05 delete, T-06 `download-csv`; TMO-01 `POST /{id}/move-out`, TMO-02
`GET /{id}/move-out-records` (alias `/move-out`), TMO-03 `PUT /{id}/move-out`, TMO-04
`DELETE /{id}/move-out`.

### 7.7 Vendors — `/api/vendors` · Equipment — `/api/equipment`
V: list (`search, vendorType, status`), by id, create, update, delete.
E: list (`search, buildingId, type, status`), by id, create, update, delete, E-06
`PATCH /{id}/status` `{ status }`, E-07 `download-csv`.

### 7.8 AMC Contracts — `/api/amc-contracts`
AMC-01 list (`search, status, contractType, vendorId, equipmentId`), AMC-02 by id, AMC-03
`GET /stats` → `{ activeCount, expiringInThirtyDaysCount, expiredCount, cancelledCount, totalCount }`,
AMC-04 create, AMC-05 update, AMC-06 delete.

### 7.9 Maintenance — `/api/maintenance`
M-01 list (`search, status, priority, category, buildingId, vendorId, equipmentId, apartmentId`),
M-02 by id, M-03 create, M-04 update, M-05 delete, M-06 `PATCH /{id}/status` `{ status }`,
M-07 `PATCH /{id}/assign` `{ vendorId }`, M-08 `GET /stats` →
`{ openCount, inProgressCount, completeCount, cancelledCount, totalCount }`, M-09 `download-csv`.

### 7.10 Income — `/api/income`
I-01 list (`search, incomeType, status, buildingId, apartmentId, startDate, endDate`), I-02 by id,
I-03 create, I-04 update, I-05 delete, I-06 `PATCH /{id}/status` `{ status }`, I-07
`GET /download/{id}` → hand-built PDF receipt, I-08 `download-csv`.

### 7.11 Expenses — `/api/expenses`
EX-01 list (`search, category, status, nature, buildingId, vendorId, apartmentId, startDate, endDate`),
EX-02 by id, EX-03 create, EX-04 update, EX-05 delete, EX-06 `PATCH /{id}/status` `{ status }`,
EX-07 `download-csv`.

### 7.12 Notifications — `/api/notifications` · Activities — `/api/activities`
N-01 list (`page, pageSize, type, is_read`), N-02 `GET /count`, N-03 `PATCH /{id}/read`,
N-04 `PATCH /read-all`, N-05 `DELETE /{id}`, N-06 `DELETE /clear-all`.
ACT-01 `GET /api/activities?page&pageSize&buildingId`.

### 7.13 Dashboard — `/api/dashboard` (dashboard permission)
D-01 `GET /stats?buildingId` → `{ totalBuildings, totalApartments, occupiedCount, vacantCount,
monthlyIncome, monthlyExpense, pendingPaymentsCount, openMaintenanceCount }` (income/expense
filtered to the current calendar month, paid only).
D-02 `GET /occupancy` → `{ occupied, vacant, maintenance, reserved, total }`.
D-03 `GET /expense-breakdown` → `[{ category, amount }]` (paid, desc).
D-04 `GET /recent-payments` → paged payments (newest date first).
D-05 `GET /open-maintenance` → paged open/in-progress (priority-ordered).

### 7.14 Reports — `/api/reports` (finance)
R-01 `GET /income`, R-02 `GET /expenses`, R-03 `GET /stats` →
`{ totalIncomes, totalExpenses, net }` (paid only), R-04 `GET /export?type=income|expense|combined`.

### 7.15 Settings — `/api/settings`
S-01 `GET`, S-04 `GET /public` (**AllowAnonymous** — icon/fav only), S-02 `PUT` (requires
`adminPassword` in body), S-03 `PUT /password` (`currentPassword, newPassword, confirmNewPassword`).

### 7.16 Deleted History — `/api/deleted-history` (admin)
DH-01 list (`search, entity_type, start_date, end_date`), DH-04 `GET /{id}` (includes `entityData`
snapshot), DH-02 `POST /{id}/restore`, DH-03 `DELETE /{id}` (permanent, admin pw).
Restore supports Building, Owner, Apartment, Tenant, Equipment, Vendor, AmcContract,
MaintenanceRequest, IncomeRecord, ExpenseRecord, User.

### 7.17 File Upload — `/api/upload` (any authenticated)
F-01 `POST /image` (png/jpg/jpeg/webp), F-02 `POST /document` (pdf/doc/docx/xls/xlsx/txt/csv),
F-03 `POST /id-proof` (pdf/doc/docx/jpg/jpeg/png), F-05 `POST /csv` (.csv), F-04
`DELETE /{fileId}`. Files saved under `FileStorageSettings.Path` (`image/`) with GUID names;
response `{ url }` built from `BaseURL` (falls back to `AppUrl`). Served statically at
`/{path}/{fileId}.{ext}`.

### 7.18 Bulk Upload — `/api/bulk-upload` (+ `/api/upload/csv` first)
BU-00 `POST /api/upload/csv` → `{ url }`; BU-01 `POST /api/bulk-upload` `{ module, fileUrl }` →
`BulkUploadViewModel`; BU-02 `GET /status?module`; BU-03 `GET /status/{id}`;
BU-04 `GET /template?module` → CSV template (headers + sample row). Modules:
apartments, tenants, owners, income, expenses, maintenance, equipment.

---

## 8. Postman Collection — `Ardh_Postman_Collection.json`

- **Name:** “Ardh Property Management API (Cookie Auth)” · schema v2.1.0 · `baseUrl = http://localhost:5240`.
- **20 folders / 140 requests.** Every request carries inline field-level docs (required/optional,
  exact enum values) in `//` comments, and most have named 200/400/401/403 response examples.
- **Auth strategy:** cookie-based (`token_key`). No collection-level auth; the sign-in example
  captures the cookie, and subsequent calls rely on Postman's cookie jar.

### 8.1 What's well aligned with the code
Buildings, apartments (IDs `a1f3b822…`, `a5c7b822…`), users (`7ca6dfd0…`, `8ca6dfd0…`), income
(`f4b3b822…`), expenses (`e4c7b822…`), AMC (`f2a3b822…`), equipment (`e1a3b822…`), move-out
tenant (`c1f7b822…`), deleted-history (`4da2b822…`), notifications (`e4f3b822…`), settings, auth,
dashboard, reports — the seeded IDs match.

### 8.2 ⚠️ Issues found in the collection (13 malformed URLs)
These requests have **broken URLs missing the module path segment** and will hit `/api/…/{id}`
(404 / no route) as written:
| Folder | Requests |
| :--- | :--- |
| AMC Contracts | AMC-02 GET, AMC-05 PUT, AMC-06 DELETE (`{{baseUrl}}f2a3b822…` missing `/api/amc-contracts/`) |
| Maintenance | M-02 GET, M-04 PUT, M-05 DELETE, M-06 PATCH, M-07 PATCH (`{{baseUrl}}m1f3b822…` missing `/api/maintenance/`) |
| Income | I-02 GET, I-04 PUT, I-05 DELETE, I-06 PATCH, I-07 PDF (`{{baseUrl}}f4b3b822…` missing `/api/income/`) |

### 8.3 ⚠️ ID drift between the seeder and the collection (would 400/404 on a fresh seed)
The seeder (fixed 2026-08-07) uses **different IDs** than several collection examples still do:
| Collection example | Collection ID | Seeded record | Seeded ID |
| :--- | :--- | :--- | :--- |
| O-02/04/05 owners | `o1f3b822-…` (non-hex `o`!) | Rahul Verma | `b3f3b822-…` |
| AP-03/04 apartment bodies `ownerId` | `o1f3b822-…` | Rahul Verma | `b3f3b822-…` |
| T-02/04/05 tenants | `t1f3b822-…` (non-hex `t`!) | Arjun Mehta | `a3f3b822-…` |
| V-02/04/05 vendors | `v1a3b822-…` (non-hex `v`!) | Sunil Kumar | `f7a3b822-…` |
| EX-03/04 expense bodies `vendorId` | `v4c7b822-…` (non-hex `v`!) | Sunil Kumar | `f7a3b822-…` |
| M-02/04/05/06/07 maintenance | `m1f3b822-…` (non-hex `m`!) | Leak request | `c1f3b822-…` |

`o/t/v/m` are **not hex characters**, so even a `Guid.Parse` would reject them — these examples
cannot work against the current seeder. (Consistent with the “known gap” recorded in
PROJECT_ANALYSIS.md §5.4.) The bulk-upload status example ID (`3f9c2a1e-…-0001`) and the upload
delete example (`3d60a25e-…`) are placeholder examples and are fine.

### 8.4 Other notes
- PUT examples for income/expenses/maintenance include `id` in the body — **required**, because
  those three controllers do NOT copy the route id into the request (unlike users/buildings/owners).
- `VerifyOtp` example sends `"otp"`; the DTO property is `OTP` — ASP.NET binds case-insensitively,
  so both work.
- Collection variable `baseUrl` must be updated if the API runs on a different port/host.
- `/api/auth/refresh` (documented in README) has no Postman request — it doesn't exist in code.

---

## 9. Findings, Risks & Observations

1. **Postman collection drift (actionable):** §8.2 (13 broken URLs) + §8.3 (6 record-ID groups use
   non-hex GUIDs that don't exist in the seed). Fix = rewrite the IDs to the seeded canonical IDs
   and repair the URLs. This is the single highest-value cleanup for the collection.
2. **Doc drift:** README documents token-in-body sign-in and a `/api/auth/refresh` endpoint —
   neither matches the code (cookie-only, no refresh endpoint).
3. **In-memory pagination:** list endpoints load all rows via `GetAllAsync()` and page in memory —
   acceptable at demo scale, but won't scale; the repo's `ToPagination` exists but isn't used by
   domain services.
4. **Manual PDF receipt:** `GenerateReceiptPdf` hand-assembles a minimal PDF with fixed xref
   offsets; fragile if strings change length. No PDF library.
5. **Duplicate-validation nuances:** income duplicate = same apartment+type+amount+**month**
   (regardless of status); expense duplicate = amount+date+head+item+nature; tanker duplicate =
   tanker number + exact delivery time. Status alone is not part of the expense signature but IS
   part of the income signature-change check on update.
6. **Auth tokens:** 24 h default / 30 d with `rememberMe`; no refresh/revocation; `refresh_token`
   column on User is unused. `Identity__Key` must be a strong secret in production.
7. **Password verification for deletes** uses the Settings admin password (BCrypt) passed via
   `?password=` or `X-Admin-Password` — a shared “admin” password, not the acting user's.
8. **`AppUrl`/`BaseURL`:** uploaded-file URLs are built from config; if neither is set the URL is
   relative. CORS is fixed to `http://localhost:3000` + `https://ardh-react.vercel.app`.
9. **NuGet warnings:** `AutoMapper 12.0.1` (NU1903, high severity advisory GHSA-rvv3-g6hj-g44x)
   — consider bumping to 12.0.4+ / a patched release. Also declared-but-unused: CloudinaryDotNet,
   Microsoft.AspNetCore.Identity.EntityFrameworkCore.
10. **Health checks:** `/health`, `/healthz` (UI), `/synthetic-check`, `/health-ui`,
    `/health-ui-api`; SQL Server check always registered (fails if DB down), external checks only
    when `EnableExternalHealthCheck=true`.
11. **Seeder/Postman canonical-ID contract** is the load-bearing convention of this repo — keep it
    in sync whenever seed data or the collection changes.
12. **Soft-delete + unique indexes:** unique constraints (e.g. amc_code, building_name) include
    soft-deleted rows; the services compensate with `AnyIncludingDeletedAsync` checks, so
    re-creating a deleted record's unique value is rejected by design (and by the DB index).

---

## 10. Quick Reference — Seeded Logins & Passwords

| Role | Email | Password |
| :--- | :--- | :--- |
| admin | `admin@gmail.com` | `P@ssw0rd` |
| property_manager | `manager@gmail.com` | `P@ssw0rd` |
| accountant | `accountant@gmail.com` | `P@ssw0rd` |
| Admin delete-password (deletes / settings) | — | `adminpassword` (from `AdminSettings__Password` / `.env`) |


Buildings/owners must pre-exist before an apartments upload (buildings isn't a bulk module) — same as today.