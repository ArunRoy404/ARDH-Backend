# 📘 ARDH Property Management Backend — API & Postman Integration Changelog

This document provides a comprehensive technical guide covering all API endpoints, URL structure changes, authentication & authorization rules, file storage routing, and real-time streaming interfaces implemented across the **ARDH Property Management Backend**.

---

## 📌 Executive Summary of Major Enhancements

1. **Dedicated Postman Artifacts**:
   - Collection file path: `postman/Ardh_Postman_Collection.json`
   - Large test dataset: `postman/large_apartments_test.xlsx` (500 rows for high-volume bulk import testing).

2. **Categorized Multi-Folder File Storage**:
   - Files are stored and served under 3 isolated static route providers: `/image/`, `/document/`, `/bulk-upload/`.

3. **Real-Time Server-Sent Events (SSE) Engine**:
   - Live 0–100% bulk import progress updates delivered over `GET /api/bulk-upload/track/{trackId}` with HTTP middleware stream buffering bypass.

4. **Strict Role-Based Access Control (RBAC)**:
   - Admin-only authorization guards (`[Authorize(Roles = "Admin")]`) applied to User Management, Settings Modification, and Soft-Deleted History.

5. **SQL Server Predicate Push-Down & Page-Scoped Lookups**:
   - 100% of entity list queries filtering and sorting execute directly on SQL Server, with lookup dictionary queries scoped to the 10 active page items.

---

## 📂 1. File Upload Endpoints & Static Routes

The backend categorizes uploaded files into isolated physical folders. Each endpoint returns a static URL pointing to the appropriate web root.

| Endpoint Route | HTTP Method | Form-Data Parameter | Storage Folder | Return URL Format Example | Frontend Use Case |
| :--- | :---: | :---: | :---: | :--- | :--- |
| `/api/upload/image` | `POST` | `file` | `image/` | `http://localhost:5240/image/a1b2c3d4.png` | Profile avatars, building photos, property banners |
| `/api/upload/document` | `POST` | `file` | `document/` | `http://localhost:5240/document/b2c8a7e4.pdf` | AMC contracts, receipts, manuals, PDFs |
| `/api/upload/id-proof` | `POST` | `file` | `document/` | `http://localhost:5240/document/c3d9b8f5.pdf` | Tenant/Owner government ID proof documents |
| `/api/upload/xlsx` | `POST` | `file` | `bulk-upload/` | `http://localhost:5240/bulk-upload/large_apartments.xlsx` | Excel spreadsheets for bulk import |
| `/api/upload/{fileId}` | `DELETE` | Path parameter | All | `{ "success": true, "message": "File deleted successfully." }` | Delete uploaded file attachment |

### Processed Bulk Result Files:
- Stored and served at: `http://localhost:5240/bulk-upload/bulk_processed_{id}.xlsx`

---

## 📡 2. Real-Time Bulk Upload SSE Progress Tracking

Bulk Excel file processing (apartments, tenants, owners, income, expenses, maintenance, equipment) provides live progress events using **Server-Sent Events (SSE)**.

### Endpoint:
```
GET /api/bulk-upload/track/{trackId}
Header: Accept: text/event-stream
```

### Event Payload JSON Schema:
```json
data: {
  "id": "3f9c2a1e-0000-4000-8000-000000000001",
  "module": "apartments",
  "status": "Processing",
  "progress": 72,
  "processedRows": 360,
  "totalRows": 500,
  "successCount": 355,
  "failedCount": 5,
  "resultFileUrl": "http://localhost:5240/bulk-upload/bulk_processed_3f9c2a1e-0000-4000-8000-000000000001.xlsx"
}
```

### Frontend Code Example (React / Vanilla JS):
```javascript
const trackId = "3f9c2a1e-0000-4000-8000-000000000001";
const eventSource = new EventSource(`http://localhost:5240/api/bulk-upload/track/${trackId}`);

eventSource.onmessage = (event) => {
  const data = JSON.parse(event.data);
  
  // Render real-time progress bar (0% -> 100%)
  setProgressBar(data.progress);
  setSuccessCount(data.successCount);
  setFailCount(data.failedCount);

  if (data.status === "Completed" || data.status === "Failed") {
    setDownloadUrl(data.resultFileUrl);
    eventSource.close(); // Disconnect when complete
  }
};

eventSource.onerror = (err) => {
  console.error("SSE connection error:", err);
  eventSource.close();
};
```

---

## 🔒 3. Role-Based Access Control (RBAC) Security Matrix

Endpoints requiring `Admin` role privileges return `403 Forbidden` if accessed by non-admin roles (`property_manager`, `accountant`, `viewer`).

| Controller Path | Allowed Roles | Required Authorization Header | Frontend UI Guidance |
| :--- | :---: | :---: | :--- |
| `GET/POST/PUT/DELETE /api/users/*` | `Admin` | `Bearer {admin_token}` | Hide User Management navigation link for non-admins |
| `PUT /api/settings` | `Admin` | `Bearer {admin_token}` | Disable System Settings edit forms for non-admins |
| `PUT /api/settings/password` | `Admin` | `Bearer {admin_token}` | Restrict password updates to Admin user |
| `GET/POST/DELETE /api/deleted-history/*` | `Admin` | `Bearer {admin_token}` | Hide Soft-Deleted History page for non-admins |

---

## 📊 4. Paginated List Query Parameters & Filters

All listing endpoints execute filtering directly inside SQL Server and paginate lookup maps for high performance.

### Standard Request Format:
```
GET /api/apartments?page=1&pageSize=10&search=302&buildingId={guid}&status=occupied
GET /api/tenants?page=1&pageSize=10&search=Arjun&buildingId={guid}&status=Active
GET /api/income?page=1&pageSize=10&incomeType=Rent&status=Paid&startDate=2026-08-01&endDate=2026-08-31
GET /api/expenses?page=1&pageSize=10&category=Utility&status=Paid&nature=Service
GET /api/buildings?page=1&pageSize=10&search=Grand&status=active
```

---

## 🧪 5. Pre-Seeded Default Accounts

| Role | Email | Password | Admin Setting Password |
| :--- | :--- | :--- | :--- |
| **Super Admin** | `admin@gmail.com` | `P@ssw0rd` | `adminpassword` |
| **Property Manager** | `manager@gmail.com` | `P@ssw0rd` | N/A |
| **Accountant** | `accountant@gmail.com` | `P@ssw0rd` | N/A |

---

## 🗂️ 6. Postman Collection Structure

```
Ardh Backend API Collection
├── 📁 Auth & Account Management
├── 📁 User Management (Admin Only)
├── 📁 Dashboard Stats
├── 📁 Buildings Management
├── 📁 Owners Management
├── 📁 Apartments Management
├── 📁 Tenants Management
├── 📁 Vendors Management
├── 📁 Equipment Management
├── 📁 AMC Contracts Management
├── 📁 Maintenance Management
├── 📁 Income Management
├── 📁 Expenses Management
├── 📁 Notifications
├── 📁 Activities
├── 📁 File Upload
│   ├── ➡ F-01. Upload Image File (POST /api/upload/image)
│   ├── ➡ F-02. Upload Document File (POST /api/upload/document)
│   ├── ➡ F-03. Upload ID Proof Document (POST /api/upload/id-proof)
│   └── ➡ F-04. Delete Uploaded File (DELETE /api/upload/{fileId})
├── 📁 Bulk Upload
│   ├── ➡ BU-00. Upload XLSX File (POST /api/upload/xlsx)
│   ├── 📁 Module Start & Status Requests
│   └── ➡ BU-05. Track Bulk Upload Progress (SSE) (GET /api/bulk-upload/track/{trackId})
├── 📁 Setting Management (Admin Only)
├── 📁 Deleted History (Admin Only)
└── 📁 Health Checks
```
