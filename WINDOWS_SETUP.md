# 🪟 ARDH Property Management Backend — Windows Setup Guide

This guide provides step-by-step instructions to set up, configure, and run the **ARDH Property Management Backend API** natively on **Windows** without using Docker.

---

## 📋 Prerequisites

Before running the application, make sure you have installed:

1. **.NET 8.0 SDK**
   * Download: [https://dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
   * Verify installation in Command Prompt:
     ```cmd
     dotnet --version
     ```
     *(Should output `8.0.xxx`)*

2. **Microsoft SQL Server (Free Edition: Express or Developer)**
   * **SQL Server Express** is **100% FREE forever** and fully compatible with this project.
   * Download: [https://www.microsoft.com/en-us/sql-server/sql-server-downloads](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) *(Scroll down to **Specialized free edition** and click **Download now** under **Express** or **Developer**)*.
   * Choose **Basic Installation**. Default instance name: `localhost\SQLEXPRESS` or `.\SQLEXPRESS`.
   
   > 💡 **Avoiding "Trial / Evaluation Expiration":**  
   > When installing, make sure to select **Express** or **Developer** edition under *"Specify a free edition"*. Do **not** select *Evaluation* (which is a temporary 180-day trial). SQL Express / Developer will **never expire**.

3. **(Optional) SQL Server Management Studio (SSMS)**
   * Useful for visually inspecting tables, database schema, and records.
   * Download: [https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms](https://learn.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms)

---

## ⚙️ Step 1: Environment Configuration (`.env`)

1. Open **Command Prompt** or **PowerShell** in the root directory:
   ```cmd
   cd c:\Users\ROY\Desktop\practice\ARDH-Backend
   ```

2. Copy [.env.example](file:///c:/Users/ROY/Desktop/practice/ARDH-Backend/.env.example) to `.env`:
   * **Command Prompt (cmd):**
     ```cmd
     copy .env.example .env
     ```
   * **PowerShell:**
     ```powershell
     Copy-Item .env.example .env
     ```

3. Open `.env` in Notepad or VS Code and set your configuration options:

   * **Connection String (Windows Authentication - Default SQL Express):**
     ```env
     ConnectionStrings__DefaultConnection=Server=localhost\SQLEXPRESS;Database=ArdhDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;
     ```

   * **Connection String (SQL Server Authentication with `sa` User):**
     ```env
     ConnectionStrings__DefaultConnection=Server=localhost\SQLEXPRESS;Database=ArdhDb;User ID=sa;Password=YourSQLPassword123!;TrustServerCertificate=True;MultipleActiveResultSets=true;
     ```

   * **JWT Secret Key (`Identity__Key`):**
     ```env
     Identity__Key=SuperSecretKeyForJwtSigning123456789!
     ```

   * **Admin Panel Password (`AdminSettings__Password`):**
     ```env
     AdminSettings__Password=adminpassword
     ```

---

## 🚀 Step 2: Run the Backend Application

You can start the server in any of the following ways:

### Method A: Command Prompt / PowerShell
Run the following command from the repository root directory:
```cmd
dotnet run --project src/CleanArchitecture/CleanArchitecture.csproj
```

### Method B: Batch File Shortcut (`run.bat`)
Double-click [`run.bat`](file:///c:/Users/ROY/Desktop/practice/ARDH-Backend/run.bat) in File Explorer or run in terminal:
```cmd
.\run.bat
```

> **Automatic Migrations & Data Seeding:**
> Upon startup, the app automatically:
> 1. Connects to SQL Server.
> 2. Creates the `ArdhDb` database if it doesn't exist.
> 3. Applies EF Core migrations (`InitialArdhSchema`).
> 4. Seeds canonical demo accounts, settings, and property records.

---

## 🌐 Step 3: Access Swagger UI & Test Endpoints

Once the application logs show it is running and listening:

* **Swagger Interactive Documentation:** [http://localhost:5240/swagger](http://localhost:5240/swagger)
* **Base API URL:** `http://localhost:5240`

### Sign In Endpoint Verification
* **Route:** `POST http://localhost:5240/api/auth/sign-in`
* **Request Body:**
  ```json
  {
    "email": "admin@gmail.com",
    "password": "P@ssw0rd",
    "rememberMe": true
  }
  ```

---

## 🔑 Default Seed Credentials

| Full Name | Email Address | Role | Password |
| :--- | :--- | :--- | :--- |
| **Super Admin** | `admin@gmail.com` | `admin` | `P@ssw0rd` |
| **Property Manager** | `manager@gmail.com` | `property_manager` | `P@ssw0rd` |
| **Accountant** | `accountant@gmail.com` | `accountant` | `P@ssw0rd` |

---

## 🛠️ Database Management & Maintenance Commands

### 1. Re-seed Fresh Demo Data (`SEED_MODE=reset`)
Wipes existing database tables and re-populates canonical demo data:
* **Command Prompt (cmd):**
  ```cmd
  set SEED_MODE=reset
  dotnet run --project src/CleanArchitecture/CleanArchitecture.csproj
  ```
* **PowerShell:**
  ```powershell
  $env:SEED_MODE="reset"
  dotnet run --project src/CleanArchitecture\CleanArchitecture.csproj
  ```

### 2. Wipe Demo Data Except Admin (`SEED_MODE=wipe`)
Deletes demo rows while preserving only the Super Admin account and system settings:
* **Command Prompt (cmd):**
  ```cmd
  set SEED_MODE=wipe
  dotnet run --project src/CleanArchitecture/CleanArchitecture.csproj
  ```
* **PowerShell:**
  ```powershell
  $env:SEED_MODE="wipe"
  dotnet run --project src/CleanArchitecture\CleanArchitecture.csproj
  ```

---

## ❓ Troubleshooting Common Issues

### 1. `Cannot connect to SQL Server` / `A network-related or instance-specific error occurred`
* **Cause:** SQL Server service is stopped or instance name is wrong.
* **Fix:**
  1. Press `Win + R`, type `services.msc`, press **Enter**.
  2. Locate `SQL Server (SQLEXPRESS)` in the list. Ensure status is **Running**.
  3. If your SQL Server was installed as default instance (without `SQLEXPRESS`), update `.env` connection string to `Server=localhost;` or `Server=127.0.0.1;`.

### 2. `'dotnet' is not recognized as an internal or external command`
* **Cause:** .NET SDK is not in System PATH or terminal was opened prior to installation.
* **Fix:** Close all open Command Prompt / PowerShell windows and reopen them after installing .NET 8.0 SDK.

### 3. `SqlException: Cannot open database "ArdhDb" requested by the login`
* **Cause:** Permission issue or corrupted database state.
* **Fix:** Ensure `TrustServerCertificate=True` is included in `ConnectionStrings__DefaultConnection` inside `.env`.
