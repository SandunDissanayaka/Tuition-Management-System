# Tuition Management System

**A Windows desktop application for managing students, attendance, class fees, and daily financial records at Jayagath Institute.**

![Platform](https://img.shields.io/badge/platform-Windows-0078D4)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![UI](https://img.shields.io/badge/UI-WPF-0C54C2)
![Database](https://img.shields.io/badge/database-SQLite-003B57)
![Architecture](https://img.shields.io/badge/architecture-MVVM-4B5563)

## Overview

Tuition Management System replaces repetitive, spreadsheet-based administrative tasks with a centralized desktop workflow. The application helps institute staff maintain student information, record attendance, track paid and outstanding class fees, review daily income and expenses, and generate PDF reports.

The current implementation supports **Grade 10** and **Grade 11** classes, stores data in a **local SQLite database**, and operates **offline** on Windows.

## Key Features

| Module | Capabilities |
| --- | --- |
| **Authentication** | User registration and login, account status verification, and role-aware navigation. |
| **Grade Selection** | Separate workflows and student lists for Grade 10 and Grade 11. |
| **Home Dashboard** | Today's class details, enrollment, attendance, fee collection, and pending-fee overview. |
| **Student Management** | Register, search, edit, and remove student records; store school, parent/guardian, and contact details. Student modifications are restricted to administrators. |
| **Attendance & Fees** | Record date-specific classes, lesson names, daily fees, and present/absent statuses; mark payments, search students, and navigate paginated attendance lists. |
| **Pending Fees** | View outstanding payments for attended classes, search records, and record fee collection. |
| **Daily Summary** | View attendance and payment totals; record printing and venue expenses and calculate net daily income. |
| **Reports** | Search a student's attendance history by name or student code and date range; export pending-fee reports to PDF. |
| **User Management** | Administrator-only account listing, password resets, account activation/deactivation, and user removal. |

### Fee calculation

A session fee is collected when a student is marked **Present** and **Paid**. A student marked **Present** without payment is included in pending fees. An **Absent** or **Not Marked** student is not charged for that session by the current application logic.

## Technology Stack

| Component | Technology |
| --- | --- |
| Language | C# |
| Framework | .NET 8 (`net8.0-windows`) |
| Desktop UI | Windows Presentation Foundation (WPF / XAML) |
| Application pattern | Model–View–ViewModel (MVVM) |
| Database | SQLite via `Microsoft.Data.Sqlite` **8.0.8** |
| PDF generation | QuestPDF **2024.10.1** |
| Development environment | Visual Studio 2022 / .NET SDK 8 |

## Getting Started

### Prerequisites

- Windows 10 or Windows 11.
- **For development:** .NET 8 SDK and Visual Studio 2022 with the **.NET desktop development** workload (or a terminal with the .NET SDK).
- **For framework-dependent builds:** .NET 8 Desktop Runtime on the target Windows computer. Self-contained published builds bundle the necessary .NET runtime.

### Run from source

1. Download or clone this repository, then open `TuitionManagementSystem.sln` in Visual Studio.
2. Restore NuGet dependencies and build the solution.
3. Run the application using **F5** or **Start**.

Alternatively, from a Windows terminal in the repository root:

```powershell
dotnet restore TuitionManagementSystem.sln
dotnet build TuitionManagementSystem.sln --configuration Release
dotnet run --project TuitionManagementSystem.csproj
```

**First launch:** The application automatically creates its database and tables and seeds an initial administrator account. For a development/demo environment, the initial administrator username is `admin`; its seeded password is configured in `Data/DatabaseHelper.cs` (`SeedDefaultAdmin`). **Replace the hard-coded credential and secure account provisioning before distributing the application or entering real student data.** New registrations receive the `User` role by default.

### Typical workflow

1. Sign in and select **Grade 10** or **Grade 11**.
2. Add or review student details under **Students** (administrator for changes).
3. Open **Attendance & Fees**, select a class date, enter the lesson and daily fee, and save attendance/payment statuses.
4. Use **Pending Fees** to identify and record outstanding payments.
5. Use **Daily Summary** to enter printing and venue expenses and review the net amount.
6. Use **Reports** to review attendance history or export a pending-fee PDF.

## Build a Windows Release

For a **self-contained 64-bit Windows** build:

```powershell
dotnet publish TuitionManagementSystem.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output ./publish
```

The `publish/` directory contains the application executable and any required published files. **Distribute the complete contents** of this directory, preferably as a ZIP attached to a GitHub Release. On the destination computer, extract the ZIP to a **writable folder** and launch `TuitionManagementSystem.exe`.

> **Why a writable folder?** The database is created alongside the running executable, under `Data/TuitionManagement.db`. Locations such as `Program Files` may prevent the application from creating or updating this file without additional permissions.

## Project Structure

```text
Tuition-Management-System/
├── App.xaml / App.xaml.cs        # Startup, database initialization, login
├── Data/
│   └── DatabaseHelper.cs        # SQLite schema and data access operations
├── Helpers/
│   ├── AppSession.cs            # Current user and selected grade
│   ├── PasswordHelper.cs        # Current password hashing/verification
│   ├── RelayCommand.cs          # WPF command support
│   ├── ViewModelBase.cs         # Property-change notifications
│   └── Converters.cs            # XAML value converters
├── Models/                       # User, Student, ClassSession, AttendanceRecord
├── ViewModels/                   # Presentation logic for application modules
├── Views/                        # WPF windows and user controls
├── TuitionManagementSystem.csproj
└── TuitionManagementSystem.sln
```

### Application architecture

The project follows an **MVVM-inspired separation of concerns**:

- **Views:** XAML screens and limited code-behind for UI behavior.
- **ViewModels:** Screen state, commands, input validation, and application workflows.
- **Models:** Data structures representing users, students, sessions, and attendance records.
- **Data access:** `DatabaseHelper` contains SQLite queries, initial schema creation, and a small schema-update routine.

The local database uses four principal tables: `Users`, `Students`, `Sessions`, and `AttendanceRecords`. Each attendance record links a student to a class session and stores attendance and payment information.

## Data Storage and Backups

The SQLite database is created automatically at:

```text
<application-directory>/Data/TuitionManagement.db
```

To back up the data, **close the application first**, then copy the database file to a secure backup location. Keep backups private: they may contain student and parent/guardian information. Restoring a backup involves replacing the database file while the application is closed. Use a backup that is compatible with the application's current schema.

No cloud database, multi-computer synchronization, or hosted server is included in this repository.

## Testing and Quality

The repository contains the application source, but **does not include an automated test project or a CI workflow**. Before publishing a release, manually verify at least:

- Registration, login, disabled-account behavior, and administrator permissions.
- Student creation, search, editing, and deletion.
- Attendance and fee status changes across multiple dates and pages.
- Pending payment collection and daily financial calculations.
- PDF generation and attendance-history filtering.
- First-run database creation and data persistence after restarting.

## Security and Deployment Notes

- Do **not** commit real student databases, passwords, or generated records to the repository.
- Review user permissions and data protection requirements before using the software outside a controlled demonstration.
- Restrict access to the installed application and its local database. The application is not designed as a multi-user network service.
- QuestPDF is configured with its Community license setting; verify that its license terms suit your intended distribution.

## Project Team

| Team member | Project role |
| --- | --- |
| **D.M.S.T. Dissanayaka** | Project Manager |
| **K.A.S.H. Wijerathne** | UI/UX Designer |
| **K.P.V.P. Divyanjali** | Business Analyst |
| **W.S.N. Rathnayke** | Main Developer |
| **P.D.L. Raveesha** | QA Engineer |

Developed as an academic group project for Jayagath Institute's tuition administration workflow.

## License

**No project license is included in the provided repository.** Unless the maintainers add a `LICENSE` file, do not assume permission to reuse or redistribute the source code beyond applicable law and the rights granted by the maintainers. Third-party packages retain their own licenses.
