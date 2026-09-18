# Database

Microsoft SQL Server 2019 or newer. Run the scripts in order:

```bash
sqlcmd -S localhost,1433 -U sa -P "Your_password123" -C -i 01_schema.sql
sqlcmd -S localhost,1433 -U sa -P "Your_password123" -C -i 02_seed.sql
```

Both are idempotent — `01_schema.sql` only creates what is missing and `02_seed.sql` merges rows by their
natural key — so they can be re-run safely after a change.

## Tables

| Table            | Purpose |
| ---------------- | ------- |
| `Roles`          | `Administrator` and `Editor`. |
| `Users`          | CMS accounts. `PasswordHash` holds a BCrypt hash, never a password. |
| `RefreshTokens`  | One row per issued refresh token, stored as a SHA-256 hash and revoked on use. |
| `Announcements`  | News items with a unique slug, category, draft/published state and author. |
| `Programs`       | Academic programs shown in the catalogue, ordered by `DisplayOrder`. |
| `ContentPages`   | Editable static pages addressed by slug (`about`, `admission`, `contact`). |
| `Faqs`           | Chatbot knowledge base; `Keywords` is a comma separated list used by the matcher. |
| `ChatMessages`   | Transcript of every chatbot exchange, grouped by `SessionId`. |

Timestamps are `DATETIME2(3)` in **UTC** — the API writes `SYSUTCDATETIME()` or `DateTime.UtcNow` and the
frontend formats them for display.

## Seeded accounts

| Email                | Password       | Role          |
| -------------------- | -------------- | ------------- |
| admin@bcas.edu.ph    | `Admin@12345`  | Administrator |
| editor@bcas.edu.ph   | `Editor@12345` | Editor        |

These exist so the CMS can be opened right after setup. Change them before the portal is reachable by anyone
else.
