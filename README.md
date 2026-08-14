# Formarca

.NET 10 foundation for the Formarca multi-tenant client panel (Identity + JWT, tenancy, groups/permissions). Business modules from the PDF are not implemented yet.

## Solution structure

```
Formarca.sln
src/
  Formarca.Domain/
  Formarca.Application/
  Formarca.Infrastructure/
  Formarca.Web/                 # Controllers API (http://localhost:5000)
  Formarca.BackgroundRunner/
  Formarca.DbMigrator/
    scripts/
      add-migration.ps1
      migrations/               # SQL scripts for each EF migration
frontend/
  formarca-web/                 # React + Vite + Ant Design (http://localhost:5173)
tests/
  Formarca.Domain.Tests/
  Formarca.Application.Tests/
  Formarca.Architecture.Tests/
```

## Prerequisites

- .NET 10 SDK
- Node.js 20+ (frontend)
- Docker (for SQL Server) or a local SQL Server instance
- `dotnet tool install -g dotnet-ef` (for migrations)

## Full stack (local)

1. Start SQL Server: `docker compose up -d`
2. Migrate + seed: `dotnet run --project src/Formarca.DbMigrator`
3. API: `dotnet run --project src/Formarca.Web` → `http://localhost:5000`
4. (Optional) Background jobs: `dotnet run --project src/Formarca.BackgroundRunner`
5. Frontend:
   ```powershell
   cd frontend/formarca-web
   npm install
   copy .env.example .env   # if needed; default VITE_API_URL=http://localhost:5000
   npm run dev
   ```
   Opens at `http://localhost:5173`.

Seed login:

| User | Password | Role |
|------|----------|------|
| `admin@formarca.com` | `Formarca@Admin123!` | PlatformAdmin |

Also seeds permission catalog, demo tenant (`demo`), and default groups (`TenantAdmins`, `ClientUsers`).

## SQL Server (Docker)

```powershell
docker compose up -d
```

Default connection string (already in `appsettings.json`):

`Server=localhost,1433;Database=Formarca;User Id=sa;Password=Your_password123;TrustServerCertificate=True;`

## Migrate + seed

```powershell
dotnet run --project src/Formarca.DbMigrator
```

## Run API

```powershell
dotnet run --project src/Formarca.Web
```

Listens on **http://localhost:5000** (see `launchSettings.json`).

Useful endpoints:

- `GET /api/health`
- `GET /api/version`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/switch-tenant`
- `GET /api/auth/me`
- `GET/POST /api/admin/tenants` (PlatformAdmin)
- `GET/POST /api/admin/users` (PlatformAdmin)
- `GET/POST /api/admin/groups` (PlatformAdmin)

OpenAPI (Development): `/openapi/v1.json`

## Frontend

```powershell
cd frontend/formarca-web
npm install
npm run dev
```

| Variable | Default | Notes |
|----------|---------|--------|
| `VITE_API_URL` | `http://localhost:5000` | API base URL, no trailing slash |
| `VITE_MOCK_AUTH` | `true` | Login local sem API; `false` usa `/api/auth` |

Dev server: **http://localhost:5173**. The API allows CORS from that origin in Development.

Login mockado (sem API, padrão ligado):

| User | Password | Role |
|------|----------|------|
| `admin@formarca.com` | `admin` | PlatformAdmin |

Para autenticar na API real, defina `VITE_MOCK_AUTH=false` no `.env` do frontend.

## Background runner

```powershell
dotnet run --project src/Formarca.BackgroundRunner
```

Includes a sample job that cleans expired refresh tokens.

## Add a migration (+ SQL export)

```powershell
./src/Formarca.DbMigrator/scripts/add-migration.ps1 MyMigrationName
```

This creates the EF migration under `Formarca.Infrastructure/Persistence/Migrations` and an idempotent `.sql` under `src/Formarca.DbMigrator/scripts/migrations/`.

DbMigrator applies schema via `Database.Migrate()`; the `.sql` files are for human review.

## Build / tests

```powershell
dotnet build Formarca.sln
dotnet test Formarca.sln
cd frontend/formarca-web && npm run build
```
