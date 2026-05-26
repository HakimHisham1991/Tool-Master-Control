# CNC Tooling Database

A web application for managing CNC tooling lists, master tool codes, and machine reference data, with multi-format export (PDF, Excel, CSV, TXT) and a multi-stage approval workflow.

Built and used internally by **UPECA PDC**.

---

## Table of Contents

1. [Overview](#overview)
2. [Key Features](#key-features)
3. [Tech Stack](#tech-stack)
4. [Project Structure](#project-structure)
5. [Getting Started](#getting-started)
   - [Prerequisites](#prerequisites)
   - [Build & Run](#build--run)
   - [First-time Login](#first-time-login)
6. [Modules / Navigation](#modules--navigation)
7. [Data Model](#data-model)
8. [Master Data & Seeding](#master-data--seeding)
9. [Approval Workflow & Stamps](#approval-workflow--stamps)
10. [Export Formats](#export-formats)
11. [Concurrency Model](#concurrency-model)
12. [Configuration](#configuration)
13. [Maintenance & Troubleshooting](#maintenance--troubleshooting)
14. [Deployment Notes](#deployment-notes)

---

## Overview

CNC Tooling Database is the source of truth for CNC tool lists used by the CAM team. A tool list represents the tooling required to machine a specific part / operation / revision. The application:

- Stores **headers** (part number, operation, revision, project code, machine, workcenter, machine model, material spec, etc.) and the **detail rows** (each tool row) of a tool list.
- Provides a master **tool code database** that CAM programmers can register new consumable codes against.
- Manages **reference data** (Part Numbers, Project Codes, Machines, Operations, Revisions, CAM Leaders, CAM Programmers, Material Specifications, Tool Suppliers, Users) through a Settings module.
- Implements a 3-stage **stamp/approval flow** (CAM Programmer -> CAM Leader -> Tool Register) per tool list.
- Locks tool lists to a single editor at a time (heartbeat-based) so two CAM programmers cannot accidentally overwrite each other.
- Exports formal tool list documents to **PDF** (printable A4 landscape with logo, image, signatures), **Excel (.xlsx)**, **CSV**, and **TXT**.

## Key Features

- **Tool List Editor** (`/ToolListEditor`)
  - Header fields with smart, dependent dropdowns (Part Number -> Project Code, Material Spec -> General Name, Machine Name -> Workcenter / Machine Model).
  - Dynamic detail rows with auto-fill from the master tool code (`Master Tool Code Database`).
  - Live edit lock with heartbeat / "Locked by ..." indicator.
  - 3-stage stamp + approval, with each approver's display name shown next to their stamp.
  - Multi-format export from a single record.
- **Master Tool Code Database** (`/ToolCodeUnique`, `/ToolCodeUniqueEditor`)
  - User-maintained reference of consumable codes (running `No.` Id, never hard-deleted).
  - Used to auto-suggest tool details when a CAM programmer types a `Consumable Code` in a tool list row.
- **Tool Code Database with Tool List** (`/ToolCode`)
  - Aggregated view that joins tool master data with the tool lists that use each tool, with full filtering / sorting on every column.
- **Tool List Database** (`/ToolList`)
  - Browse / search / filter all tool lists, see who has them locked, status, and last modified date.
- **Settings** (`/Settings`)
  - 12 master-data screens, each with create / edit / delete / reset-from-Excel.
  - Per-user **stamp** image upload.
  - "Reset All Settings" (re-seeds every master from the bundled `MASTER - *.xlsx` files).
- **Authentication & Sessions**
  - Cookie-based session, 8-hour idle timeout, custom middleware redirects unauthenticated requests to `/login`.
  - Public escape hatches: `/Account/ReloadUsers` and `/Account/LoginDebug` (no auth, useful when seed users have empty passwords).

## Tech Stack

| Layer            | Technology                                                          |
| ---------------- | ------------------------------------------------------------------- |
| Runtime          | **.NET 10** (`net10.0`), ASP.NET Core MVC, Razor Views              |
| Database         | **SQLite** via Entity Framework Core (`Microsoft.EntityFrameworkCore.Sqlite`) |
| Excel I/O        | **ClosedXML** (read seed files, write `.xlsx` exports)              |
| PDF              | **PDFsharp-MigraDoc** (custom A4 landscape layout)                  |
| Frontend         | Razor + plain HTML / CSS / vanilla JS (no SPA framework)            |
| Auth             | Custom session-based middleware (no Identity)                       |

## Project Structure

```
Tool-Master-Control/
└── CNCToolingDatabase/
    ├── Controllers/                # ASP.NET MVC controllers
    │   ├── AccountController.cs
    │   ├── HomeController.cs
    │   ├── SettingsController.cs           # 12 master-data screens + user mgmt
    │   ├── ToolCodeController.cs           # Tool code database with tool list view
    │   ├── ToolCodeUniqueController.cs     # Master Tool Code Database (list)
    │   ├── ToolCodeUniqueEditorController.cs
    │   ├── ToolListController.cs           # Tool List Database (list)
    │   └── ToolListEditorController.cs     # Editor + PDF/XLSX/CSV/TXT export
    ├── Data/
    │   ├── ApplicationDbContext.cs         # EF Core DbContext
    │   ├── DbSeeder.cs                     # Reads MASTER - *.xlsx and seeds tables
    │   ├── MASTER - *.xlsx                 # Seed data (copied to output)
    │   ├── LOGO/                           # Header logo for PDF
    │   ├── PART_IMAGE / PART_IMAGE_SEED/   # Per-part-number image library
    │   ├── PDF_EXPORT/                     # Static assets used by the PDF (TOOL_SPECS.png)
    │   └── STAMP/                          # Optional stamp image library
    ├── Helpers/
    │   ├── ExcelExportHelper.cs            # Shared XLSX styling / borders
    │   ├── ExcelHelper.cs                  # XLSX read helpers (used by seeder)
    │   ├── PdfFontBootstrap.cs             # Initializes PDFsharp fonts
    │   └── ToolListPdfGenerator.cs         # The full PDF layout (logo, specs, picture, table, stamps)
    ├── Middleware/
    │   └── AuthenticationMiddleware.cs     # Session redirect to /login
    ├── Models/                             # EF entities
    ├── Models/ViewModels/                  # MVC view models
    ├── Repositories/                       # EF-backed data access (User, ToolList, ToolMaster)
    ├── Services/                           # Business logic
    │   ├── AuthService.cs
    │   ├── ToolCodeService.cs
    │   ├── ToolCodeUniqueService.cs
    │   └── ToolListService.cs
    ├── Views/                              # Razor views (Account, Home, Settings/*, ToolList*, ToolCode*)
    ├── wwwroot/                            # Static assets (site.css, site.js)
    ├── Program.cs                          # Composition root, DI, middleware pipeline, dev-time ALTER TABLE
    ├── appsettings.json
    └── CNCToolingDatabase.csproj
```

## Getting Started

### Prerequisites

- **Windows / Linux / macOS** (Windows is the primary target — `PdfFontBootstrap` enables Windows fonts when running on Windows).
- **.NET SDK 10.0** (`dotnet --version` should report `10.0.x`).
- No external database service required — the app uses a local SQLite file (`CNCTooling.db`) created on first run.

### Build & Run

From the repository root:

```powershell
# Restore + compile
dotnet build CNCToolingDatabase\CNCToolingDatabase.csproj

# Run on http://localhost:5000 (or set PORT=8080)
dotnet run --project CNCToolingDatabase\CNCToolingDatabase.csproj
```

What happens on first run:

1. `Program.cs` calls `Database.EnsureCreated()` and then a series of idempotent `ALTER TABLE ADD COLUMN IF MISSING` statements — this lets you upgrade an older `CNCTooling.db` without dropping it.
2. `DbSeeder.Seed(...)` populates every master table from the `MASTER - *.xlsx` files bundled in `Data/`. Existing rows are preserved.
3. The custom auth middleware redirects unauthenticated requests to `/login`. After login you land on **Master Tool Code Database** (`/ToolCodeUnique`).

### First-time Login

Users come from `Data/MASTER - USER.xlsx`. If passwords are blank or the seed didn't load, two helper endpoints exist (no login required):

- `GET /Account/ReloadUsers` — Forces re-seeding from `MASTER - USER.xlsx`. Returns a JSON success message.
- `GET /Account/LoginDebug` — Returns `userCount` and the list of usernames + password length (no passwords) for diagnostics.

Then log in at `/login` with the username/password from the seed file (e.g. `adib.jamil / 123`).

## Modules / Navigation

The sidebar (`Views/Shared/_Layout.cshtml`) exposes six top-level modules:

| Sidebar Item                          | Route                       | Purpose                                                   |
| ------------------------------------- | --------------------------- | --------------------------------------------------------- |
| Master Tool Code Database             | `/ToolCodeUnique`           | Browse / search the user-maintained tool code reference   |
| Create / Edit Tool Code               | `/ToolCodeUniqueEditor`     | Add or edit a tool code in the master                     |
| Tool Code Database with Tool List     | `/ToolCode`                 | Cross-view of tool master + tool lists that use each tool |
| Tool List Database                    | `/ToolList`                 | List/search/filter all tool lists, see lock status        |
| Create / Edit Tool List               | `/ToolListEditor`           | The main editor (header, details, stamps, exports)        |
| Settings                              | `/Settings`                 | 12-card grid of master data screens + Reset All           |

The Settings landing page links to:

- User, Part Number, Project Code, Machine Name, Machine Workcenter, Machine Model, Operation, Revision, CAM Leader, CAM Programmer, Material Specification (On Drawing), Tool Supplier.

Each settings page supports server-side pagination, sorting, search, create / edit (modal), soft-active flag, and a per-page **Reset** button that re-seeds that table from its `MASTER - *.xlsx`.

## Data Model

Primary entities (see `Models/`):

- **User** — `Username` (unique), `Password` (plain in DB — see [security note](#security-note)), `DisplayName`, `Stamp` (BLOB, used in PDF and on-screen), `IsActive`.
- **ToolListHeader** — One row per tool list. Fields:
  - `ToolListName` (auto-generated as `{PartNumber}_{Operation}_{Revision}`)
  - `PartNumber`, `Operation`, `Revision`, `ProjectCode`, `MachineName`, `MachineWorkcenter`, `MachineModel`, `MaterialSpecId`
  - Approver fields (3 stages):
    - `ApprovedByUserId` / `ApprovedBy` / `ApprovedDate` — CAM Programmer stamp
    - `CamLeaderApprovedByUserId` / `CamLeaderApprovedDate` — CAM Leader stamp
    - `ToolRegisterByUserId` / `ToolRegisterByDate` — Tool Register stamp
  - `LockedBy`, `LockStartTime`, `LastHeartbeat` — concurrent edit lock
- **ToolListDetail** — Tool rows for a header (`ToolNumber`, `ToolDescription`, `ConsumableCode`, `Supplier`, `HolderExtensionCode`, `Diameter`, `FluteLength`, `ProtrusionLength`, `CornerRadius`, `ArborCode`, `ToolPathTimeMinutes`, `Remarks`).
- **ToolMaster** — Auto-maintained from saved tool list rows; one row per `ConsumableCode`.
- **ToolCodeUnique** — User-maintained registry of consumable codes (separate from `ToolMaster`). `Id` is a running "No." that always increases.
- Reference tables: `ProjectCode`, `MachineName`, `MachineWorkcenter`, `MachineModel`, `CamLeader`, `CamProgrammer`, `Operation`, `Revision`, `PartNumber`, `MaterialSpec`, `ToolSupplier`.

Relationships:

- `ToolListHeader 1..* ToolListDetail` (cascade delete).
- `ToolListHeader *..1 MaterialSpec` (set null on delete).
- `MachineName *..1 MachineModel` (set null on delete).
- `PartNumber *..1 ProjectCode`, `PartNumber *..1 MaterialSpec` (set null).

## Master Data & Seeding

All seed data lives next to the binary in `Data/MASTER - *.xlsx`. They are copied to the output folder via the `<Content Include="...">` entries in `CNCToolingDatabase.csproj`.

`DbSeeder` performs:

1. Defensive `CREATE TABLE IF NOT EXISTS` and `ALTER TABLE ADD COLUMN` statements (so old DBs can be upgraded in place).
2. Reads each `MASTER - *.xlsx` and merges rows into the corresponding table — existing rows are kept, new rows added.
3. `DbSeeder.ResetUsers(...)` and the per-page "Reset" buttons (`Settings/Reset*`) re-import a single sheet on demand.

If you change a seed file:

- Rebuild the project so `dotnet` copies the file into the output folder.
- Click the relevant "Reset" button on the Settings page (or call `/Settings/ResetAllSettings` from the Settings landing page).

## Approval Workflow & Stamps

Each tool list has three sequential stamps (rendered as cells in `Views/ToolListEditor/Index.cshtml` and in the PDF footer):

| Cell | Label              | Server field set                                                           | Display name source                          |
| ---- | ------------------ | -------------------------------------------------------------------------- | -------------------------------------------- |
| 1    | `CAM Programmer:`  | `ApprovedByUserId` / `ApprovedDate` (stamp). `ApprovedBy` overwritten with approver's display name. | `Model.CamProgrammer` (header dropdown) |
| 2    | `Approved by:`     | `CamLeaderApprovedByUserId` / `CamLeaderApprovedDate`                      | `Model.ApprovedBy`                           |
| 3    | `Tool Register By:`| `ToolRegisterByUserId` / `ToolRegisterByDate`                              | Looked up from `Users.DisplayName`           |

Rules enforced by the controller:

- Stamps must be applied in order (1 -> 2 -> 3).
- Rejecting an upstream stamp is blocked while a downstream stamp exists.
- Each user uploads their stamp image via `Settings/User -> Update Stamp`. The image is served by `GET /Settings/UserStamp?id={userId}` and embedded in the PDF.

The endpoints:

- `POST /ToolListEditor/Approve` / `Reject`
- `POST /ToolListEditor/ApproveCamLeader` / `RejectCamLeader`
- `POST /ToolListEditor/ApproveToolRegister` / `RejectToolRegister`

Approve responses include `approvedByUserId`, `approvedDateFormatted`, and `approvedByName` so the UI updates without a page refresh.

## Export Formats

The export endpoint is `GET /ToolListEditor/Export?id={id}&format={pdf|excel|csv|txt}`.

### PDF (`format=pdf`)

Generated by `Helpers/ToolListPdfGenerator.cs` using PDFsharp + MigraDoc. Layout (A4 landscape, 1.5 cm margins on each side):

1. Logo + title (`Master Tooling List`).
2. Six-column borderless info row (Tool List No., Part Description, Project Code, Unit, Work Centre, Machine Model).
3. **Specs table** (9 rows x 2 columns) with the same header data shown in the on-screen form.
4. Picture cell (left, merged across 11 columns) + Tool Specs reference image (right).
5. Tool list table (12 columns, full content width).
6. Three stamp cells with `<Label>: <Name>` + signature image + approval date.
7. Footer with page number.

All bordered tables (specs row, picture row, tool list, ...) share **one rightmost border** that matches the page's right margin (the column widths are scaled to fill `29.7 cm - 2 * 1.5 cm = 26.7 cm`). See `WidthScale` in `ToolListPdfGenerator.cs`.

### Excel (`format=excel`)

Produced via ClosedXML. Layout:

```
Tool List:        ...
Part Number:      ...
Part Description: ...
Operation:        ...
Revision:         ...
Project Code:     ...
Machine:          ...
Workcenter:       ...
Machine Model:    ...
                                <-- blank
[Tool list table with bordered header row + data rows]
                                <-- 3 blank rows
CAM Programmer:   ...
Approved By:      ...
```

### CSV / TXT (`format=csv` / `format=txt`)

Same content as Excel but with `,` (CSV) or tab (TXT) separator. The signatures appear three blank lines below the table.

File names follow `{ToolListName}_{yyyyMMdd_HHmmss}.{ext}`.

## Concurrency Model

- A tool list is locked to one editor at a time.
- When the editor opens (`GetToolListForEditAsync`), `AcquireLockAsync` either grabs the lock (if free) or marks the view as **read-only** with a "Locked by `<user>`" banner.
- The browser sends `POST /ToolListEditor/Heartbeat` on a timer.
- `ReleaseExpiredLocksAsync(TimeSpan.FromMinutes(1))` runs on the listing endpoint and frees any lock whose heartbeat is older than 1 minute (covers tab-closed without explicit unlock).
- Saving and explicit logout call `/ToolListEditor/ReleaseLock`.

This is **single-instance** locking — it is not safe across multiple processes pointing at the same SQLite file from different machines.

## Configuration

Most configuration is hard-coded in `Program.cs`:

- **Port** — `PORT` env var (defaults to `5000`). Skipped entirely if `ASPNETCORE_URLS` is set (e.g. on shared hosts like MonsterASP).
- **Connection string** — SQLite, `Data Source=CNCTooling.db` (relative to the working directory).
- **Session timeout** — 8 hours idle.
- **Logging** — `appsettings.json` / `appsettings.Development.json`.

`launchSettings.json` is intentionally minimal; deployment relies on environment variables.

## Maintenance & Troubleshooting

| Symptom                                              | What to do                                                                                  |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| Cannot log in / "Invalid username or password"       | Hit `GET /Account/ReloadUsers`, then `GET /Account/LoginDebug` to confirm users / lengths.  |
| Locked tool list won't release                       | Wait ~1 minute (heartbeat expiry) or have the locking user re-open + close. The list page shows current lock holders. |
| Export PDF has no logo / wrong logo                  | Replace `Data/LOGO/ZENIX.png` and rebuild (or copy into `bin/.../net10.0/Data/LOGO/`).      |
| Part image missing in PDF                            | Drop `<PartNumber>.png/.jpg/.jpeg/.gif` into `Data/PART_IMAGE/`.                             |
| Master data drifted from Excel                       | Use the per-page `Reset` button on `/Settings/...`, or `Reset All Settings` from `/Settings`. |
| Need to add a new column to an existing DB           | Add an `EnsureColumn(...)` line in `Program.cs` and ship — it runs idempotently on startup. |

### Security Note

This app stores plain-text passwords (`Password` column) and uses session-cookie auth without anti-forgery tokens on POST actions. It is suitable for an **internal LAN deployment behind a trusted network**. Before exposing to the public internet, you should at minimum:

- Hash + salt passwords (e.g. `PasswordHasher<User>`).
- Add `[ValidateAntiForgeryToken]` to mutating endpoints.
- Replace the custom middleware with ASP.NET Core Identity or another battle-tested auth stack.

## Deployment Notes

- The csproj copies every `Data/MASTER - *.xlsx` and the `LOGO/`, `PART_IMAGE/`, `PART_IMAGE_SEED/`, `PDF_EXPORT/`, `STAMP/` folders to the output directory with `PreserveNewest`. Make sure the deployment target receives the `bin/<config>/net10.0/Data/...` tree.
- A static-files mapping at `/Data/LOGO` is registered in `Program.cs` so the editor view can load the logo via HTTP.
- For shared ASP.NET Core hosting (e.g. MonsterASP) the host sets `ASPNETCORE_URLS`; the app respects that and does not call `UseUrls`.
- The SQLite database is created next to the running binary (`CNCTooling.db`). Persist that file across deploys (or back it up regularly).

---

_Developed by **UPECA PDC**._
