# BCAS_Web

Dynamic web information portal and integrated CMS for **Batangas College of Arts and Sciences (BCAS)**, with a
rule-based chatbot for common student questions. BSIT capstone project.

| Layer     | Technology                                                       |
| --------- | ---------------------------------------------------------------- |
| Frontend  | React 19 + TypeScript, Vite, React Router                         |
| Backend   | ASP.NET Core 8 Web API (C#), Dapper, JWT bearer authentication    |
| Database  | Microsoft SQL Server 2019+                                        |
| Tooling   | Docker Compose, GitHub Actions, xUnit, Swagger / OpenAPI          |

---

## Repository layout

```
backend/                  ASP.NET Core solution (BCAS.sln)
  src/BCAS.Domain/        Entities - no dependencies
  src/BCAS.Application/   DTOs, service interfaces, business rules
  src/BCAS.Infrastructure/Dapper repositories, JWT and password hashing
  src/BCAS.Api/           Controllers, middleware, Program.cs
  tests/BCAS.UnitTests/   xUnit tests
database/                 SQL Server schema and seed scripts
frontend/                 React + TypeScript single page application
docs/                     API reference
```

The backend follows a layered (clean-architecture style) design: `Api → Application → Domain`, with
`Infrastructure` implementing the interfaces the application layer declares. Only `Infrastructure` knows about
SQL Server, so the data access can be swapped without touching the business rules.

---

## Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download)
- [Node.js 20 or newer](https://nodejs.org/) (the repository is built with Node 22)
- SQL Server 2019+ — a local instance, SQL Server Express, or the Docker image
- `sqlcmd` or SQL Server Management Studio to run the database scripts

---

## Quick start with Docker

Everything (database, API, portal) in one command:

```bash
docker compose up --build
```

| Service | URL                              |
| ------- | -------------------------------- |
| Portal  | http://localhost:3000            |
| API     | http://localhost:8080            |
| Swagger | http://localhost:8080/swagger    |
| SQL     | localhost,1433 (`sa`)            |

The `db-migrate` service applies `database/01_schema.sql` and `database/02_seed.sql` automatically before the
API starts.

---

## Manual setup

### 1. Database

```bash
sqlcmd -S localhost,1433 -U sa -P "Your_password123" -C -i database/01_schema.sql
sqlcmd -S localhost,1433 -U sa -P "Your_password123" -C -i database/02_seed.sql
```

Both scripts are idempotent, so they can be re-run after a schema change.

Seeded CMS accounts (**development only — change them before going live**):

| Email                | Password       | Role          |
| -------------------- | -------------- | ------------- |
| admin@bcas.edu.ph    | `Admin@12345`  | Administrator |
| editor@bcas.edu.ph   | `Editor@12345` | Editor        |

### 2. Backend

```bash
cd backend
dotnet restore
dotnet user-secrets --project src/BCAS.Api set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=BCAS_Web;User Id=sa;Password=Your_password123;TrustServerCertificate=True;Encrypt=True;"
dotnet user-secrets --project src/BCAS.Api set "Jwt:Key" "a-signing-key-of-at-least-32-characters"
dotnet run --project src/BCAS.Api
```

The API starts on <http://localhost:5216> with Swagger UI at `/swagger`.

> The signing key must be at least 32 characters; the API refuses to start otherwise. In production supply it
> through the `JWT__KEY` environment variable and never commit it.

### 3. Frontend

```bash
cd frontend
npm install
cp .env.example .env.local     # adjust VITE_API_BASE_URL if the API runs elsewhere
npm run dev
```

The portal starts on <http://localhost:5173>, which is the origin allowed by the API's CORS policy
(`Cors:AllowedOrigins` in `appsettings.json`).

---

## What the system does

**Public portal** — home page with the latest news and programs, searchable and paginated announcements with
category filters, program catalogue, and CMS-editable pages (`/pages/about`, `/pages/admission`,
`/pages/contact`).

**Content management** — staff sign in at `/login` and manage announcements and the chatbot knowledge base at
`/admin`. Editors create and update content; only administrators may delete it.

**Chatbot** — the floating assistant scores a visitor's message against the keywords of the FAQ entries stored
in SQL Server and replies with the best match, or offers suggestions when nothing matches. Every exchange is
written to `dbo.ChatMessages`, so the staff can see which questions the knowledge base still misses. No external
AI service is required.

---

## Security notes

- Passwords are stored as BCrypt hashes (work factor 12).
- Access tokens are short-lived JWTs; refresh tokens are random 512-bit values stored **hashed** (SHA-256) and
  rotated on every use, so a leaked database cannot be replayed.
- Content is rendered as text, never as HTML, so stored copy cannot inject markup into the portal.
- Every SQL statement is parameterised through Dapper, and `LIKE` search terms are escaped.
- Authorisation is role based: `Administrator` and `Editor` may write, only `Administrator` may delete.

---

## Tests and checks

```bash
cd backend  && dotnet test        # xUnit: slug generation, paging rules, chatbot matching, JWT and hashing
cd frontend && npm run lint       # oxlint
cd frontend && npm run build      # TypeScript type check + production bundle
```

GitHub Actions runs all of the above on every push and pull request (`.github/workflows/ci.yml`).

---

## Documentation

- [`docs/api-reference.md`](docs/api-reference.md) — endpoint by endpoint reference
- [`database/README.md`](database/README.md) — schema notes and how to re-run the scripts
- Swagger UI at `/swagger` when the API runs in development
