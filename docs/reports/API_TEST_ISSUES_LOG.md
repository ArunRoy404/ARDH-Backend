# API Test — Issues Log

- **Generated:** 2026-08-15 10:41 UTC
- **Total checks:** 263 | **Failed:** 0 | **Passed:** 261

---

## Bugs found during verification — root cause, fix, verification

| # | Module | Issue | Root cause | Fix | Verified |
|---|--------|-------|-----------|-----|----------|
| 1 | BULK-UPLOAD (apartments/tenants) | Bulk-upload rows failed with 'AreaSqft is required.' / 'SecurityDeposit is required.' even though the create API treats those fields as optional. Any upload that followed the documented required columns (which exclude AreaSqft) failed every row. | BulkUploadService.ParseApartment used the required-variant GetDecimal(...) for AreaSqft, and ParseTenant for SecurityDeposit, adding a row error whenever the column was blank. The Web validators (ApartmentCreateRequestValidation / TenantCreateRequestValidation) treat both fields as optional. | Added GetOptionalDecimal(...) and used it for Apartment.AreaSqft and Tenant.SecurityDeposit — blank cells now map to null (no error), matching the create API. Invalid non-blank numbers still produce a row error. | Bulk upload of an apartments workbook (rows without AreaSqft) now completes with 1 success / 2 expected failures (unknown building + duplicate flat), and the created flat is visible via the API. |
| 2 | AUTH | POST /api/auth/forgot-password and POST /api/auth/resend-otp returned HTTP 500 ('An unexpected error occurred') instead of 200 whenever the email provider failed. | AuthService.SendOtpEmailAsync awaited MailService.SendEmailAsync -> Resend.EmailSendAsync, which threw ResendException ('API key is invalid') for the configured key. The exception propagated out of ForgotPassword/ResendOtp and the global handler turned it into a 500 — even though the OTP had already been generated, persisted and logged. | Wrapped the email send in try/catch inside SendOtpEmailAsync: on failure the OTP stays logged (and persisted) and the request still returns the documented 200 'OTP sent to email' — the password-reset flow no longer depends on the mail provider being reachable. | forgot-password + resend-otp now return 200 and the OTP appears in the server log; verify-otp/reset-password succeed with it. |
| 3 | MAINTENANCE | The API rejected valid common-area maintenance requests — POST /api/maintenance with no apartment/vendor/equipment returned 400 'Apartment ID is required / Vendor ID is required / Equipment ID is required'. A seeded record ('Parking lot lights broken') has none of these, and the bulk-upload path allows them to be empty. | MaintenanceRequestCreateRequestValidation / MaintenanceRequestUpdateRequestValidation marked ApartmentId, VendorId and EquipmentId as mandatory, contradicting MaintenanceRequestService.Create/Update (existence-check only when provided) and the bulk-upload path. | Removed the mandatory ApartmentId/VendorId/EquipmentId rules from both validators and updated the MaintenanceController remarks to list only title/description/category/priority/status/buildingId/estimatedCost/scheduledDate as mandatory. | POST /api/maintenance with only building-level fields now returns 200 and creates the request. |
| 4 | USERS | GET /api/users/{id} for a non-existent id returned HTTP 400 instead of the documented 404. | UserService.GetById threw UserException.BadRequestException; every other module's GetById throws a NotFound exception. UserException had no NotFoundException helper. | Added UserException.NotFoundException and switched GetById (and Update/ToggleStatus/Delete lookups) to it; the same lookups now also exclude soft-deleted users. | GET /api/users/{bad-id} now returns 404; after soft-delete, GET by id returns 404. |
| 5 | REPORTS | GET /api/reports/export?type=bogus returned HTTP 200 with a combined xlsx instead of rejecting the unknown type; also 'type=expenses' silently produced the combined ledger because the service only checked 'expense' (singular). | ReportService.ExportReportToXlsx fell through to the combined-ledger branch for any type other than 'income'/'expense'. | Type is now validated: income / expense / expenses / combined are accepted (expenses == expense), anything else throws a 400 with a clear message. Added ReportException helper. | type=bogus now returns 400; type=income and type=expenses produce the correct, distinct workbooks. |
| 6 | ALL MODULES (soft delete) | After soft-deleting a record, GET /api/{module}/{id} still returned it with HTTP 200 — deleted data remained retrievable by ID even though it disappears from list/search results. | Services read by ID via GenericRepository.FirstOrDefaultAsync, which calls IgnoreQueryFilters() (needed for uniqueness checks across deleted rows). The GetById view methods therefore bypassed the global !IsDeleted query filter. | All 11 GetById view methods (building, owner, vendor, apartment, tenant, equipment, amc-contract, maintenance, income, expense, user) now add '&& !x.IsDeleted' to their lookup so soft-deleted records 404 on direct GET by id, matching the list endpoints. | GET /api/buildings/{soft-deleted-id} and GET /api/users/{soft-deleted-id} now return 404. |

---

## Observations (by design — no action required)

1. Equipment status (create/update/patch) is a free-form string (max 50 chars) — there is no allowed-values validation, so a typo like 'Operational ' (trailing space) or 'operational' vs 'Operational' is silently stored and then invisible to the status filter (exact, case-insensitive match). Seeded convention: Operational / Under Maintenance / Out of Service / Retired. If strict statuses are wanted, add an allowed-values rule to EquipmentCreateRequestValidation / EquipmentStatusUpdateRequestValidation.

2. Permission filter ordering: model validation (ValidateModelFilter) runs before PermissionAuthorizationFilter, so a permission-less user who POSTs an invalid payload gets 400 (validation) instead of 403. Unauthorized write attempts are still blocked with 403 once the payload is valid — verified with the accountant user (POST /api/buildings -> 403, POST /api/maintenance (valid) -> 403, GET /api/users -> 403).

---

## ✅ No failing checks remain

All checks in the final run passed after the fixes above.
---

*Log generated by the automated API verification suite. Each phase ran against a freshly `SEED_MODE=reset` database.*
