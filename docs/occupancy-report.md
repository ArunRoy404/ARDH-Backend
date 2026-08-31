# Occupancy Report

A portfolio-wide report answering, for any date range: how many days was each apartment occupied vs. vacant, and how much rent was actually earned vs. left on the table.

## What it computes, per apartment

For a selected `fromDate` → `toDate`:

1. **The window is clamped to today.** If `toDate` is in the future, the report uses today instead — it never calculates future occupancy. If `fromDate` itself is after today, the request is rejected (400).
2. **Every lease (`Tenant` record) for the apartment is checked for overlap** with the window — including leases that have since ended (moved-out tenants), not just the current one.
3. **Only the overlapping slice of each lease counts**: from whichever is later (lease start or `fromDate`), to whichever is earlier (lease end, `toDate`, or today).
4. **Multiple leases in the window are added together.** If an apartment changed tenants mid-window, both leases' occupied days contribute.
5. **Whatever's left over is vacant**: `Vacant Days = Total Report Days − Occupied Days`.
6. **Months are calendar-accurate**, not `totalDays / 30` — e.g. a full 365-day non-leap year shows as "12 months, 0 days," not "12.17 months."

## Rent figures

For each apartment, the report returns three amounts:

- **Expected Occupied Rent** — rent earned during the occupied days.
- **Vacancy Rent Value** — rent that *could have* been earned during the vacant days.
- **Total Potential Rent** — the two added together.

### Using the rent that actually applied at the time

`Apartment.ExpectedRent` (the current asking rent) is never applied retroactively. Instead:

- Every lease (`Tenant`) has its own `MonthlyRent`, captured at creation.
- If that rent is changed later while the tenant stays in place, the system now keeps a history of it (`TenantRentHistory` — one row per rate, with an effective-from/to date) instead of silently overwriting the old value.
- The report walks that history and prorates each rate across only the calendar days it actually applied to:

  ```
  Period Rent = Monthly Rent × Applicable Days ÷ Days in that Calendar Month
  ```

**Worked example** (matches the spec this feature was built against):

| Month | Status | Rent |
|---|---|---|
| January | Occupied | ₹10,000 |
| February | Vacant | — |
| March | Occupied (new lease) | ₹12,000 |

Result: **Expected Occupied Rent = ₹22,000**, **Vacancy Rent Value = ₹10,000**, **Total Potential Rent = ₹32,000**.

### What rent applies to a vacant stretch?

This is the one place the report has to make a judgment call, since a vacant apartment isn't earning anything real — the figure is "what it *would have* earned."

- **A gap between two known leases** (a tenant moved out, another moved in later) is valued at the **outgoing tenant's rent** — this is exactly the February row above: it's priced at January's ₹10,000, not March's ₹12,000 and not today's `ExpectedRent`.
- **An open-ended vacancy** — before the apartment's very first tenant, or after its most recent one with nothing lined up next — is valued at the apartment's **current `ExpectedRent`**, since there's no lease rate to anchor to (or the vacancy is ongoing right now, so today's asking rent is the more meaningful number).

### Partial months

Rent is prorated by real calendar days in that specific month, not a flat 30-day assumption:

> A tenant occupying an apartment at ₹31,000/month for just 1–15 January owes `31,000 × 15 ÷ 31 = ₹15,000` for that stretch — not `31,000 × 15 ÷ 30`.

### Currency rounding

Per-apartment rent figures are rounded to 2 decimal places, and `Total Potential Rent` is always exactly `Expected Occupied Rent + Vacancy Rent Value` as displayed (rounded once, not independently, so the two line items always add up to the total shown).

## The clip example from the spec

> Apartment 201 has a lease from 2 May 2025 to 1 May 2026. The user runs the report for 2 May 2025 → 31 Dec 2025.

Only 2 May 2025 → 31 Dec 2025 counts as occupied — the report doesn't reach into 2026 just because the lease technically runs that long, because the requested window ends 31 Dec 2025.

## One occupancy record per apartment at a time

A data-integrity rule this feature depends on: **two active leases can no longer overlap in their dates for the same apartment.** Previously the system only checked "does this apartment already have *a* current tenant," which didn't actually look at dates — a backdated/historical lease could be blocked even when it didn't really conflict, and (in principle) two genuinely overlapping active leases were possible. Creating or updating a tenant now checks real date-range overlap against every other *active* lease on that apartment.

## API

All endpoints require the `occupancy_reports` permission-bearing role for non-GET use (in practice, reads are open to any authenticated user, matching every other report module — see Permissions below). Base route: `/api/occupancy-reports`.

### `GET /api/occupancy-reports`

Paginated, per-apartment breakdown.

| Query param | Required | Description |
|---|---|---|
| `fromDate` | Yes | Report window start (`YYYY-MM-DD`) |
| `toDate` | Yes | Report window end (`YYYY-MM-DD`) — clamped to today |
| `buildingId` | No | Filter to one building |
| `page` / `pageSize` | No | Defaults `1` / `10` |

```json
{
  "items": [
    {
      "apartmentId": "a1f3b822-...",
      "flatNumber": "302",
      "buildingId": "b1f7b822-...",
      "buildingName": "Grand Plaza Towers",
      "totalReportDays": 365,
      "occupiedDays": 244,
      "vacantDays": 121,
      "occupiedDurationDisplay": "8 months, 1 day",
      "vacantDurationDisplay": "3 months, 30 days",
      "expectedOccupiedRent": 692000.00,
      "vacancyRentValue": 342833.33,
      "totalPotentialRent": 1034833.33
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10
}
```

### `GET /api/occupancy-reports/summary`

Same filters, no pagination — one object with portfolio-wide totals (`apartmentCount`, `totalOccupiedDays`, `totalVacantDays`, `totalExpectedOccupiedRent`, `totalVacancyRentValue`, `totalPotentialRent`).

### `GET /api/occupancy-reports/export`

Same filters, returns an `.xlsx` workbook with the same columns as the list endpoint.

### Errors

| Status | Cause |
|---|---|
| 400 | `fromDate`/`toDate` missing, `fromDate` after `toDate`, or `fromDate` after today |

## Permissions

New module: `occupancy_reports`, defaulted to `admin` and `property_manager` roles. Like every other reporting module (`income`, `expenses`, `reports`), reads are open to any authenticated user — the permission exists for consistency and any future write endpoint, not to lock down viewing today.

## Known limitations

- **Historical accuracy depends on lease data being clean going forward.** Two active leases can no longer be created overlapping, but pre-existing overlapping data (if any existed before this feature shipped) is handled defensively — the earlier-starting lease claims the contested days first — rather than double-counting, but it won't retroactively fix bad historical dates.
- **A tenant's own `MonthlyRent` can still be edited directly on their record** (as opposed to being tracked automatically) — the system now keeps history for that going forward, but only for changes made through the normal update flow.
- **Report generation loads all matching apartments into memory** before paginating, the same pattern the existing Income/Expense reports already use — fine at the scale of a single portfolio, not built for huge multi-tenant deployments.
