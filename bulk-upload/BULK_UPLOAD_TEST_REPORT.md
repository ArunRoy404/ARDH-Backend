# Bulk Upload Test Report — ARDH Property Management System

**Date:** August 22, 2026  
**Test Environment:** In-memory database (SQL Server unavailable), SEED_MODE=reset  
**Test Scope:** All 7 bulk upload modules, 401+ test rows total  
**Method:** Generated XLSX files with 50+ rows per module, uploaded via API, processed via background service, analyzed processed output files

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Test Files Generated](#test-files-generated)
3. [CRITICAL Issues](#critical-issues)
   - [Issue #1: All Valid Rows Fail with Generic Error](#issue-1-all-valid-rows-fail-with-generic-error)
   - [Issue #2: ToReadableError Cannot Extract Service-Level Errors](#issue-2-toreadableerror-cannot-extract-service-level-errors)
4. [MEDIUM Issues](#medium-issues)
   - [Issue #3: Notification/Activity Service Exceptions Not Wrapped](#issue-3-notificationactivity-service-exceptions-not-wrapped)
   - [Issue #4: Cross-Row Side Effects Not Obvious to Users](#issue-4-cross-row-side-effects-not-obvious-to-users)
   - [Issue #5: Background Service Lacks User Context](#issue-5-background-service-lacks-user-context)
5. [LOW Issues](#low-issues)
   - [Issue #6: Equipment Status is Free-Text Not Enum](#issue-6-equipment-status-is-free-text-not-enum)
   - [Issue #7: Maintenance Category is Free-Text Not Enum](#issue-7-maintenance-category-is-free-text-not-enum)
   - [Issue #8: Expense PaymentMethod is Free-Text Not Enum](#issue-8-expense-paymentmethod-is-free-text-not-enum)
   - [Issue #9: No Uniqueness on Equipment/Maintenance](#issue-9-no-uniqueness-on-equipmentmaintenance)
6. [Working Correctly](#working-correctly)
7. [Detailed Error Output by Module](#detailed-error-output-by-module)

---

## Executive Summary

The bulk upload system has **two critical bugs** that render it unusable for its primary purpose: successfully creating records from uploaded XLSX files. While the **parse-level validation layer is excellent** (missing fields, invalid enums, type checks, reference lookups all produce clear, specific errors), every row that passes validation and reaches the service-level `Create()` method fails with the unhelpful generic error:

> "An unexpected error occurred while processing this row. Please check the values and try again."

This means **0% of valid rows succeed** across all 7 modules. The root cause is a gap in exception handling between the `BulkUploadService` and the downstream services it calls.

---

## Test Files Generated

| Module | Rows | Success Cases | Error Cases |
|--------|------|---------------|-------------|
| Owners | 50 | 20 unique valid owners | 7 missing fields, 2 invalid enums, 5 duplicates, 16 edge cases |
| Apartments | 56 | 25 valid apartments | 5 missing fields, 3 lookup errors, 5 invalid types, 2 duplicates, 16 edge cases |
| Tenants | 55 | 15 valid tenants | 7 missing fields, 4 invalid enums, 3 lookup errors, 1 occupancy, 4 duplicates, 21 edge cases |
| Income | 58 | 20 income records | 6 missing fields, 5 invalid enums, 3 lookup errors, 24 edge cases |
| Expenses | 64 | 25 expense records | 8 missing fields, 4 invalid enums, 5 lookup errors, 1 duplicate, 21 edge cases |
| Maintenance | 63 | 25 maintenance records | 7 missing fields, 3 invalid enums, 4 lookup errors, 24 edge cases |
| Equipment | 55 | 25 equipment records | 6 missing fields, 2 invalid dates, 1 lookup error, 21 edge cases |

---

## CRITICAL Issues

### Issue #1: All Valid Rows Fail with Generic Error

**Severity:** 🔴 CRITICAL  
**Modules:** ALL (owners, apartments, tenants, income, expenses, maintenance, equipment)  
**Impact:** The bulk upload feature is completely broken for creating records

**Evidence:**

Every module processed 0 successful rows:

```
Module: OWNERS    | Total: 50 | Success: 0  | Failed: 50
Module: APARTMENTS | Total: 56 | Success: 0  | Failed: 56
Module: TENANTS   | Total: 55 | Success: 0  | Failed: 55
Module: INCOME    | Total: 58 | Success: 0  | Failed: 58
Module: EXPENSES  | Total: 64 | Success: 0  | Failed: 64
Module: MAINTENANCE | Total: 63 | Success: 0 | Failed: 63
Module: EQUIPMENT | Total: 55 | Success: 0  | Failed: 55
```

**Specific Example — Owners:**

Row 1 of `owners_bulk_test.xlsx` — a completely valid, unique owner:

| Field | Value |
|-------|-------|
| FullName | `Bulk Owner 001` |
| Phone | `+91 9800010001` |
| Email | `bulk.owner001@test.com` |
| City | `Mumbai` |
| Address | `1 Test Street` |
| IdType | `Aadhar` |
| IdNumber | `OWNER-TEST-0001` |
| BankName | `ICICI Bank` |
| AccountNumber | `8888810001` |
| IfscCode | `ICIC00010001` |
| Status | `Active` |

**Result:** `Failed | An unexpected error occurred while processing this row. Please check the values and try again.`

This row passes all parse-level validations. No missing fields, no invalid enums, no invalid types. Yet it fails at the service level with a completely uninformative error.

**Specific Example — Apartments:**

Row 1 — valid apartment referencing existing seed building and owner:

| Field | Value |
|-------|-------|
| BuildingName | `Grand Plaza Towers` |
| OwnerName | `Rahul Verma` |
| NestawayId | `NEST-APT-0001` |
| FlatNumber | `1001` |
| ApartmentType | `2 BHK` |
| ExpectedRent | `21000` |

**Result:** `Failed | An unexpected error occurred while processing this row. Please check the values and try again.`

**Specific Example — Equipment:**

Row 1 — valid equipment referencing existing seed building:

| Field | Value |
|-------|-------|
| BuildingName | `Grand Plaza Towers` |
| Name | `Bulk Equipment 001` |
| Type | `Pump` |
| Brand | `Kirloskar` |
| InstallDate | `2025-01-01` |
| Status | `Operational` |

**Result:** `Failed | An unexpected error occurred while processing this row. Please check the values and try again.`

**Processed XLSX output pattern (identical across ALL modules):**

```
Row 2:  Bulk Owner 001 | Failed | An unexpected error occurred while processing this row...
Row 3:  Bulk Owner 002 | Failed | An unexpected error occurred while processing this row...
Row 4:  Bulk Owner 003 | Failed | An unexpected error occurred while processing this row...
...all 20 valid owners fail...
Row 22: (empty)        | Failed | FullName is required.          ← parse error works ✓
Row 23: Missing Phone  | Failed | Phone is required.             ← parse error works ✓
...
```

---

### Issue #2: ToReadableError Cannot Extract Service-Level Errors

**Severity:** 🔴 CRITICAL  
**File:** `src/CleanArchitecture/Application/Services/BulkUploadService.cs` (lines ~1144-1155)  
**Impact:** Any exception not of type `UserFriendlyException` (or not wrapped as its immediate child) produces a useless generic error message

**Root Cause Code:**

```csharp
// BulkUploadService.cs — line 1144
private static string ToReadableError(Exception ex)
{
    if (ex is UserFriendlyException friendly)
    {
        return friendly.Message;
    }
    if (ex.InnerException is UserFriendlyException innerFriendly)
    {
        return innerFriendly.Message;
    }
    return "An unexpected error occurred while processing this row. Please check the values and try again.";
}
```

**The Problem:**

This method only handles two cases:
1. The exception IS a `UserFriendlyException`
2. The exception's FIRST InnerException IS a `UserFriendlyException`

It does NOT handle:
- `AggregateException` wrapping a `UserFriendlyException`
- `DbUpdateException` with a `UserFriendlyException` deeper in the chain
- Any exception thrown OUTSIDE of `ExecuteTransactionAsync` (e.g., in notification/activity services after the transaction commits)
- Exceptions from the in-memory database that don't map to `SqlException` or `DbUpdateException`

**Example Exception Chain (observed):**

```
Exception: InvalidOperationException (from notification service SaveChangesAsync)
  └─ InnerException: null
     → ToReadableError returns "An unexpected error occurred..."
```

vs. what SHOULD happen:

```
Exception: UserFriendlyException (from OwnerException.BadRequestException)
  └─ Message: "Owner with name 'Rahul Verma' already exists."
     → ToReadableError should return this message
```

---

## MEDIUM Issues

### Issue #3: Notification/Activity Service Exceptions Not Wrapped

**Severity:** 🟡 MEDIUM  
**Files:** All service `Create()` methods (OwnerService, ApartmentService, TenantService, etc.)  
**Impact:** Even when the record IS successfully saved to the database, the subsequent notification/activity creation failure causes the entire row to be marked as "Failed"

**Root Cause:**

In every service's `Create()` method, the notification and activity calls happen AFTER the `ExecuteTransactionAsync`:

```csharp
// OwnerService.cs — typical pattern (all services follow this)
public async Task Create(OwnerCreateRequest request, CancellationToken cancellationToken)
{
    // 1. Validation checks (before transaction) — throws UserFriendlyException
    var isNameExist = await _unitOfWork.OwnerRepository.AnyIncludingDeletedAsync(...);
    if (isNameExist) throw OwnerException.BadRequestException("...");

    // 2. Create entity
    var owner = new Owner { ... };

    // 3. Save inside transaction — exceptions wrapped by TransactionException
    await _unitOfWork.ExecuteTransactionAsync(
        async () => await _unitOfWork.OwnerRepository.AddAsync(owner), cancellationToken);

    // 4. Notification/activity AFTER transaction — NOT wrapped! ← BUG
    await _notificationService.CreateNotificationInternal(
        "properties", "Owner Added", $"Owner '{owner.FullName}' was added.", cancellationToken);
}
```

If step 4 throws ANY exception (e.g., `DbUpdateException` from the notification table, `InvalidOperationException` from a null reference, etc.), it propagates as-is to `BulkUploadService.CreateRecord()`:

```csharp
// BulkUploadService.cs — CreateRecord catch block
try
{
    await CreateRecord(record.Module, request, lookup, cancellationToken);
    success++;
}
catch (Exception ex)
{
    _unitOfWork.ClearChangeTracker();
    failed++;
    processedList.Add(PadAndAppendStatus(row, headers.Count, "Failed", ToReadableError(ex)));
    // ↑ ex is NOT a UserFriendlyException → generic error message
}
```

**The record IS saved** to the database (the transaction committed in step 3), but the user sees "Failed" in the processed XLSX, and the success counter doesn't increment. This creates a data integrity issue: the record exists but appears as failed.

---

### Issue #4: Cross-Row Side Effects Not Obvious to Users

**Severity:** 🟡 MEDIUM  
**Modules:** Tenants, Income, Expenses, Apartments  
**Impact:** Users may not understand why some rows fail due to earlier rows in the same file

**Example — Tenants (occupied apartment cascade):**

The seed data has apartment 302 in GPT occupied by Arjun Mehta. The test file has 15 valid tenant rows cycling through 3 vacant apartments (101, 1204-B, 301 in GPT/Oakridge). But 1204-B is actually occupied by John Tenant in the seed data, so:

```
Row 2: Bulk Tenant 001 → GPT/101 (vacant)  → should succeed
Row 3: Bulk Tenant 002 → GPT/1204-B (occupied!) → "Apartment '1204-B' is already occupied"
Row 4: Bulk Tenant 003 → Oakridge/301 (vacant) → should succeed
Row 5: Bulk Tenant 004 → GPT/101 → but now 101 has tenant from Row 2! → "Apartment already occupied"
```

Each successful tenant creation changes apartment occupancy, causing subsequent rows referencing the same apartment to fail. The processed XLSX shows:

```
Row 2:  Bulk Tenant 001 | Success  ← if working correctly
Row 3:  Bulk Tenant 002 | Failed   | Apartment '1204-B' is already occupied
Row 5:  Bulk Tenant 004 | Failed   | Apartment '101' is already occupied (by Row 2's tenant)
```

This is **correct behavior** but the error message doesn't explain that it's because a previous row in the same batch already occupied it.

---

### Issue #5: Background Service Lacks User Context

**Severity:** 🟡 MEDIUM  
**File:** `src/CleanArchitecture/Application/Services/BulkUploadBackgroundService.cs`  
**Impact:** All records created via bulk upload have `CreatedBy = Guid.Empty` instead of the user who initiated the upload

**Root Cause Code:**

```csharp
// BulkUploadBackgroundService.cs — line 37
using var scope = _scopeFactory.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IBulkUploadService>();
await service.ProcessAsync(bulkUploadId, stoppingToken);
```

The background service creates a new DI scope. The `ICurrentUser` (registered as scoped) uses `IHttpContextAccessor`:

```csharp
// CurrentUser.cs
public Guid GetCurrentUserId()
{
    var userIdStr = GetCurrentStringUserId();
    return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
}

public string GetCurrentStringUserId()
{
    var user = _httpContextAccessor.HttpContext?.User;  // ← NULL in background context
    var userIdClaim = user?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
    return userIdClaim?.Value ?? string.Empty;  // ← returns ""
}
```

In the background scope, `HttpContext` is `null`, so `GetCurrentUserId()` returns `Guid.Empty`. Every entity created by bulk upload has:

```csharp
CreatedBy = Guid.Empty  // instead of the actual user who uploaded the file
```

---

## LOW Issues

### Issue #6: Equipment Status is Free-Text Not Enum

**Severity:** 🟢 LOW  
**File:** `src/CleanArchitecture/Shared/Models/Equipment/EquipmentCreateRequest.cs`  
**Impact:** No validation against allowed status values

```csharp
// EquipmentCreateRequest.cs
public string Status { get; set; } = "Operational";
```

The bulk upload parser accepts any string:
```csharp
// BulkUploadService.cs — ParseEquipment
Status = GetRequired(row, h, "Status", "Status", errors) ?? "Operational",
```

Users can upload `"TypoStatus"` or `"123"` and it will be stored as-is. Unlike Income/Expense/Maintenance which validate enums, Equipment has no status validation.

---

### Issue #7: Maintenance Category is Free-Text Not Enum

**Severity:** 🟢 LOW  
**File:** `src/CleanArchitecture/Shared/Models/Maintenance/MaintenanceRequestCreateRequest.cs`  
**Impact:** No validation against allowed category values

```csharp
// MaintenanceRequestCreateRequest.cs
public string Category { get; set; } = string.Empty;
```

```csharp
// BulkUploadService.cs — ParseMaintenance
Category = GetRequired(row, h, "Category", "Category", errors),
```

Any string is accepted for maintenance category. The template suggests values like "Plumbing", "Electrical", "HVAC", but these are not enforced.

---

### Issue #8: Expense PaymentMethod is Free-Text Not Enum

**Severity:** 🟢 LOW  
**File:** `src/CleanArchitecture/Shared/Models/Expenses/ExpenseRecordCreateRequest.cs`  
**Impact:** Unlike Income (which validates `IncomePaymentMethod` enum), Expense accepts any string

```csharp
// ExpenseRecordCreateRequest.cs
public string? PaymentMethod { get; set; }
```

```csharp
// BulkUploadService.cs — ParseExpense
PaymentMethod = GetRequired(row, h, "PaymentMethod", "PaymentMethod", errors),
```

---

### Issue #9: No Uniqueness on Equipment/Maintenance

**Severity:** 🟢 LOW  
**Impact:** Identical equipment or maintenance records can be created without any duplicate detection

**Equipment:** The `EquipmentService.Create()` has no uniqueness check on Name or SerialNumber:
```csharp
// EquipmentService.cs — Create
// Only checks building existence, then saves directly
await _unitOfWork.ExecuteTransactionAsync(
    async () => await _unitOfWork.EquipmentRepository.AddAsync(equipment), cancellationToken);
```

**Maintenance:** Similarly, `MaintenanceRequestService.Create()` has no uniqueness check:
```csharp
// Only checks building/apartment/vendor/equipment existence
await _unitOfWork.ExecuteTransactionAsync(async () =>
{
    await _unitOfWork.MaintenanceRequestRepository.AddAsync(maintenanceRequest);
    ...
}, cancellationToken);
```

Compare with **Owners** which checks 5 unique fields (name, email, phone, ID, account) and **Apartments** which checks flat number + Nestaway ID uniqueness.

---

## Working Correctly

Despite the critical service-level issues, the **parse-level validation layer is excellent**:

### ✅ Required Field Validation (all modules)
Every required field produces a clear `"X is required."` error when empty:
- Owners: FullName, Phone, Email, IdNumber, BankName, AccountNumber, IfscCode
- Apartments: BuildingName, OwnerName, NestawayId, FlatNumber, ApartmentType
- Tenants: BuildingName, FlatNumber, FullName, Phone, IdNumber, MoveInDate, LeaseStartDate, MonthlyRent
- Income: Amount, PaymentMethod, Status, PaymentDate
- Expenses: ExpenseHead, SpecificItem, Amount, BuildingName, ExpenseDate, PaymentMethod, Status
- Maintenance: Title, Description, Category, BuildingName, Status, EstimatedCost, ScheduledDate
- Equipment: BuildingName, Name, Type, Brand, Status, InstallDate

### ✅ Enum Validation
Invalid enum values produce clear errors listing valid options:
- `"IdType 'DrivingLicense' is invalid. Valid values: Aadhar, PanCard, Passport."`
- `"Status 'Suspended' is invalid. Valid values: Active, Inactive, Pending."`
- `"IncomeEntity 'InvalidEntity' is invalid. Valid values: ApartmentWise, GeneralOthers."`
- `"PaymentMethod 'CryptoCurrency' is invalid. Valid values: TransferFromNestaway, DirectFromTenant, Cash, BankTransfer, Cheque, Others."`

### ✅ Data Type Validation
- `"HasBalcony 'maybe' is not a valid boolean. Use true/false or 1/0."`
- `"Floor 'abc' is not a valid whole number."`
- `"Amount 'not-a-number' is not a valid number."`
- `"MoveInDate 'not-a-date' is not a valid date. Use YYYY-MM-DD (e.g. 2026-08-07)."`

### ✅ Reference Lookup Validation
- `"Building 'Ghost Building' was not found. The building must be created before it can be referenced."`
- `"Owner 'Non Existent Owner' was not found. The owner must be created before it can be referenced."`
- `"Vendor 'Sunil Plumbing' was not found. The vendor must be created before it can be referenced."`
- `"Apartment with flat number '9999' was not found in building 'Grand Plaza Towers'."`

### ✅ Global Column Check
Missing required columns in the header triggers a global error:
```
"Missing required column(s): buildingname, ownername. Download the template for 'apartments' to see the exact column names."
```
All rows are marked as Failed with this global error.

### ✅ Special Validation Logic
- `"FlatNumber is required when Entity is ApartmentSpecific."` (expenses)
- `"Income entries can only be recorded for occupied apartments."` (income — service level)
- `"Apartment '302' is already occupied by another tenant."` (tenants — service level)

### ✅ Header Normalization (CsvHelper)
Headers are normalized (lowercased, spaces/underscores/dashes removed), so all of these resolve to the same column:
- `BuildingName`, `building_name`, `Building Name`, `building-name`

### ✅ Empty Row Handling
Rows where all cells are whitespace are silently skipped (not counted as success or failure).

---

## Detailed Error Output by Module

### Owners (50 rows)

**Error Pattern Summary:**
| Error Message | Count | Type |
|--------------|-------|------|
| An unexpected error occurred while processing this row... | 39 | Service-level (CRITICAL) |
| FullName is required. | 2 | Parse-level ✓ |
| Phone is required. | 1 | Parse-level ✓ |
| Email is required. | 1 | Parse-level ✓ |
| IdNumber is required. | 1 | Parse-level ✓ |
| BankName is required. | 1 | Parse-level ✓ |
| AccountNumber is required. | 1 | Parse-level ✓ |
| IfscCode is required. | 1 | Parse-level ✓ |
| IdType 'DrivingLicense' is invalid... | 1 | Parse-level ✓ |
| Status 'Suspended' is invalid... | 1 | Parse-level ✓ |

**Rows 1-20 (valid unique owners):** All fail with "An unexpected error occurred..."  
**Rows 21-29 (missing fields + invalid enums):** All produce correct parse errors ✓  
**Rows 30-50 (duplicates + edge cases):** All fail with "An unexpected error occurred..."

### Apartments (56 rows)

| Error Message | Count | Type |
|--------------|-------|------|
| An unexpected error occurred while processing this row... | 41 | Service-level (CRITICAL) |
| BuildingName is required. | 2 | Parse-level ✓ |
| OwnerName is required. | 1 | Parse-level ✓ |
| NestawayId is required. | 1 | Parse-level ✓ |
| FlatNumber is required. | 1 | Parse-level ✓ |
| ApartmentType is required. | 1 | Parse-level ✓ |
| Building 'Non Existent Building' was not found... | 1 | Parse-level ✓ |
| Owner 'Non Existent Owner' was not found... | 1 | Parse-level ✓ |
| HasBalcony 'maybe' is not a valid boolean... | 1 | Parse-level ✓ |
| Floor 'abc' is not a valid whole number. | 1 | Parse-level ✓ |
| AreaSqft 'not-a-number' is not a valid number. | 1 | Parse-level ✓ |
| (additional type checks) | 4 | Parse-level ✓ |

### Tenants (55 rows)

| Error Message | Count | Type |
|--------------|-------|------|
| An unexpected error occurred while processing this row... | 32 | Service-level (CRITICAL) |
| Apartment '1204-B' is already occupied... | 5 | Service-level (correct behavior) |
| BuildingName is required. | 3 | Parse-level ✓ |
| FullName is required. | 3 | Parse-level ✓ |
| BuildingName and FlatNumber are required... | 1 | Parse-level ✓ |
| Phone/IdNumber is required. | 2 | Parse-level ✓ |
| IdType/Status invalid enum | 2 | Parse-level ✓ |
| MoveInDate 'not-a-date'... | 1 | Parse-level ✓ |
| (additional checks) | 6 | Parse-level ✓ |

### Income (58 rows)

| Error Message | Count | Type |
|--------------|-------|------|
| An unexpected error occurred while processing this row... | 32 | Service-level (CRITICAL) |
| Income entries can only be recorded for occupied apartments... | 13 | Service-level (correct behavior) |
| Amount is required. | 3 | Parse-level ✓ |
| IncomeEntity/IncomeType/PaymentMethod/Status invalid enum | 4 | Parse-level ✓ |
| Amount 'not-a-number'... | 1 | Parse-level ✓ |
| PaymentDate 'not-a-date'... | 1 | Parse-level ✓ |
| (additional checks) | 4 | Parse-level ✓ |

### Expenses (64 rows)

| Error Message | Count | Type |
|--------------|-------|------|
| An unexpected error occurred while processing this row... | 34 | Service-level (CRITICAL) |
| Vendor 'Sunil Plumbing' was not found... | 6 | Parse-level ✓ (name mismatch — seed has "Sunil Kumar") |
| Vendor 'Sharma Elevator' was not found... | 7 | Parse-level ✓ (name mismatch — seed has "Rajesh Sharma") |
| ExpenseDate '2026-08-33' not valid... | 2 | Parse-level ✓ |
| (required fields + enum checks) | 15 | Parse-level ✓ |

### Maintenance (63 rows)

| Error Message | Count | Type |
|--------------|-------|------|
| An unexpected error occurred while processing this row... | 31 | Service-level (CRITICAL) |
| Vendor 'Sunil Plumbing' was not found... | 8 | Parse-level ✓ |
| Vendor 'Sharma Elevator' was not found... | 7 | Parse-level ✓ |
| StartDate '2026-08-33' not valid... | 2 | Parse-level ✓ |
| (required fields + enum checks) | 15 | Parse-level ✓ |

### Equipment (55 rows)

| Error Message | Count | Type |
|--------------|-------|------|
| An unexpected error occurred while processing this row... | 46 | Service-level (CRITICAL) |
| BuildingName is required. | 1 | Parse-level ✓ |
| Name/Type/Brand/Status is required. | 4 | Parse-level ✓ |
| InstallDate/WarrantyExpiryDate not valid... | 3 | Parse-level ✓ |
| Building 'Ghost Building' was not found... | 1 | Parse-level ✓ |

---

## Recommendations (Priority Order)

1. **Fix `ToReadableError()`** to recursively walk the full exception chain (not just 2 levels) and extract any `UserFriendlyException` message found anywhere in the chain.

2. **Wrap post-transaction exceptions** in `UserFriendlyException` in all service `Create()` methods, or ensure notification/activity services throw only `UserFriendlyException`.

3. **Add exception logging** in `BulkUploadService.CreateRecord()` catch block so the actual exception type and message appear in server logs even when the processed XLSX shows the generic error.

4. **Propagate user context** to the background service by storing the `CreatedBy` user ID on the `BulkUpload` record and passing it through the scope.

5. **Add uniqueness constraints** for Equipment (Name) and Maintenance (Title) at the service level, consistent with how Owners and Apartments handle uniqueness.

6. **Consider enum-izing** Equipment Status, Maintenance Category, and Expense PaymentMethod for consistency with other modules.
