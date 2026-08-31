# Apartment Charge History

A per-apartment record of every rent, maintenance charge, and water charge change, showing exactly when each rate was active — answering "what was this apartment's maintenance charge in June?" not just "what is it now?"

## What it tracks

For every apartment, three independent timelines are kept — **Rent** (the apartment's `ExpectedRent`), **Maintenance** (`MaintenanceCharge`), and **Water** (`WaterCharge`) — each as a sequence of segments:

- **`EffectiveFrom` / `EffectiveTo`** — the date range the amount actually applied for. `EffectiveTo = null` means the segment is currently active.
- Segments never overlap, and there's never a gap: the moment one closes, the next opens the following day (or the same day, for a brand-new apartment).

## How it stays in sync

Nothing about how `Apartment.ExpectedRent`/`MaintenanceCharge`/`WaterCharge` are displayed changes — they remain the live, current values shown everywhere (apartment list, details, dropdowns), exactly as before. The history table is additive, updated as a side effect whenever those fields change:

- **On apartment creation**, one open-ended segment (`EffectiveTo = null`) is seeded for each of the three charge types, dated from the apartment's creation date.
- **On update**, each of the three fields is checked independently — only a field that actually changed gets a new segment:
  1. If no history exists yet for that charge type (a pre-feature apartment), a fresh open-ended segment is inserted.
  2. If the currently-open segment hasn't taken effect yet (it starts in the future), its amount is corrected in place — no new segment, since nothing historical happened yet.
  3. Otherwise, the currently-open segment is closed (`EffectiveTo` = yesterday) and a new open-ended segment starts today at the new amount.

This is the same mechanism `TenantRentHistory` already uses for tenant lease rent — applied here to the apartment's own charge fields instead.

## API

### `GET /api/apartments/{id}/charge-history`

Returns the full timeline for one apartment, across all three charge types. No pagination — an apartment's history is small. Falls under the existing `apartments` permission; like every other `GET /api/apartments/*` route, it's open to any authenticated user.

```json
[
  { "chargeType": "Rent", "amount": 85000.00, "effectiveFrom": "2026-07-20T00:00:00", "isCurrent": true },
  { "chargeType": "Maintenance", "amount": 4000.00, "effectiveFrom": "2026-07-20T00:00:00", "effectiveTo": "2026-08-14T00:00:00", "isCurrent": false },
  { "chargeType": "Maintenance", "amount": 4500.00, "effectiveFrom": "2026-08-15T00:00:00", "isCurrent": true },
  { "chargeType": "Water", "amount": 800.00, "effectiveFrom": "2026-07-20T00:00:00", "isCurrent": true }
]
```

`effectiveTo` is omitted entirely (not shown as `null`) for the currently active segment of each charge type — the API's global JSON setting drops null fields rather than emitting them.

### Errors

| Status | Cause |
|---|---|
| 404 | Apartment does not exist |

## Worked example (seed data)

Apartment 302 (Grand Plaza Towers, flat 302) demonstrates a real change: its maintenance charge was ₹4,000 from creation through 14 August, then raised to ₹4,500 from 15 August onward — matching the apartment's current `MaintenanceCharge` value exactly. Its Rent and Water charges have been flat since creation, so each shows as a single open-ended segment.

Every other seeded apartment has a single flat-rate segment per charge type (no changes yet), matching its current `ExpectedRent`/`MaintenanceCharge`/`WaterCharge`.

## Known limitations

- **Pre-existing apartments (from before this feature shipped) are backfilled** via the migration with one open-ended segment per charge type, dated from the apartment's creation date — this is a reasonable approximation, not a true history, since the real rate may have changed earlier without being recorded.
- **History rows have no ID exposed via the API** — they're never fetched or mutated individually, only ever returned as part of the full per-apartment timeline.
