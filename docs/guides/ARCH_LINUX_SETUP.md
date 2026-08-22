# 🚀 Arch Linux (Hyprland/Hyde) Setup Guide (Native Local Run)

This guide outlines how to run the **ARDH Property Management Backend** natively on Arch Linux without Docker.

---

## 📦 1. Required System Packages

Install the .NET 8 SDK on your system:

```bash
sudo pacman -Syu dotnet-sdk-8.0
```

---

## 🏃 2. Running Natively (Without Docker)

You have two options for the database when running natively:

### Option A: Use the Built-In In-Memory Database (Easiest)
This project has built-in support for an in-memory database. Choosing this option requires **zero** database installations or configurations.

1. Open `src/CleanArchitecture/appsettings.Development.json` and change `UseInMemoryDatabase` to `true`:
   ```json
   "UseInMemoryDatabase": true,
   ```
2. Run the application:
   ```bash
   dotnet run --project src/CleanArchitecture
   ```
3. The API will be available at [http://localhost:5240](http://localhost:5240) (or the port defined in `appsettings.Development.json`).

---

### Option B: Use a Local Microsoft SQL Server Instance
If you want to persist data to a real SQL database without Docker, you can install MS SQL Server natively on Arch Linux.

1. **Install MS SQL Server from the AUR**:
   Using an AUR helper like `yay` or `paru`:
   ```bash
   yay -S mssql-server
   ```
2. **Configure and start MS SQL Server**:
   ```bash
   # Run the configuration script to set the SA password and edition (Developer/Express is free)
   sudo /opt/mssql/bin/mssql-conf setup

   # Start the service
   sudo systemctl enable --now mssql-server
   ```
3. **Update Connection String**:
   In `src/CleanArchitecture/appsettings.Development.json`, set your connection string to use SQL Server Authentication with the SA password you configured during setup:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=ArdhDb;User ID=SA;Password=YourConfiguredPassword;TrustServerCertificate=True;MultipleActiveResultSets=true;"
   }
   ```
4. **Run the API**:
   ```bash
   dotnet run --project src/CleanArchitecture
   ```
   > [!TIP]
   > The application automatically applies database migrations and seeds default admin accounts on startup.

---

## 🛠️ 3. Recommended Tools & Editor Setup

### IDE / Editors
* **JetBrains Rider**: (Highly Recommended). Available in the AUR:
  ```bash
  yay -S rider
  ```
* **VS Code / VSCodium**:
  ```bash
  yay -S visual-studio-code-bin
  ```
  Install the official **C#** and **C# Dev Kit** extensions.
* **Neovim**: Install LSP support for C# using `Mason` (search for `csharp-language-server` or `omnisharp`) and `netcoredbg` for debugging.

### Database Client
* **DBeaver** to view/manage your database tables:
  ```bash
  sudo pacman -S dbeaver
  ```
