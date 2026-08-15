# API Test — Success Log

- **Generated:** 2026-08-15 10:41 UTC
- **Environment:** SQL Server (`ARDHDB`) — wiped and re-seeded with `SEED_MODE=reset` before every phase
- **Total checks:** 263 | **Passed:** 261 | **Failed:** 0
- **Server:** `http://localhost:5240` (JWT-cookie auth; admin `admin@gmail.com` / `P@ssw0rd`)

> Every check below ran against a freshly wiped + re-seeded database. The API was restarted with
> `SEED_MODE=reset` before each module group, so all data was pristine for every API.

---

## AUTH

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | sign-in invalid password | POST | `/api/auth/sign-in` | 400 | 400 |
| 2 | sign-in non-existent email | POST | `/api/auth/sign-in` | 400 | 400 |
| 3 | sign-in valid admin | POST | `/api/auth/sign-in` | 200 (signed in) | 200 |
| 4 | profile authenticated | GET | `/api/auth/profile` | 200 | 200 |
| 5 | profile unauthenticated -> 401 | GET | `/api/auth/profile` | 401 | 401 |
| 6 | forgot-password valid email | POST | `/api/auth/forgot-password` | 200 (sent) | 200 |
| 7 | verify-otp wrong otp | POST | `/api/auth/verify-otp` | 400 (wrong) | 400 |
| 8 | verify-otp correct | POST | `/api/auth/verify-otp` | 200 (verified) | 200 |
| 9 | reset-password mismatch confirm | POST | `/api/auth/reset-password` | 400 (match) | 400 |
| 10 | reset-password correct | POST | `/api/auth/reset-password` | 200 (reset) | 200 |
| 11 | sign-in manager with NEW password | POST | `/api/auth/sign-in` | 200 | 200 |
| 12 | sign-in manager with OLD password rejected | POST | `/api/auth/sign-in` | 400 | 400 |
| 13 | resend-otp | POST | `/api/auth/resend-otp` | 200 (sent) | 200 |
| 14 | forgot-password non-existent email | POST | `/api/auth/forgot-password` | 400 | 400 |
| 15 | logout | DELETE | `/api/auth/logout` | 200 | 200 |
| 16 | profile after logout -> 401 | GET | `/api/auth/profile` | 401 | 401 |

## SETTINGS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | get settings | GET | `/api/settings` | 200 | 200 |
| 2 | get public settings (no auth) | GET | `/api/settings/public` | 200 | 200 |
| 3 | update settings valid | PUT | `/api/settings` | 200 (updated) | 200 |
| 4 | update settings missing fields -> 400 | PUT | `/api/settings` | 400 | 400 |
| 5 | update password wrong current | PUT | `/api/settings/password` | 400 () | 400 |
| 6 | update password correct | PUT | `/api/settings/password` | 200 (updated) | 200 |
| 7 | update password mismatch confirm -> 400 | PUT | `/api/settings/password` | 400 | 400 |

## USERS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list users | GET | `/api/users` | 200 | 200 |
| 2 | get user by id (admin) | GET | `/api/users/7ca6dfd0-bfd8-4f10-977b-608b8b4081c7` | 200 | 200 |
| 3 | get user bad id -> 404 | GET | `/api/users/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 4 | create user valid | POST | `/api/users` | 200 (created) | 200 |
| 5 | sign-in as newly created user | POST | `/api/auth/sign-in` | 200 | 200 |
| 6 | create user duplicate email | POST | `/api/users` | 400 (exists) | 400 |
| 7 | create user password mismatch -> 400 | POST | `/api/users` | 400 | 400 |
| 8 | update user | PUT | `/api/users/1e45a98a-abb7-4803-85d9-ce548ec6ca2d` | 200 (updated) | 200 |
| 9 | toggle user status | PATCH | `/api/users/1e45a98a-abb7-4803-85d9-ce548ec6ca2d/toggle-status` | 200 (toggled) | 200 |
| 10 | delete user without password -> 400 | DELETE | `/api/users/1e45a98a-abb7-4803-85d9-ce548ec6ca2d` | 400 | 400 |
| 11 | delete user wrong password -> 400 | DELETE | `/api/users/1e45a98a-abb7-4803-85d9-ce548ec6ca2d?password=wrongpass` | 400 | 400 |
| 12 | delete user with password | DELETE | `/api/users/1e45a98a-abb7-4803-85d9-ce548ec6ca2d?password=NewAdm@123` | 200 (deleted) | 200 |
| 13 | deleted user not found -> 404 | GET | `/api/users/1e45a98a-abb7-4803-85d9-ce548ec6ca2d` | 404 | 404 |

## NOTIFICATIONS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list notifications | GET | `/api/notifications` | 200 | 200 |
| 2 | notification count | GET | `/api/notifications/count` | 200 | 200 |
| 3 | mark notification read | PATCH | `/api/notifications/97af452e-d210-42a1-89a3-7e0f8aa9c06d/read` | 200 (read) | 200 |
| 4 | mark all read | PATCH | `/api/notifications/read-all` | 200 (read) | 200 |
| 5 | list filtered is_read=true | GET | `/api/notifications?is_read=true` | 200 | 200 |
| 6 | delete notification | DELETE | `/api/notifications/97af452e-d210-42a1-89a3-7e0f8aa9c06d` | 200 (deleted) | 200 |
| 7 | clear all notifications | DELETE | `/api/notifications/clear-all` | 200 (cleared) | 200 |

## ACTIVITIES

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list activities | GET | `/api/activities` | 200 | 200 |

## UPLOAD

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | upload xlsx valid | POST | `/api/upload/xlsx` | 200 () | 200 |
| 2 | upload image invalid ext -> 400 | POST | `/api/upload/image` | 400 () | 400 |
| 3 | upload document pdf valid | POST | `/api/upload/document` | 200 () | 200 |
| 4 | upload document invalid ext -> 400 | POST | `/api/upload/document` | 400 () | 400 |
| 5 | upload xlsx csv rejected | POST | `/api/upload/xlsx` | 400 () | 400 |
| 6 | upload id-proof pdf valid | POST | `/api/upload/id-proof` | 200 () | 200 |
| 7 | upload id-proof invalid ext -> 400 | POST | `/api/upload/id-proof` | 400 () | 400 |
| 8 | delete uploaded file | DELETE | `/api/upload/f2ea5ca6-97ff-450b-b8e1-a89c928bd56a` | 200 (deleted) | 200 |
| 9 | delete non-existent file -> 404 | DELETE | `/api/upload/doesnotexist` | 404 | 404 |

## BUILDINGS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list buildings | GET | `/api/buildings` | 200 | 200 |
| 2 | list buildings search | GET | `/api/buildings?search=Grand` | 200 | 200 |
| 3 | get building by id | GET | `/api/buildings/b1f7b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 4 | get building bad id -> 404 | GET | `/api/buildings/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 5 | get building stats | GET | `/api/buildings/b1f7b822-29c4-52a8-ad29-c8be5d491f24/stats` | 200 | 200 |
| 6 | create building valid | POST | `/api/buildings` | 200 (created) | 200 |
| 7 | create building duplicate name (case-insensitive) | POST | `/api/buildings` | 400 (exists) | 400 |
| 8 | create building missing required -> 400 | POST | `/api/buildings` | 400 | 400 |
| 9 | update building | PUT | `/api/buildings/03f950a5-92b4-407a-b620-c9299af28d15` | 200 (updated) | 200 |
| 10 | update building duplicate name -> 400 | PUT | `/api/buildings/03f950a5-92b4-407a-b620-c9299af28d15` | 400 | 400 |
| 11 | delete building without password -> 400 | DELETE | `/api/buildings/03f950a5-92b4-407a-b620-c9299af28d15` | 400 | 400 |
| 12 | delete building wrong password -> 400 | DELETE | `/api/buildings/03f950a5-92b4-407a-b620-c9299af28d15?password=wrong` | 400 | 400 |
| 13 | delete building with password | DELETE | `/api/buildings/03f950a5-92b4-407a-b620-c9299af28d15?password=********` | 200 (deleted) | 200 |
| 14 | deleted building get -> 404 | GET | `/api/buildings/03f950a5-92b4-407a-b620-c9299af28d15` | 404 | 404 |

## OWNERS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list owners | GET | `/api/owners` | 200 | 200 |
| 2 | get owner by id | GET | `/api/owners/b3f3b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 3 | get owner bad id -> 404 | GET | `/api/owners/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 4 | create owner valid | POST | `/api/owners` | 200 (created) | 200 |
| 5 | create owner duplicate name (case-insensitive) -> 400 | POST | `/api/owners` | 400 | 400 |
| 6 | create owner duplicate email -> 400 | POST | `/api/owners` | 400 | 400 |
| 7 | create owner duplicate phone -> 400 | POST | `/api/owners` | 400 | 400 |
| 8 | create owner duplicate id number -> 400 | POST | `/api/owners` | 400 | 400 |
| 9 | update owner | PUT | `/api/owners/3bba1d79-81c9-4c29-b002-7aa17d8f74ad` | 200 (updated) | 200 |
| 10 | update owner duplicate account -> 400 | PUT | `/api/owners/3bba1d79-81c9-4c29-b002-7aa17d8f74ad` | 400 | 400 |
| 11 | delete owner with password | DELETE | `/api/owners/3bba1d79-81c9-4c29-b002-7aa17d8f74ad?password=********` | 200 (deleted) | 200 |
| 12 | owners export xlsx | GET | `/api/owners/download-xlsx` | 200 | 200 |
| 13 | owners filter status | GET | `/api/owners?status=Active` | 200 | 200 |

## VENDORS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list vendors | GET | `/api/vendors` | 200 | 200 |
| 2 | get vendor by id | GET | `/api/vendors/f7a3b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 3 | get vendor bad id -> 404 | GET | `/api/vendors/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 4 | create vendor valid | POST | `/api/vendors` | 200 (created) | 200 |
| 5 | create vendor duplicate email -> 400 | POST | `/api/vendors` | 400 | 400 |
| 6 | create vendor duplicate gst -> 400 | POST | `/api/vendors` | 400 | 400 |
| 7 | update vendor | PUT | `/api/vendors/efc6db87-cc6e-4550-9a64-919691a81847` | 200 (updated) | 200 |
| 8 | update vendor duplicate phone -> 400 | PUT | `/api/vendors/efc6db87-cc6e-4550-9a64-919691a81847` | 400 | 400 |
| 9 | delete vendor with password | DELETE | `/api/vendors/efc6db87-cc6e-4550-9a64-919691a81847?password=********` | 200 (deleted) | 200 |

## APARTMENTS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list apartments | GET | `/api/apartments` | 200 | 200 |
| 2 | get apartment by id | GET | `/api/apartments/a1f3b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 3 | get apartment bad id -> 404 | GET | `/api/apartments/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 4 | list apartments filter building | GET | `/api/apartments?buildingId=b1f7b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 5 | list apartments filter status Vacant | GET | `/api/apartments?status=Vacant` | 200 | 200 |
| 6 | create apartment valid | POST | `/api/apartments` | 200 (created) | 200 |
| 7 | create apartment duplicate nestawayId (case-insensitive) -> 400 | POST | `/api/apartments` | 400 | 400 |
| 8 | create apartment duplicate flat same building -> 400 | POST | `/api/apartments` | 400 | 400 |
| 9 | create apartment same flat other building (per-building unique) | POST | `/api/apartments` | 200 (created) | 200 |
| 10 | create apartment bad building -> 400 | POST | `/api/apartments` | 400 | 400 |
| 11 | create apartment bad owner -> 400 | POST | `/api/apartments` | 400 | 400 |
| 12 | create apartment missing flat/apartmentType -> 400 | POST | `/api/apartments` | 400 | 400 |
| 13 | update apartment | PUT | `/api/apartments/ca758cca-00ee-4422-bdad-6f5a6c48af18` | 200 (updated) | 200 |
| 14 | update apartment nestaway duplicate -> 400 | PUT | `/api/apartments/ca758cca-00ee-4422-bdad-6f5a6c48af18` | 400 | 400 |
| 15 | delete apartment with password | DELETE | `/api/apartments/ca758cca-00ee-4422-bdad-6f5a6c48af18?password=********` | 200 (deleted) | 200 |
| 16 | apartments export xlsx | GET | `/api/apartments/download-xlsx` | 200 | 200 |

## TENANTS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list tenants | GET | `/api/tenants` | 200 | 200 |
| 2 | get tenant by id | GET | `/api/tenants/a3f3b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 3 | get tenant bad id -> 404 | GET | `/api/tenants/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 4 | create tenant valid (vacant flat) | POST | `/api/tenants` | 200 (created) | 200 |
| 5 | create tenant duplicate email -> 400 | POST | `/api/tenants` | 400 | 400 |
| 6 | create tenant duplicate idNumber -> 400 | POST | `/api/tenants` | 400 | 400 |
| 7 | create tenant occupied apartment -> 400 | POST | `/api/tenants` | 400 | 400 |
| 8 | create tenant bad apartment -> 400 | POST | `/api/tenants` | 400 | 400 |
| 9 | update tenant | PUT | `/api/tenants/b4a6b987-b2a3-4f80-9ddf-3ef046fbd5c7` | 200 (updated) | 200 |
| 10 | update tenant to occupied apartment -> 400 | PUT | `/api/tenants/b4a6b987-b2a3-4f80-9ddf-3ef046fbd5c7` | 400 | 400 |
| 11 | delete tenant with password | DELETE | `/api/tenants/b4a6b987-b2a3-4f80-9ddf-3ef046fbd5c7?password=********` | 200 (deleted) | 200 |
| 12 | tenants export xlsx | GET | `/api/tenants/download-xlsx` | 200 | 200 |

## TENANT-MOVE-OUT

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | get seeded move-out for John | GET | `/api/tenants/c1f7b822-29c4-52a8-ad29-c8be5d491f24/move-out` | 200 | 200 |
| 2 | create move-out for Arjun | POST | `/api/tenants/a3f3b822-29c4-52a8-ad29-c8be5d491f24/move-out` | 200 (created) | 200 |
| 3 | create duplicate move-out -> 400 | POST | `/api/tenants/a3f3b822-29c4-52a8-ad29-c8be5d491f24/move-out` | 400 | 400 |
| 4 | get move-out for Arjun | GET | `/api/tenants/a3f3b822-29c4-52a8-ad29-c8be5d491f24/move-out` | 200 | 200 |
| 5 | get move-out via move-out-records alias | GET | `/api/tenants/a3f3b822-29c4-52a8-ad29-c8be5d491f24/move-out-records` | 200 | 200 |
| 6 | update move-out for Arjun | PUT | `/api/tenants/a3f3b822-29c4-52a8-ad29-c8be5d491f24/move-out` | 200 (updated) | 200 |
| 7 | delete move-out for Arjun | DELETE | `/api/tenants/a3f3b822-29c4-52a8-ad29-c8be5d491f24/move-out` | 200 (deleted) | 200 |
| 8 | move-out 404 after delete | GET | `/api/tenants/a3f3b822-29c4-52a8-ad29-c8be5d491f24/move-out` | 404 | 404 |

## EQUIPMENT

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list equipment | GET | `/api/equipment` | 200 | 200 |
| 2 | get equipment by id | GET | `/api/equipment/e2c7b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 3 | get equipment bad id -> 404 | GET | `/api/equipment/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 4 | create equipment valid | POST | `/api/equipment` | 200 (created) | 200 |
| 5 | create equipment bad building -> 400 | POST | `/api/equipment` | 400 | 400 |
| 6 | create equipment arbitrary status accepted (free-form, observed) | POST | `/api/equipment` | 200 | 200 |
| 7 | update equipment | PUT | `/api/equipment/9d0ca859-ffda-44d8-861d-ca0ed030ce2e` | 200 (updated) | 200 |
| 8 | update equipment status valid | PATCH | `/api/equipment/9d0ca859-ffda-44d8-861d-ca0ed030ce2e/status` | 200 (updated) | 200 |
| 9 | update equipment status arbitrary accepted (free-form, observed) | PATCH | `/api/equipment/9d0ca859-ffda-44d8-861d-ca0ed030ce2e/status` | 200 | 200 |
| 10 | delete equipment with password | DELETE | `/api/equipment/9d0ca859-ffda-44d8-861d-ca0ed030ce2e?password=********` | 200 (deleted) | 200 |
| 11 | equipment export xlsx | GET | `/api/equipment/download-xlsx` | 200 | 200 |

## MAINTENANCE

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list maintenance | GET | `/api/maintenance` | 200 | 200 |
| 2 | maintenance stats | GET | `/api/maintenance/stats` | 200 | 200 |
| 3 | get maintenance by id | GET | `/api/maintenance/c1f3b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 4 | get maintenance bad id -> 404 | GET | `/api/maintenance/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 5 | create maintenance valid (full) | POST | `/api/maintenance` | 200 (created) | 200 |
| 6 | create maintenance missing title -> 400 | POST | `/api/maintenance` | 400 | 400 |
| 7 | create maintenance invalid priority -> 400 | POST | `/api/maintenance` | 400 | 400 |
| 8 | create maintenance invalid status -> 400 | POST | `/api/maintenance` | 400 | 400 |
| 9 | create maintenance bad building -> 400 | POST | `/api/maintenance` | 400 | 400 |
| 10 | create maintenance bad vendor -> 400 | POST | `/api/maintenance` | 400 | 400 |
| 11 | create maintenance common-area (no apt/vendor/equip) | POST | `/api/maintenance` | 200 (created) | 200 |
| 12 | update maintenance | PUT | `/api/maintenance/172020d8-1217-4436-9398-f47ad308f5e9` | 200 (updated) | 200 |
| 13 | update maintenance status | PATCH | `/api/maintenance/172020d8-1217-4436-9398-f47ad308f5e9/status` | 200 (updated) | 200 |
| 14 | update maintenance status invalid -> 400 | PATCH | `/api/maintenance/172020d8-1217-4436-9398-f47ad308f5e9/status` | 400 | 400 |
| 15 | assign vendor to maintenance | PATCH | `/api/maintenance/172020d8-1217-4436-9398-f47ad308f5e9/assign` | 200 (updated) | 200 |
| 16 | delete maintenance with password | DELETE | `/api/maintenance/172020d8-1217-4436-9398-f47ad308f5e9?password=********` | 200 (deleted) | 200 |
| 17 | maintenance export xlsx | GET | `/api/maintenance/download-xlsx` | 200 | 200 |

## INCOME

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list income | GET | `/api/income` | 200 | 200 |
| 2 | get income by id | GET | `/api/income/f4b3b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 3 | get income bad id -> 404 | GET | `/api/income/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 4 | list income filter building | GET | `/api/income?buildingId=b1f7b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 5 | create income ApartmentWise valid | POST | `/api/income` | 200 (created) | 200 |
| 6 | create income GeneralOthers valid | POST | `/api/income` | 200 (created) | 200 |
| 7 | create income ApartmentWise missing building/apartment -> 400 | POST | `/api/income` | 400 | 400 |
| 8 | create income on vacant apartment -> 400 | POST | `/api/income` | 400 | 400 |
| 9 | create income duplicate (same apt/type/amount/month) -> 400 | POST | `/api/income` | 400 | 400 |
| 10 | create income amount zero -> 400 | POST | `/api/income` | 400 | 400 |
| 11 | create income invalid status -> 400 | POST | `/api/income` | 400 | 400 |
| 12 | update income | PUT | `/api/income/04fb2afe-79cb-4fe8-a247-a73ac29c4f08` | 200 (updated) | 200 |
| 13 | update income status | PATCH | `/api/income/04fb2afe-79cb-4fe8-a247-a73ac29c4f08/status` | 200 (updated) | 200 |
| 14 | update income status invalid -> 400 | PATCH | `/api/income/04fb2afe-79cb-4fe8-a247-a73ac29c4f08/status` | 400 | 400 |
| 15 | income receipt pdf | GET | `/api/income/download/04fb2afe-79cb-4fe8-a247-a73ac29c4f08` | 200 | 200 |
| 16 | delete income with password | DELETE | `/api/income/04fb2afe-79cb-4fe8-a247-a73ac29c4f08?password=********` | 200 (deleted) | 200 |
| 17 | income export xlsx | GET | `/api/income/download-xlsx` | 200 | 200 |

## EXPENSES

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list expenses | GET | `/api/expenses` | 200 | 200 |
| 2 | get expense by id | GET | `/api/expenses/e4c7b822-29c4-52a8-ad29-c8be5d491f40` | 200 | 200 |
| 3 | get expense bad id -> 404 | GET | `/api/expenses/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 4 | create expense BuildingLevel valid | POST | `/api/expenses` | 200 (created) | 200 |
| 5 | create expense ApartmentSpecific valid | POST | `/api/expenses` | 200 (created) | 200 |
| 6 | create expense ApartmentSpecific missing apartment -> 400 | POST | `/api/expenses` | 400 | 400 |
| 7 | create expense future date -> 400 | POST | `/api/expenses` | 400 | 400 |
| 8 | create expense amount zero -> 400 | POST | `/api/expenses` | 400 | 400 |
| 9 | create expense duplicate -> 400 | POST | `/api/expenses` | 400 | 400 |
| 10 | create water tanker missing fields -> 400 | POST | `/api/expenses` | 400 | 400 |
| 11 | create water tanker valid | POST | `/api/expenses` | 200 (created) | 200 |
| 12 | create water tanker duplicate delivery -> 400 | POST | `/api/expenses` | 400 | 400 |
| 13 | create expense invalid tanker time -> 400 | POST | `/api/expenses` | 400 | 400 |
| 14 | update expense | PUT | `/api/expenses/8cb17ee8-769e-46d9-a545-6bbd4e6f1463` | 200 (updated) | 200 |
| 15 | update expense status | PATCH | `/api/expenses/8cb17ee8-769e-46d9-a545-6bbd4e6f1463/status` | 200 (updated) | 200 |
| 16 | update expense status invalid -> 400 | PATCH | `/api/expenses/8cb17ee8-769e-46d9-a545-6bbd4e6f1463/status` | 400 | 400 |
| 17 | delete expense with password | DELETE | `/api/expenses/8cb17ee8-769e-46d9-a545-6bbd4e6f1463?password=********` | 200 (deleted) | 200 |
| 18 | expenses export xlsx | GET | `/api/expenses/download-xlsx` | 200 | 200 |

## AMC-CONTRACTS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list amc contracts | GET | `/api/amc-contracts` | 200 | 200 |
| 2 | amc contract stats | GET | `/api/amc-contracts/stats` | 200 | 200 |
| 3 | get amc contract by id | GET | `/api/amc-contracts/f2a3b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 4 | get amc contract bad id -> 404 | GET | `/api/amc-contracts/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 5 | create amc contract valid | POST | `/api/amc-contracts` | 200 (created) | 200 |
| 6 | create amc duplicate code -> 400 | POST | `/api/amc-contracts` | 400 | 400 |
| 7 | create amc duplicate contract number -> 400 | POST | `/api/amc-contracts` | 400 | 400 |
| 8 | create amc bad vendor -> 400 | POST | `/api/amc-contracts` | 400 | 400 |
| 9 | update amc contract | PUT | `/api/amc-contracts/563d8bbc-84a2-454e-9649-d23a44fe662e` | 200 (updated) | 200 |
| 10 | update amc to duplicate code -> 400 | PUT | `/api/amc-contracts/563d8bbc-84a2-454e-9649-d23a44fe662e` | 400 | 400 |
| 11 | delete amc contract with password | DELETE | `/api/amc-contracts/563d8bbc-84a2-454e-9649-d23a44fe662e?password=********` | 200 (deleted) | 200 |

## REPORTS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | income report | GET | `/api/reports/income` | 200 | 200 |
| 2 | income report with filters | GET | `/api/reports/income?startDate=2026-08-01&endDate=2026-08-31` | 200 | 200 |
| 3 | expense report | GET | `/api/reports/expenses` | 200 | 200 |
| 4 | report stats | GET | `/api/reports/stats` | 200 | 200 |
| 5 | report export income xlsx | GET | `/api/reports/export?type=income` | 200 | 200 |
| 6 | report export expenses xlsx | GET | `/api/reports/export?type=expenses` | 200 | 200 |
| 7 | report export combined xlsx | GET | `/api/reports/export?type=combined` | 200 | 200 |
| 8 | report export invalid type -> 400 | GET | `/api/reports/export?type=bogus` | 400 | 400 |

## DASHBOARD

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | dashboard stats | GET | `/api/dashboard/stats` | 200 | 200 |
| 2 | dashboard occupancy | GET | `/api/dashboard/occupancy` | 200 | 200 |
| 3 | dashboard expense breakdown | GET | `/api/dashboard/expense-breakdown` | 200 | 200 |
| 4 | dashboard recent payments | GET | `/api/dashboard/recent-payments` | 200 | 200 |
| 5 | dashboard open maintenance | GET | `/api/dashboard/open-maintenance` | 200 | 200 |

## DELETED-HISTORY

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | list deleted history | GET | `/api/deleted-history` | 200 | 200 |
| 2 | get deleted history by id | GET | `/api/deleted-history/4da2b822-29c4-52a8-ad29-c8be5d491f24` | 200 | 200 |
| 3 | get deleted history bad id -> 404 | GET | `/api/deleted-history/00000000-0000-0000-0000-000000000000` | 404 | 404 |
| 4 | create building for restore test | POST | `/api/buildings` | 200 (created) | 200 |
| 5 | soft-delete building (creates history) | DELETE | `/api/buildings/6d460f0a-74be-48d8-917f-daaa982b6819?password=********` | 200 (deleted) | 200 |
| 6 | restore deleted record | POST | `/api/deleted-history/2e8145ac-0332-408d-9385-a567dd9edf66/restore` | 200 (restored) | 200 |
| 7 | building visible again after restore | GET | `/api/buildings/6d460f0a-74be-48d8-917f-daaa982b6819` | 200 | 200 |
| 8 | create building for permanent delete | POST | `/api/buildings` | 200 (created) | 200 |
| 9 | soft-delete building 2 | DELETE | `/api/buildings/a3cf4357-c99b-47db-ac4f-aa3ae64ce182?password=********` | 200 (deleted) | 200 |
| 10 | permanent delete without password -> 400 | DELETE | `/api/deleted-history/db7b15fc-c74a-49f6-8e0a-5f09f0a0ee8c` | 400 | 400 |
| 11 | permanent delete with password | DELETE | `/api/deleted-history/db7b15fc-c74a-49f6-8e0a-5f09f0a0ee8c?password=********` | 200 (deleted) | 200 |
| 12 | history row gone -> 404 | GET | `/api/deleted-history/db7b15fc-c74a-49f6-8e0a-5f09f0a0ee8c` | 404 | 404 |

## BULK-UPLOAD

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | template download apartments | GET | `/api/bulk-upload/template?module=apartments` | 200 | 200 |
| 2 | template download tenants | GET | `/api/bulk-upload/template?module=tenants` | 200 | 200 |
| 3 | template download owners | GET | `/api/bulk-upload/template?module=owners` | 200 | 200 |
| 4 | template download income | GET | `/api/bulk-upload/template?module=income` | 200 | 200 |
| 5 | template download expenses | GET | `/api/bulk-upload/template?module=expenses` | 200 | 200 |
| 6 | template download maintenance | GET | `/api/bulk-upload/template?module=maintenance` | 200 | 200 |
| 7 | template download equipment | GET | `/api/bulk-upload/template?module=equipment` | 200 | 200 |
| 8 | template invalid module -> 400 | GET | `/api/bulk-upload/template?module=bogus` | 400 | 400 |
| 9 | bulk upload list (empty) | GET | `/api/bulk-upload/status` | 200 | 200 |
| 10 | bulk upload xlsx file | POST | `/api/upload/xlsx` | 200 () | 200 |
| 11 | start bulk upload job | POST | `/api/bulk-upload` | 200 (Apartments) | 200 |
| 12 | bulk upload status by id | GET | `/api/bulk-upload/status/7e17e26c-02a0-4346-9b1b-42cb5f9348d5` | 200 | 200 |
| 13 | bulk-created apartment visible | GET | `/api/apartments?search=NEST-BULK-001` | 200 (NEST-BULK-001) | 200 |

## PERMISSIONS

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | unauthenticated -> 401 | GET | `/api/buildings` | 401 | 401 |
| 2 | accountant GET users -> 403 | GET | `/api/users` | 403 | 403 |
| 3 | accountant GET buildings (open-read) -> 200 | GET | `/api/buildings` | 200 | 200 |
| 4 | accountant POST buildings -> 403 | POST | `/api/buildings` | 403 | 403 |
| 5 | accountant POST income (finance) -> 200 | POST | `/api/income` | 200 | 200 |
| 6 | accountant POST maintenance -> 403 | POST | `/api/maintenance` | 403 | 403 |
| 7 | accountant POST expenses (finance) -> 200 | POST | `/api/expenses` | 200 | 200 |
| 8 | manager GET notifications (dashboard) -> 200 | GET | `/api/notifications` | 200 | 200 |
| 9 | manager GET deleted-history -> 403 | GET | `/api/deleted-history` | 403 | 403 |

## EXPORT-CONTENT

| # | Check | Method | Endpoint | Expected | Got |
|---|-------|--------|----------|----------|-----|
| 1 | apartments export has names | GET | `/api/apartments/download-xlsx` | contains Grand Plaza Towers | 200 |
| 2 | apartments export has owner name | GET | `/api/apartments/download-xlsx` | contains Amit Sharma | 200 |
| 3 | apartments export (no GUIDs) | GET | `/api/apartments/download-xlsx` | no GUIDs | 0 |
| 4 | tenants export has names | GET | `/api/tenants/download-xlsx` | contains Arjun Mehta | 200 |
| 5 | tenants export (no GUIDs) | GET | `/api/tenants/download-xlsx` | no GUIDs | 0 |
| 6 | owners export has names | GET | `/api/owners/download-xlsx` | contains Rahul Verma | 200 |
| 7 | equipment export has names | GET | `/api/equipment/download-xlsx` | contains OTIS Elevator Block A | 200 |
| 8 | equipment export (no GUIDs) | GET | `/api/equipment/download-xlsx` | no GUIDs | 0 |
| 9 | maintenance export has title | GET | `/api/maintenance/download-xlsx` | contains Water leakage in Flat 302 | 200 |
| 10 | maintenance export (no GUIDs) | GET | `/api/maintenance/download-xlsx` | no GUIDs | 0 |
| 11 | income export has notes | GET | `/api/income/download-xlsx` | contains Rent for Flat 302 | 200 |
| 12 | income export (no GUIDs) | GET | `/api/income/download-xlsx` | no GUIDs | 0 |
| 13 | expenses export has head | GET | `/api/expenses/download-xlsx` | contains Electricity Bill | 200 |
| 14 | expenses export (no GUIDs) | GET | `/api/expenses/download-xlsx` | no GUIDs | 0 |
| 15 | combined report export | GET | `/api/reports/export?type=combined` | contains Received from | 200 |

## Informational captures

- **bulk job result (status=Finished total=3 success=1 failed=2)** (`POST /api/bulk-upload`): `{"success":true,"message":"Request processed successfully.","data":{"id":"7e17e26c-02a0-4346-9b1b-42cb5f9348d5","module":"Apartments","status":"Finished","originalFileUrl":"http://localhost:5240/image/568c1293-0847-458d-941d-01406e49ad2c.xlsx","processedFileUrl":"http://localhost:5240/image/bulk_pro`
- **processed file row values** (`GET http://localhost:5240/image/bulk_processed_7e17e26c-02a0-4346-9b1b-42cb5f9348d5.xlsx`): `0 | ownerName | nestawayId | flatNumber | apartmentType | status | error | Grand Plaza Towers | Rahul Verma | NEST-BULK-001 | 401 | 2 BHK | Success |  | Nonexistent Tower | Rahul Verma | NEST-BULK-002 | 402 | 2 BHK | Failed | Building 'Nonexistent Tower' was not found. The building must be created b`

