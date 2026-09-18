# Database

SQL Server schema for the BCAS web portal and CMS (BW-9).

## Scripts

| Script | Purpose |
| --- | --- |
| `001_schema.sql` | Schemas, tables, foreign keys and indexes. Idempotent. |
| `002_seed.sql` | Content statuses, roles, the four departments and the initial Super Admin. Idempotent. |

Both scripts are repeatable from an empty database and safe to re-run, so a
teammate can reset their local database at any time.

## Applying them

```bash
sqlcmd -S localhost -U sa -P "<password>" -Q "IF DB_ID('BcasWeb') IS NULL CREATE DATABASE BcasWeb;"
sqlcmd -S localhost -U sa -P "<password>" -d BcasWeb -i database/001_schema.sql
sqlcmd -S localhost -U sa -P "<password>" -d BcasWeb -i database/002_seed.sql
```

## Layout

Three schemas keep the modules apart:

- **`auth`** — `Departments`, `Roles`, `Users`, `UserDepartments`
- **`content`** — `ContentStatuses`, `Images`, `News`, `Announcements`, `Events`,
  `AcademicPrograms`, `AdmissionRequirements`, `ServicesPolicies`, `Faqs`
- **`ops`** — `Inquiries`, `ActivityLog`, `SearchAnalytics`

Every content table follows the same contract:

- `DepartmentId` — `NULL` means school-wide, otherwise the owning department
- `StatusId` — `Draft` / `Scheduled` / `Published` / `Archived`, via `content.ContentStatuses`
- `PublishAt` — when a scheduled item goes live
- `CreatedBy` / `CreatedAt` / `UpdatedBy` / `UpdatedAt` — audit columns

## Seeded Super Admin

| | |
| --- | --- |
| Email | `superadmin@bcas.edu.ph` |
| Password | `ChangeMe!2026` |

The account is created with `MustChangePassword = 1`. **Rotate this credential
before deploying anywhere reachable outside localhost.**

Passwords are stored as PBKDF2-HMAC-SHA256, 210,000 iterations, 16-byte salt,
32-byte key, encoded as `pbkdf2-sha256$<iterations>$<b64 salt>$<b64 hash>`. The
iteration count travels with each hash, so it can be raised later without
invalidating existing credentials.
