# CNC Tooling Database

## Overview

A production-grade internal tool for CNC engineers managing 4000+ part numbers with centralized tooling data. The application replaces Excel-based workflows with a database-driven system featuring tool code catalogs, tool list management, and revision control.

## Current Status

**Application is fully functional and running on port 5000**

## User Preferences

Preferred communication style: Simple, everyday language.

## System Architecture

### Primary Application: ASP.NET Core MVC Web Application

Located in `CNCToolingDatabase/` directory:
- Server-side rendered views using Razor
- Entity Framework Core 8.0 with SQLite database
- Session-based authentication (no external OAuth/Identity)
- Industrial/technical design aesthetic optimized for data-heavy workflows
- Runs on port 5000

### Backend Architecture

**ASP.NET Core (.NET 8.0)**
- MVC pattern with Controllers and Razor views
- Entity Framework Core with SQLite (`CNCTooling.db`)
- Repository pattern for data access
- Service layer for business logic
- Session-based authentication with hardcoded users

### Authentication

- Hardcoded users: `user/123`, `john/123`, `kim/123`
- Session-based with middleware protection
- Unauthenticated requests redirect to `/login`

### Database Structure

**Entities:**
- `User` - Authentication users
- `ToolListHeader` - Part number, operation, revision, project, machine, work center
- `ToolListDetail` - Individual tools with specifications (diameter, flute length, etc.)
- `ToolMaster` - Derived tool code catalog (auto-updated from ToolListDetail changes)

### Navigation Structure

- Persistent left sidebar (collapsible) + top header
- Three main modules:
  1. **Tool Code Database** (`/ToolCode`) - Read-only view of ToolMaster records with filters
  2. **Tool List Database** (`/ToolList`) - CRUD operations with locking status
  3. **Create/Edit Tool List** (`/ToolListEditor`) - Inline editing with real-time locking

### Key Features

- Real-time locking mechanism (30-second heartbeat, 5-minute idle timeout)
- ToolMaster auto-updates from ToolListDetail changes
- Multi-format export (Excel/CSV/TXT)
- Revision control for tool lists
- Concurrent edit prevention

### Build and Development

- `npm run server:dev` - Starts the .NET backend via Node.js wrapper on port 5000

### Routes

| Route | Description |
|-------|-------------|
| `/` | Home page (requires auth) |
| `/login` | Login page |
| `/logout` | Logout action |
| `/ToolCode` | Tool Code Database (read-only) |
| `/ToolList` | Tool List Database (CRUD) |
| `/ToolListEditor` | Create/Edit Tool List |

### External Dependencies

**Database:**
- SQLite - Primary database stored at `CNCToolingDatabase/CNCTooling.db`

**.NET Packages:**
- Microsoft.EntityFrameworkCore.Sqlite 8.0.0
- Microsoft.EntityFrameworkCore.Design 8.0.0

### Environment Variables

- `PORT` - Server port (defaults to 5000)
