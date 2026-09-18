# BCAS Web portal (frontend)

React 19 + TypeScript single page application built with Vite, consuming the ASP.NET Core API in `../backend`.

```bash
npm install
cp .env.example .env.local   # VITE_API_BASE_URL
npm run dev                  # http://localhost:5173
npm run lint                 # oxlint
npm run build                # type check + production bundle in dist/
```

## Structure

```
src/api/         Typed fetch client, DTO types and endpoint wrappers
src/auth/        Auth context, provider and useAuth hook
src/components/  Layout, chat widget, route guard, shared status views
src/hooks/       useAsync - cancellable data loading
src/pages/       Public pages; pages/admin holds the CMS screens
src/utils/       Formatting helpers
```

The access token is kept in memory and the refresh token in `localStorage`; the API client renews an expired
token once and replays the request, so a reload keeps the user signed in.
