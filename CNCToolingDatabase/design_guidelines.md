# CNC Tooling Database — Design Guidelines

## Brand Identity

**Purpose**: Production-grade internal tool for engineers managing 4000+ part numbers with centralized tooling data. Eliminates Excel chaos, enables reuse, maintains revision control.

**Aesthetic Direction**: **Industrial/Technical** — Clean, data-focused, no-nonsense. This is an engineering tool, not a consumer app. Visual hierarchy serves function: critical data is instantly scannable, actions are one-click, noise is eliminated. Think precision machining translated to UI.

**Memorable Element**: Ultra-fast data navigation with persistent context. The collapsible sidebar stays visible across all operations, and every table remembers your filters/sort state. Engineers never lose their place.

---

## Navigation Architecture

**Root Navigation**: Persistent left sidebar (collapsible) + top header

**Sidebar Modules** (fixed order):
1. **Tool Code Database** — Global read-only consumable catalog
2. **Tool List Database** — All Part+OP combinations with edit status
3. **Create/Edit Tool List** — Active editing workspace

**Top Header** (always visible):
- Left: App name "CNC Tooling Database"
- Right: Logged-in username + Logout button

**Login Flow**:
- `/login` — Unauthenticated users always redirected here
- Session-based authentication (no external providers)
- Post-login redirect to last visited module or Tool Code Database

---

## Screen-by-Screen Specifications

### 1. Login Screen (`/login`)
**Layout**:
- Centered card (400px max-width)
- No header/sidebar
- White background

**Components**:
- App name (large, bold)
- Username input field
- Password input field (masked)
- Login button (full-width, primary color)
- Error message area (hidden until failure)

**Behavior**:
- Enter submits form
- Invalid credentials show inline error
- Successful login → redirect to Tool Code Database

---

### 2. Tool Code Database (Module 1)
**Purpose**: Engineers search the global consumable catalog to find/verify tool specs before creating tool lists.

**Layout**:
- Header: Module title "Tool Code Database" (left), Export buttons (right)
- Search bar (full-width, sticky below header)
- Filter row: Dropdowns for Consumable Code, Diameter, Arbor Code, Holder/Extension, Part Number
- Data table (frozen header row)
- Pagination controls (bottom)

**Table Columns** (exact order):
1. Tool Number
2. Tool Description
3. Consumable Code
4. Supplier
5. Holder/Extension Code
6. Diameter
7. Flute Length
8. Protrusion Length
9. Corner Radius
10. Arbor Code
11. Part Number
12. Operation
13. Revision
14. Project Code
15. Machine Name
16. Machine Workcenter

**Features**:
- Live search (debounced 300ms)
- Column sort (click header)
- Multi-select filters (AND logic)
- Export: Excel / CSV / TXT buttons (icon + label)
- 100 rows/page
- Read-only (no inline editing)

**Empty State**: "No tools match your filters" with reset filters button

---

### 3. Tool List Database (Module 2)
**Purpose**: Engineers browse all Part+OP tool lists, see edit status, open for modification.

**Layout**:
- Header: Module title "Tool List Database" (left), Export buttons (right)
- Search bar
- Data table (frozen header)
- Pagination controls

**Table Columns** (exact order):
1. Tool List Name (blue hyperlink)
2. Part Number
3. Operation
4. Revision
5. Created By
6. Created Date
7. Status (badge: Available / Locked by [user])
8. Last Modified Date
9. Editing Duration (if locked)
10. Action (icon button: Edit if available, View if locked)

**Behavior**:
- Clicking Tool List Name opens Create/Edit in NEW TAB
- Locked rows show who's editing + duration
- Status badge color: Green (Available), Red (Locked)
- Export buttons exclude Action column

**Empty State**: "No tool lists created yet" with "Create New" button

---

### 4. Create/Edit Tool List (Module 3)
**Purpose**: Active workspace for building/editing Part+OP tool lists.

**Layout**:
- Top section (Header Inputs):
  - Auto-generated name (large, bold, read-only): `{PartNumber}_{Operation}_{Revision}`
  - Form grid (2 columns): Part Number, Operation, Revision, Project Code, Machine Name, Machine Workcenter
- Toolbar (sticky below header):
  - Left: Open, Save, Close buttons
  - Right: Export buttons
- Data table (editable, starts with 5 rows)
- Add Row / Remove Row buttons (bottom)

**Table Columns** (exact order):
1. Tool Number
2. Tool Description
3. Consumable Code
4. Supplier
5. Holder/Extension Code
6. Diameter
7. Flute Length
8. Protrusion Length
9. Corner Radius
10. Arbor Code

**Behavior**:
- Open: Modal picker to load existing Part+OP
- Save: Validates, saves header+details, updates ToolMaster, releases lock
- Close: Confirmation dialog if unsaved changes, releases lock
- Editable cells: Click to edit, Tab to next
- Numeric fields: Decimal validation (2 places)
- Auto-expand table as rows fill
- Heartbeat every 30s to maintain lock
- Lock released on Close or timeout (5 min idle)

**Empty State** (new tool list): 5 blank rows ready for input

---

## Color Palette

**Primary**: `#2563EB` (Blue-600) — Actions, links, primary buttons
**Accent**: `#DC2626` (Red-600) — Locked status, delete, errors
**Success**: `#16A34A` (Green-600) — Available status, save confirmation
**Background**: `#F9FAFB` (Gray-50) — Page background
**Surface**: `#FFFFFF` — Cards, tables, modals
**Border**: `#E5E7EB` (Gray-200) — Table borders, dividers
**Text Primary**: `#111827` (Gray-900)
**Text Secondary**: `#6B7280` (Gray-500)

---

## Typography

**Font**: System default (`-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`)

**Type Scale**:
- H1 (Module Titles): 24px, Bold, Gray-900
- H2 (Section Headers): 18px, Semibold, Gray-900
- Body (Table Data): 14px, Regular, Gray-900
- Small (Metadata): 12px, Regular, Gray-500
- Button: 14px, Semibold

---

## Visual Design

**Tables**:
- Zebra striping (even rows: Gray-50)
- 1px Gray-200 borders
- 12px cell padding
- Frozen header with Gray-100 background
- Hover: Gray-100 background

**Buttons**:
- Primary: Blue-600 background, white text, 8px padding, 4px radius
- Secondary: White background, Gray-300 border, Gray-700 text
- Icon buttons: Gray-400 icon, hover Gray-600

**Inputs**:
- Gray-300 border, 8px padding, 4px radius
- Focus: Blue-600 border, 2px outline

**Status Badges**:
- Available: Green-100 background, Green-700 text
- Locked: Red-100 background, Red-700 text
- 4px padding, 999px radius (pill shape)

---

## Assets to Generate

1. **app-icon.png** — WHERE USED: Browser tab favicon, sidebar logo (32×32px)
   - Minimalist icon representing precision/tooling (e.g., stylized cutting tool or "T" lettermark)
   - Blue-600 on transparent background

2. **empty-tools.svg** — WHERE USED: Tool Code Database empty state
   - Illustration of organized tool cabinet/drawer (line art style)
   - Gray-400 color

3. **empty-toollist.svg** — WHERE USED: Tool List Database empty state
   - Illustration of empty checklist or table outline
   - Gray-400 color

4. **locked-icon.svg** — WHERE USED: Tool List Database status column (when locked)
   - Lock icon, 16×16px, Red-600 color