# 🗄️ ARDH Database Guide (Seeding · Migration · Delete All Data · Add User)

This guide covers four independent database operations for the **ARDH Property Management** backend
(`src/CleanArchitecture`), which runs on **.NET 8.0 / EF Core 8 / SQL Server**.

> **Connection string** (used by all examples below):
> `Server=localhost;Database=ARDHDB;User ID=SA;Password=ARDHDatabase404;TrustServerCertificate=True;MultipleActiveResultSets=true`
>
> **Project path**: `src/CleanArchitecture/CleanArchitecture.csproj`
>
> **Important — after the seed-ID fix (2026-08-07):** the app must be **rebuilt** before running any
> seeding / reset command, otherwise you get the old crashing binary:
> `dotnet build src/CleanArchitecture/CleanArchitecture.csproj`

---

## Part 1 — Seeding

Seeding is **automatic**. The `ApplicationDbContextInitializer` runs at every app startup and:

1. Applies any pending EF migrations (`Database.MigrateAsync()`).
2. Seeds a **full demo dataset** (users, buildings, owners, apartments, tenants, vendors,
   equipment, AMC contracts, maintenance requests, income, expenses, settings, notifications,
   activities, deleted-history) — **but only if the relevant tables are empty**.

All seed record IDs are **fixed** and match the examples in `Ardh_Postman_Collection.json`.

### 1.1 Seed a brand-new database

Just start the app — migrations + seeding happen automatically:

```bash
dotnet run --project src/CleanArchitecture/CleanArchitecture.csproj
```

or (Windows):

```bat
run.bat
```

or (Linux):

```bash
./run
```

On the first start against an empty `ARDHDB`, you will see log lines like:

```
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20260712104442_InitialArdhSchema'.
      ...
info: CleanArchitecture.Infrastructure.Data.ApplicationDbContextInitializer[0]
```

### 1.2 Seeded login accounts

| Name | Email | Role | Password |
| :--- | :--- | :--- | :--- |
| Super Admin | `admin@gmail.com` | `admin` | `P@ssw0rd` |
| Property Manager | `manager@gmail.com` | `property_manager` | `P@ssw0rd` |
| Accountant | `accountant@gmail.com` | `accountant` | `P@ssw0rd` |

### 1.3 Seeding is idempotent

Seeding only inserts rows when a table is empty (`if (await _context.Users.AnyAsync()) return;`).
If you restart the app, **existing data is left untouched** — nothing is duplicated.

### 1.4 Force a full re-seed

See **Part 3** (`SEED_MODE=reset`), which wipes everything and re-runs the seeder.

---

## Part 2 — Migrations

EF Core migrations live in `src/CleanArchitecture/Infrastructure/Migrations/` and are
**applied automatically on startup**. You normally only need `dotnet ef` when you change an entity.

### 2.1 Prerequisites (once)

The `dotnet-ef` tool (v8.0.0) is already declared in `.config/dotnet-tools.json`:

```bash
cd /home/roy/practice/aaaa-test
dotnet tool restore
```

> If you ever need it globally instead:
> `dotnet tool install --global dotnet-ef --version 8.0.0`

### 2.2 Apply pending migrations manually

```bash
dotnet ef database update \
  --project src/CleanArchitecture/CleanArchitecture.csproj
```

### 2.3 Create a new migration after changing an entity

```bash
dotnet ef migrations add <MigrationName> \
  --project src/CleanArchitecture/CleanArchitecture.csproj
```

Example:

```bash
dotnet ef migrations add AddTenantLeaseDeposit \
  --project src/CleanArchitecture/CleanArchitecture.csproj
```

This generates `<timestamp>_AddTenantLeaseDeposit.cs` (+ `.Designer.cs`) and updates
`ApplicationDbContextModelSnapshot.cs`. The next time the app starts (or you run
`dotnet ef database update`), it is applied.

### 2.4 Other useful migration commands

```bash
# Revert the last migration (scaffold only, doesn't touch DB)
dotnet ef migrations remove --project src/CleanArchitecture/CleanArchitecture.csproj

# Generate an idempotent SQL script for all migrations
dotnet ef migrations script --project src/CleanArchitecture/CleanArchitecture.csproj

# See migration status
dotnet ef migrations list --project src/CleanArchitecture/CleanArchitecture.csproj
```

> **Note**: Because `MigrateAsync()` runs on every startup, a DB that is out of sync is
> automatically migrated the moment the API starts. Manual `database update` is optional.

---

## Part 3 — Delete ALL Database Data (Full Reset)

There are four ways to wipe everything:

### 3.0 Wipe everything but keep one admin user — `SEED_MODE=wipe`

The initializer also supports a wipe-only mode that deletes every row from every table
(same FK-safe order as `SEED_MODE=reset`) and then seeds **only** the Super Admin user —
no demo buildings, owners, tenants, etc.

```bat
:: Windows (PowerShell)
$env:SEED_MODE="wipe"
dotnet run --project src/CleanArchitecture\CleanArchitecture.csproj
```

```bash
# Linux / macOS
SEED_MODE=wipe dotnet run --project src/CleanArchitecture/CleanArchitecture.csproj
```

After this the only row in the whole database is:

| Name | Email | Role | Password |
| :--- | :--- | :--- | :--- |
| Super Admin | `admin@gmail.com` | `admin` | `P@ssw0rd` |

Verify in the log: `Database wiped: only the admin user remains.`

Unset (or unset in your shell) `SEED_MODE` afterwards, otherwise the **next** restart wipes again.

### 3.1 Recommended (full demo reset) — `SEED_MODE=reset` (wipes + re-seeds in one start)

The initializer supports an environment-variable driven reset:

```bash
# Linux / macOS
SEED_MODE=reset dotnet run --project src/CleanArchitecture/CleanArchitecture.csproj
```

```bat
:: Windows (PowerShell)
$env:SEED_MODE="reset"
dotnet run --project src/CleanArchitecture\CleanArchitecture.csproj
```

```bat
:: Windows (cmd)
set SEED_MODE=reset
dotnet run --project src/CleanArchitecture\CleanArchitecture.csproj
```

What happens:
1. The app connects to `ARDHDB`.
2. `ResetDatabaseAsync()` **deletes every row** from all tables in FK-safe order
   (notifications/recipients → activities → deleted-history → move-outs → income → expenses →
   maintenance → AMC → equipment → tenants → apartments → owners → vendors → buildings →
   forgot-password → users → settings). Since the fix, this also removes soft-deleted rows
   (`IgnoreQueryFilters()`).
3. The full demo dataset is re-seeded with the fixed canonical IDs.
4. The API starts normally.

Verify in the log: `Database reset: all existing data removed.`

> ⚠️ **Before the fix (2026-08-07)** `SEED_MODE=reset` crashed with
> `Guid string should only contain hexadecimal characters` because several seed IDs contained
> non-hex letters (`o1f3b822…`, `t1f3b822…`, `v1a3b822…`, `m1f3b822…`). This is fixed — all
> 43 `Guid.Parse` constants are now valid and match the Postman collection.

### 3.2 Drop & recreate the database (destructive)

```bash
# Needs sqlcmd / SSMS. Adjust credentials as needed.
/opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P 'ARDHDatabase404' -C -Q "
IF DB_ID('ARDHDB') IS NOT NULL
BEGIN
    ALTER DATABASE ARDHDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ARDHDB;
END
"
```

Then start the app once — it recreates the DB from migrations and seeds it (see Part 1).

### 3.3 Delete table data manually (without re-seed)

```bash
/opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P 'ARDHDatabase404' -C -d ARDHDB -Q "
DELETE FROM notification_recipients;
DELETE FROM notifications;
DELETE FROM activities;
DELETE FROM deleted_histories;
DELETE FROM tenant_move_out_records;
DELETE FROM income_records;
DELETE FROM expense_records;
DELETE FROM maintenance_requests;
DELETE FROM amc_contracts;
DELETE FROM equipment;
DELETE FROM tenants;
DELETE FROM apartments;
DELETE FROM owners;
DELETE FROM vendors;
DELETE FROM buildings;
DELETE FROM forgot_password;
DELETE FROM users;
DELETE FROM settings;
"
```

> ⚠️ Manual SQL deletes bypass soft-delete filters and **do not re-seed**. Prefer Part 3.1.

---

## Part 4 — Add a User via Terminal

### 4.1 Option A — via the API (recommended, uses the same logic as the app)

```bash
# 1) Sign in as admin, save the session cookie
curl -c /tmp/ardh_cookies.txt -X POST http://localhost:5240/api/auth/sign-in \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@gmail.com","password":"P@ssw0rd","rememberMe":true}'

# 2) Create the user (token is sent automatically via the cookie jar)
curl -b /tmp/ardh_cookies.txt -X POST http://localhost:5240/api/users \
  -H 'Content-Type: application/json' \
  -d '{
        "name": "Front Desk Executive",
        "email": "frontdesk@gmail.com",
        "phone": "+91 9000011111",
        "password": "P@ssw0rd",
        "confirmPassword": "P@ssw0rd",
        "address": "Lobby, Grand Plaza Towers",
        "role": "viewer",
        "permissions": "dashboard",
        "avatarURL": "https://images.unsplash.com/photo-1535713875002-d1d0cf377fde"
      }'
```

Roles: `admin | property_manager | accountant | viewer`
Permissions (comma separated): `dashboard, properties, finance, operations, admin`

### 4.2 Option B — direct SQL insert (BCrypt hash required)

The app stores passwords with **BCrypt** (`BCrypt.Net-Next`). You cannot insert plain text —
you must insert a valid BCrypt hash. Generate one with the exact same package the app uses:

```bash
# Create a throwaway helper project (once)
mkdir -p /tmp/hashgen && cd /tmp/hashgen
cat > hashgen.csproj <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
</Project>
EOF
cat > Program.cs <<'EOF'
Console.WriteLine(BCrypt.Net.BCrypt.HashPassword("MyNewPass123!"));
EOF
dotnet run
```

Example hash for `MyNewPass123!` (yours will differ — BCrypt salts per run):

```
$2a$11$FlBWtAKLk6BAW2bQkB/jqeG/Ba93z9t3X.iafYTq/zSGeWGb/sAYG
```

Then insert (note: `users` table, `password_hash` column stores the BCrypt hash, `is_deleted = 0`):

```bash
/opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P 'ARDHDatabase404' -C -d ARDHDB -Q "
INSERT INTO users
    (id, name, email, phone, password_hash, role, address, permissions, avatar_url,
     is_active, last_login_at, refresh_token, created_at, updated_at, created_by, updated_by, is_deleted)
VALUES
    (NEWID(), 'Terminal User', 'terminal.user@example.com', '+91 9988776655',
     '\$2a\$11\$FlBWtAKLk6BAW2bQkB/jqeG/Ba93z9t3X.iafYTq/zSGeWGb/sAYG',
     'viewer', '1 Test Street', 'dashboard', NULL,
     1, NULL, NULL, GETUTCDATE(), GETUTCDATE(), NULL, NULL, 0);
"
```

> **Escaping tip**: in `bash` double quotes, escape `$` as `\$`; in PowerShell use single quotes
> for the `-Q` argument.

### 4.3 Verify the new user

```bash
# API
curl -b /tmp/ardh_cookies.txt "http://localhost:5240/api/users?search=Terminal"
```

or sign in directly with the new credentials:

```bash
curl -X POST http://localhost:5240/api/auth/sign-in \
  -H 'Content-Type: application/json' \
  -d '{"email":"terminal.user@example.com","password":"MyNewPass123!"}'
```

---

## Appendix — Quick reference

| Operation | Command |
| :--- | :--- |
| Start + auto-migrate + auto-seed | `dotnet run --project src/CleanArchitecture/CleanArchitecture.csproj` |
| Wipe + re-seed (full demo data) | `SEED_MODE=reset dotnet run --project src/CleanArchitecture/CleanArchitecture.csproj` |
| Wipe + keep only admin user | `SEED_MODE=wipe dotnet run --project src/CleanArchitecture/CleanArchitecture.csproj` |
| Apply migrations | `dotnet ef database update --project src/CleanArchitecture/CleanArchitecture.csproj` |
| New migration | `dotnet ef migrations add <Name> --project src/CleanArchitecture/CleanArchitecture.csproj` |
| Add user (API) | sign-in → `POST /api/users` |
| Add user (SQL) | `INSERT INTO users …` with a BCrypt `password_hash` |
| Default admin | `admin@gmail.com` / `P@ssw0rd` |
| Admin delete-password (X-Admin-Password / `?password=`) | `adminpassword` |
