# BCAS Web API reference

Base URL in development: `http://localhost:5216` (Docker: `http://localhost:8080`).
All responses are JSON; failures use RFC 7807 `application/problem+json`.

## Authentication

The CMS endpoints expect an `Authorization: Bearer <accessToken>` header. Tokens come from `/api/auth/login`
and are renewed with `/api/auth/refresh`; the frontend does this transparently when a request returns 401.

| Method | Route                | Auth | Body                          | Returns                           |
| ------ | -------------------- | ---- | ----------------------------- | --------------------------------- |
| POST   | `/api/auth/login`    | —    | `{ email, password }`         | `{ accessToken, refreshToken, expiresAt, user }` |
| POST   | `/api/auth/refresh`  | —    | `{ refreshToken }`            | same as login (the old token is revoked) |
| POST   | `/api/auth/logout`   | user | `{ refreshToken }`            | `204 No Content`                  |
| GET    | `/api/auth/me`       | user | —                             | `{ id, email, fullName, role }`   |

Roles: **Administrator** (full access) and **Editor** (create and update, no delete).

## Announcements

| Method | Route                              | Auth            | Notes |
| ------ | ---------------------------------- | --------------- | ----- |
| GET    | `/api/announcements`               | —               | Query: `page`, `pageSize` (max 100), `search`, `category`, `includeDrafts`. Drafts are only returned to CMS users. |
| GET    | `/api/announcements/categories`    | —               | Distinct categories of published items. |
| GET    | `/api/announcements/{slug}`        | —               | Single item; drafts are 404 for the public. |
| POST   | `/api/announcements`               | Admin or Editor | Creates the item and derives a unique slug from the title. |
| PUT    | `/api/announcements/{id}`          | Admin or Editor | `publishedAt` is stamped the first time an item goes live. |
| DELETE | `/api/announcements/{id}`          | Admin           | |

Listing responses are paged: `{ items, page, pageSize, totalCount, totalPages }`.

## Programs

| Method | Route                   | Auth            | Notes |
| ------ | ----------------------- | --------------- | ----- |
| GET    | `/api/programs`         | —               | Query: `includeInactive` (CMS users only). Ordered by `displayOrder`. |
| GET    | `/api/programs/{slug}`  | —               | |
| POST   | `/api/programs`         | Admin or Editor | `409 Conflict` when the code is already taken. |
| PUT    | `/api/programs/{id}`    | Admin or Editor | |
| DELETE | `/api/programs/{id}`    | Admin           | |

## Content pages

| Method | Route                | Auth            | Notes |
| ------ | -------------------- | --------------- | ----- |
| GET    | `/api/pages`         | —               | Query: `includeDrafts` (CMS users only). |
| GET    | `/api/pages/{slug}`  | —               | e.g. `about`, `admission`, `contact`. |
| POST   | `/api/pages`         | Admin or Editor | |
| PUT    | `/api/pages/{id}`    | Admin or Editor | |
| DELETE | `/api/pages/{id}`    | Admin           | |

## Chatbot

| Method | Route                        | Auth            | Notes |
| ------ | ---------------------------- | --------------- | ----- |
| POST   | `/api/chatbot/ask`           | —               | Body `{ message, sessionId? }` → `{ sessionId, reply, matchedFaqId, suggestions }`. Pass the returned `sessionId` back to keep the conversation grouped. |
| GET    | `/api/chatbot/faqs`          | —               | Query: `includeInactive` (CMS users only). |
| POST   | `/api/chatbot/faqs`          | Admin or Editor | |
| PUT    | `/api/chatbot/faqs/{id}`     | Admin or Editor | |
| DELETE | `/api/chatbot/faqs/{id}`     | Admin           | |

### How the matcher works

The visitor's message is lower-cased, split into words and stripped of stop words. Each active FAQ scores the
share of those words it covers — a hit on the curated `keywords` list counts 1.0, a hit on the question text
0.6. The highest scoring entry wins if it covers at least 34% of the message; otherwise the assistant returns
the fallback reply together with three suggested questions. Add every phrasing a student might use to
`keywords` to improve recall.

## Health

`GET /api/health` returns `200` with `{ status: "healthy", database: "up" }`, or `503` when the database cannot
be reached. Useful as a container or load balancer probe.
