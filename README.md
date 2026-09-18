# BCAS_Web

Development of a Dynamic Web-Information Portal and Integrated CMS for Batangas College of Arts and Sciences (BCAS). Includes a public website, content management system, and chatbot feature. Built with ASP.NET Core Web API (C#), Dapper, SQL Server, React + TypeScript, and JWT auth. BSIT capstone project.

## Repository structure

```
BCAS_Web/
├── BCAS_Web.sln              Solution entry point
├── .editorconfig             Shared code style (C#, TypeScript, SQL)
├── backend/
│   ├── Directory.Build.props Shared MSBuild settings for every backend project
│   └── BCAS.Api/             ASP.NET Core Web API
│       ├── Common/           Cross-cutting: DB connection factory, error shapes
│       ├── Features/         One folder per feature (Auth, ActivityLog, …)
│       ├── Options/          Strongly-typed configuration sections
│       └── Program.cs        Composition root and HTTP pipeline
├── database/                 SQL Server schema and seed scripts (see its README)
└── frontend/                 React + TypeScript (Vite)
    └── src/
        ├── api/              HTTP client and API types
        ├── app/              Application shell and routing
        ├── features/         One folder per feature (auth, …)
        ├── routes/           Route-level pages and guards
        └── styles/           Global styles and design tokens
```

Backend code is organised by **feature**, not by layer: everything for a feature
(controller, service, repository, DTOs) sits in one folder under `Features/`.

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm
- SQL Server 2019+ (Developer Edition or the Docker image) and `sqlcmd`

## Getting started

### 1. Database

```bash
sqlcmd -S localhost -U sa -P "<password>" -Q "IF DB_ID('BcasWeb') IS NULL CREATE DATABASE BcasWeb;"
sqlcmd -S localhost -U sa -P "<password>" -d BcasWeb -i database/001_schema.sql
sqlcmd -S localhost -U sa -P "<password>" -d BcasWeb -i database/002_seed.sql
sqlcmd -S localhost -U sa -P "<password>" -d BcasWeb -i database/003_sessions_and_password_resets.sql
sqlcmd -S localhost -U sa -P "<password>" -d BcasWeb -i database/004_account_provisioning.sql
```

See [`database/README.md`](database/README.md) for the schema layout and the
seeded Super Admin credentials.

### 2. Backend

The connection string and the JWT signing key are **never committed**. Set them
through user secrets for local development:

```bash
cd backend/BCAS.Api
dotnet user-secrets set "ConnectionStrings:BcasDb" \
  "Server=localhost;Database=BcasWeb;User Id=sa;Password=<password>;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)"
dotnet run
```

The API starts on `https://localhost:7148` with Swagger UI at `/swagger` and a
health check at `/health`. In other environments the same two settings are read
from environment variables (`ConnectionStrings__BcasDb`, `Jwt__SigningKey`).

### 3. Frontend

```bash
cd frontend
cp .env.example .env.local   # adjust VITE_API_BASE_URL if the API is elsewhere
npm install
npm run dev
```

The dev server runs on `http://localhost:5173`, which is the origin allowed by
the API's CORS policy (`Cors:AllowedOrigins` in `appsettings.json`).

## Everyday commands

| Task | Backend | Frontend |
| --- | --- | --- |
| Build | `dotnet build` | `npm run build` |
| Run | `dotnet run --project backend/BCAS.Api` | `npm run dev` |
| Type check | — | `npm run typecheck` |
| Lint | style rules run as part of `dotnet build` | `npm run lint` |
| Format | `dotnet format` | `npm run format` |

Code style is shared through `.editorconfig` (C#, SQL and TypeScript indentation
and naming) plus ESLint and Prettier on the frontend. C# style violations are
reported as build warnings, so they surface without blocking a local build.

## Branching convention

- `main` — protected; always builds and runs.
- `feature/BW-<ticket>-<short-slug>` — one branch per Jira ticket, e.g.
  `feature/BW-10-jwt-login`.
- `fix/BW-<ticket>-<short-slug>` — bug fixes against an existing feature.

Branch off `main`, keep the Jira key in the branch name and in every commit
subject (`BW-10: add login endpoint`), and open a pull request back into `main`.
A branch is merged only after the build passes and one teammate has reviewed it.

## Authentication

| Endpoint | Auth | Purpose |
| --- | --- | --- |
| `POST /api/auth/login` | anonymous | Returns a signed JWT plus the user's basic profile. |
| `GET /api/auth/session` | bearer | Re-reads the signed-in user so the client can route on load. |
| `POST /api/auth/logout` | bearer | Revokes the caller's token. Idempotent. |
| `POST /api/auth/forgot-password` | anonymous | Emails a reset link when the account exists. |
| `POST /api/auth/reset-password` | anonymous | Completes a reset using the token from the link. |
| `GET /api/admin/accounts` | Super Admin | Lists all accounts, deactivated ones included. |
| `GET /api/admin/accounts/reference` | Super Admin | Role and department options for the create form. |
| `POST /api/admin/accounts` | Super Admin | Provisions an account and emails its invitation. |
| `PUT /api/admin/accounts/{id}/activation` | Super Admin | Activates or deactivates an account. |

Token claims carry the user id (`sub`), the role code (`role`) and one `dept`
claim per department the user is scoped to.

| Role code | Role | Landing route |
| --- | --- | --- |
| `SUPER_ADMIN` | Super Admin | `/admin` |
| `ACADEMIC_HEAD` | Academic Head | `/department` |
| `REGISTRAR` | Admin Office/Registrar | `/registrar` |
| `VP_OPERATIONS` | VP of Operations | `/operations` |

Unknown emails, wrong passwords and deactivated accounts all return the same
`401` with a generic message, so the endpoint cannot be used to discover which
accounts exist. Repeated failures lock an account for a configurable window
(`Login:MaxFailedAttempts`, `Login:LockoutMinutes`).

### Sign-out

Access tokens are stateless, so signing out records the token's `jti` in
`auth.RevokedTokens` and the API rejects it for the rest of its lifetime. The
client also clears its stored session and navigates with `replace`, so the
browser's Back button cannot return to an admin screen; API responses are sent
`no-store` so nothing admin-related is served from cache.

### Password reset

`POST /api/auth/forgot-password` answers with the same message whether or not the
email is registered. When it is, a single-use link valid for
`PasswordReset:TokenLifetimeMinutes` is emailed; only the token's SHA-256 hash is
stored. Requesting a new link, or completing a reset, retires every outstanding
token for that account.

Without SMTP configured (`Email:Enabled` is `false` by default) the reset email
is written to the application log instead of being sent, so the link can be
copied from the console during development. To send real mail, set
`Email:Enabled`, `Email:SmtpHost` and the credentials — the password belongs in
user secrets or the deployment secret store, not `appsettings.json`.

Password policy is defined once in `Features/Auth/PasswordPolicy.cs` and mirrored
in `frontend/src/features/auth/passwordPolicy.ts` for inline feedback; the
server's copy is the one that decides.

## Account administration

Everything under `/api/admin/accounts` is Super Admin only; any other signed-in
role gets HTTP 403.

**Provisioning.** The Super Admin supplies a name, email, role and — for an
Academic Head only — a department. A department sent for any other role is
rejected, as is one missing for an Academic Head. A duplicate email returns
`409` with a message naming the address. No password is set at creation: the
account is stored with an unusable hash and emailed a link to
`/set-password`, valid for seven days and usable once.

**Activation.** Deactivating sets `auth.Users.IsActive` to false and nothing
else. Nothing is deleted, so authored content stays published with its original
author and the activity log is retained; reactivating restores access and clears
any leftover lockout, with no re-provisioning. The API refuses to let a Super
Admin deactivate their own account or the last active Super Admin.

A deactivated user cannot sign in, and an access token they already hold is
rejected on their next request — the bearer handler checks revocation and
account status together, in one query, on every authenticated call.
