# ARDH Property Management Backend API

This repository hosts the streamlined ARDH Property Management backend, refactored according to the custom schema defined in `ardh_database_schema.dbml`.

All redundant boilerplate (e.g., books, legacy ASP.NET Identity tables) has been removed. The authentication and user management systems have been fully customized to strictly match the property management domain structure.

---

## 🏗️ Architecture & Tech Stack

- **Core Framework**: ASP.NET Core 8.0 Web API
- **Design Pattern**: Clean Architecture (Domain, Application, Web API, Infrastructure)
- **Database Access**: Entity Framework Core with Repository Pattern and Unit of Work
- **Security**: BCrypt hashing for password security & custom JSON Web Token (JWT) credentials
- **API Documentation**: Swagger/OpenAPI

---

## 🚀 How to Run Locally

### 1. Database Configuration
By default, the application runs using an **In-Memory Database** for seamless development. 
Settings are located in `src/CleanArchitecture/appsettings.Development.json`:

```json
{
  "UseInMemoryDatabase": true,
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ArdhDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

To switch to a relational database like SQL Server, set `"UseInMemoryDatabase": false` and update the connection string.

### 2. Startup Command
Run the following command from the root folder:

```powershell
dotnet run --project src/CleanArchitecture/CleanArchitecture.csproj
```

The server will initialize, automatically apply EF migrations (if running on a relational DB), and seed development accounts.

---

## 🔑 Default Seed Credentials
The database initializer automatically seeds the following credentials for testing:

| Full Name | Email Address | Role | Password |
| :--- | :--- | :--- | :--- |
| **Super Admin** | `admin@gmail.com` | `Admin` | `P@ssw0rd` |
| **Property Manager** | `manager@gmail.com` | `PropertyManager` | `P@ssw0rd` |

---

## 📑 API Endpoint Documentation

### 🔓 Authentication Endpoints (`/api/auth`)

#### 1. Sign In
- **Route**: `POST /api/auth/sign-in`
- **Request Body**:
```json
{
  "email": "admin@gmail.com",
  "password": "P@ssw0rd",
  "rememberMe": true
}
```
- **Response (200 OK)**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Super Admin",
  "email": "admin@gmail.com",
  "role": "SuperAdmin",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

#### 2. Forgot Password (Request OTP)
- **Route**: `POST /api/auth/forgot-password`
- **Request Body**:
```json
{
  "email": "admin@gmail.com"
}
```
- **Response (200 OK)**:
*(Note: Generates a 6-digit OTP code, saves it to the DB with a 10-minute expiry, and prints/logs it in the backend terminal console for easy local testing).*
```json
{
  "message": "Password reset OTP sent to email."
}
```

#### 3. Verify OTP
- **Route**: `POST /api/auth/verify-otp`
- **Request Body**:
```json
{
  "email": "admin@gmail.com",
  "otp": "123456"
}
```
- **Response (200 OK)**:
```json
{
  "message": "OTP verified successfully. You can now reset your password."
}
```

#### 4. Reset Password
- **Route**: `POST /api/auth/reset-password`
- **Request Body**:
```json
{
  "email": "admin@gmail.com",
  "otp": "123456",
  "newPassword": "newPassword123",
  "confirmNewPassword": "newPassword123"
}
```
- **Response (200 OK)**:
```json
{
  "message": "Password has been reset successfully."
}
```

#### 5. Refresh Token
- **Route**: `GET /api/auth/refresh`
- **Response (200 OK)**:
```json
"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

#### 6. Get Current User Profile
- **Route**: `GET /api/auth/profile`
- **Headers**: `Authorization: Bearer <token>`
- **Response (200 OK)**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Super Admin",
  "email": "admin@gmail.com",
  "phone": "1234567890",
  "role": "SuperAdmin",
  "address": null,
  "avatarUrl": null,
  "isActive": true,
  "permissions": null,
  "lastLoginAt": "2026-07-12T10:45:00Z"
}
```

#### 7. Logout
- **Route**: `DELETE /api/auth/logout`
- **Response (200 OK)**:
```json
{
  "message": "Successfully logged out."
}
```

---

### 🛡️ User Management Endpoints (`/api/users`)
*All requests require a valid JWT token (either as HTTP-only cookie `token_key` or in the `Authorization: Bearer <token>` header). Must be authenticated as `SuperAdmin` or `Admin`.*

#### 1. [U-01] List Users (with Pagination, Search, and Filtering)
- **Route**: `GET /api/users`
- **Query Parameters**:
  - `page` (int, default: `1`)
  - `pageSize` (int, default: `10`)
  - `search` (string, optional, searches in `name`, `email`, `phone`)
  - `role` (UserRole enum: `SuperAdmin` | `Admin`, optional)
  - `is_active` (boolean, optional)
- **Response (200 OK)**:
```json
{
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "name": "Super Admin",
      "email": "admin@gmail.com",
      "phone": "1234567890",
      "address": "Bangalore",
      "role": "SuperAdmin",
      "avatarURL": "https://example.com/avatar.png",
      "city": "Bangalore",
      "isActive": true,
      "permissions": "all",
      "lastLoginAt": "2026-07-12T10:45:00Z",
      "createdAt": "2026-07-12T10:30:00Z",
      "updatedAt": "2026-07-12T10:45:00Z"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10
}
```

#### 2. [U-02] Get Single User
- **Route**: `GET /api/users/{id}`
- **Response (200 OK)**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Super Admin",
  "email": "admin@gmail.com",
  "phone": "1234567890",
  "address": "Bangalore",
  "role": "SuperAdmin",
  "avatarURL": "https://example.com/avatar.png",
  "city": "Bangalore",
  "isActive": true,
  "permissions": "all",
  "lastLoginAt": "2026-07-12T10:45:00Z",
  "createdAt": "2026-07-12T10:30:00Z",
  "updatedAt": "2026-07-12T10:45:00Z"
}
```

#### 3. [U-03] Create User
- **Route**: `POST /api/users`
- **Request Body**:
```json
{
  "name": "Admin User",
  "email": "admin2@gmail.com",
  "phone": "555-0199",
  "password": "password123",
  "confirmPassword": "password123",
  "address": "Mumbai",
  "role": "Admin",
  "permissions": "read:billing,write:billing",
  "avatarURL": "https://example.com/avatar.png"
}
```
- **Response (200 OK)**:
```json
{
  "message": "User created successfully."
}
```

#### 4. [U-04] Update User
- **Route**: `PUT /api/users/{id}`
- **Request Body**:
```json
{
  "name": "Admin User Updated",
  "email": "admin2@gmail.com",
  "phone": "555-0199",
  "address": "Pune",
  "role": "Admin",
  "isActive": true,
  "permissions": "read:billing",
  "avatarURL": "https://example.com/avatar.png"
}
```
- **Response (200 OK)**:
```json
{
  "message": "User updated successfully."
}
```

#### 5. [U-05] Soft Delete User
- **Route**: `DELETE /api/users/{id}`
- **Response (200 OK)**:
```json
{
  "message": "User deleted successfully."
}
```
*(Note: Performs a soft-delete by setting the `DeletedAt` timestamp and setting `IsActive` to `false` without removing the record from the database).*

#### 6. [U-06] Toggle User Status
- **Route**: `PATCH /api/users/{id}/toggle-status`
- **Response (200 OK)**:
```json
{
  "message": "User status toggled successfully."
}
```
*(Note: Toggles the `IsActive` flag between `true` and `false` for the specified user).*
