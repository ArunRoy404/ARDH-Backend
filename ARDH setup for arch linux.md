# 🚀 Arch Linux (Hyprland/Hyde) Setup Guide for ARDH Backend

This guide outlines how to install the required packages and run the **ARDH Property Management Backend** (ASP.NET Core 8.0 with MS SQL Server) on your Arch Linux system.

---

## 📦 1. Required System Packages

Open your terminal and install the following using `pacman`:

```bash
# Update repositories and install .NET 8 SDK + Docker
sudo pacman -Syu dotnet-sdk-8.0 docker docker-compose
```

> [!NOTE]
> `dotnet-sdk-8.0` automatically includes the ASP.NET Core runtime.

---

## 🐳 2. Enable and Start Docker

Since Microsoft SQL Server does not run natively on Arch Linux without complex workarounds, running it via **Docker** is the standard approach.

```bash
# Enable and start the Docker daemon
sudo systemctl enable --now docker

# Optional: Add your user to the docker group so you don't need 'sudo' for docker commands
sudo usermod -aG docker $USER
```
> [!IMPORTANT]
> If you add yourself to the `docker` group, you must log out of your session and log back in (or run `newgrp docker`) for the changes to take effect.

---

## 🏃 3. Running the Project

You have two main ways to run the application:

### Option A: Run everything via Docker Compose (Recommended)
This is the easiest method and requires zero configuration changes.

1. Start both the API and the SQL Server Database:
   ```bash
   docker compose up --build
   ```
2. The API will be available at [http://localhost:3001](http://localhost:3001).
3. The swagger UI will be available at [http://localhost:3001/swagger](http://localhost:3001/swagger).

---

### Option B: Run the Database in Docker & API Natively

If you want to run and debug the API locally from your editor/IDE:

1. **Start the SQL Server database in the background**:
   ```bash
   docker compose up -d sqlserver
   ```
2. **Update your Local Connection String**:
   Since Windows Authentication (`Trusted_Connection=True`) does not work on Linux, update your local connection string in `src/CleanArchitecture/appsettings.Development.json` to use SQL Server Authentication:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=127.0.0.1;Database=ArdhDb;User ID=SA;Password=MyPass@word;TrustServerCertificate=True;MultipleActiveResultSets=true;"
   }
   ```
3. **Run the API**:
   ```bash
   dotnet run --project src/CleanArchitecture
   ```
   > [!TIP]
   > The application automatically applies database migrations and seeds default admin accounts on startup. You don't need to manually run `dotnet ef database update`.

---

## 🛠️ 4. Recommended Tools & Editor Setup

Since you are running Hyprland/Hyde, you might prefer keyboard-driven environments. Here are the best toolchains:

### IDE / Editors
* **Neovim**: Install LSP support for C# using `Mason` (search for `csharp-language-server` or `omnisharp`) and `netcoredbg` for debugging.
* **JetBrains Rider**: (Highly Recommended for .NET on Linux). Available in the AUR:
  ```bash
  yay -S rider
  ```
* **VS Code / VSCodium**:
  ```bash
  yay -S visual-studio-code-bin
  ```
  Install the official **C#** and **C# Dev Kit** extensions.

### Database Client
* **DBeaver** or **Azure Data Studio** to view/manage your MS SQL database:
  ```bash
  sudo pacman -S dbeaver
  # or AUR for Azure Data Studio:
  yay -S azuredatastudio-bin
  ```
