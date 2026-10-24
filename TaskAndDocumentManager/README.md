# Task and Document Manager

An ASP.NET Core API for workspace-scoped task and document workflows. It includes authentication, team membership, document sharing, notifications, audit queries, and real-time updates.

> **Development status:** This project is under active development and is not production-ready. Several repositories still use in-memory storage, so their data is lost when the process restarts. See [Persistence status](#persistence-status).

## Features

- User registration, login, JWT authentication, role-based access, and password reset
- Workspace-aware tasks, teams, and membership operations
- Document upload, metadata, sharing, access revocation, download, and task linking
- Notifications, audit queries, and global search
- SignalR notification and presence hubs
- Rate limiting, request metrics, security headers, and health checks
- Swagger/OpenAPI documentation in the Development environment

## Technology

- ASP.NET Core and .NET SDK version pinned in [`global.json`](global.json)
- Entity Framework Core with PostgreSQL through Npgsql
- JWT bearer authentication
- xUnit tests

## Repository layout

```text
Application/       Use cases, interfaces, DTOs, and tests
Domain/            Domain entities and business rules
Infrastructure/    Persistence, repositories, storage, and integrations
src/Api/           ASP.NET Core API, controllers, middleware, and hubs
docs/              Development and testing documentation
```

## Prerequisites

- The .NET SDK version specified in `global.json` (currently .NET 10.0.100-rc.2)
- PostgreSQL for the EF Core-backed features
- OpenSSL, if you want to generate a local JWT signing key from the command line

## Local setup

Clone the repository, change to its root directory, and restore dependencies:

```bash
dotnet restore
```

Create a local settings file from the safe template:

```bash
cp appsettings.example.json appsettings.json
```

Configure the database connection and JWT signing key with .NET User Secrets. Replace the database connection string with credentials for your local PostgreSQL instance.

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=taskdocs;Username=<your-user>;Password=<your-password>"
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
```

The API also accepts the legacy `ConnectionStrings:DefaultConnection` setting. SMTP settings are optional; configure `Email:Smtp:Host`, `Email:Smtp:Port`, `Email:Smtp:Username`, and `Email:Smtp:Password` through User Secrets or environment variables when using a mail server.

Build and run the API:

```bash
dotnet build TaskAndDocumentManager.csproj
dotnet run --project TaskAndDocumentManager.csproj
```

The checked-in launch profiles use `http://localhost:5121` and `https://localhost:7216`. In Development, Swagger UI is available at `/swagger`.

## API areas

Version 1 routes use the `/api/v1` prefix:

| Area | Route |
| --- | --- |
| Authentication and user administration | `/api/v1/auth` |
| Tasks | `/api/v1/tasks` |
| Documents | `/api/v1/documents` |
| Teams | `/api/v1/teams` |
| Notifications | `/api/v1/notifications` |
| Search | `/api/v1/search` |
| Audit logs | `/api/v1/audit-logs` |

Most routes require a bearer token. Authentication, user-management, and other endpoint-specific policies determine which operations are public or restricted by role.

## Health checks

- `GET /health/live` reports whether the API process is running.
- `GET /health` and `GET /health/ready` report database and file-storage health.

## Persistence status

PostgreSQL and EF Core are configured, but persistence is not implemented uniformly across the application.

| Data | Current storage |
| --- | --- |
| Tasks | PostgreSQL through EF Core |
| Workspaces | PostgreSQL through EF Core |
| Password-reset tokens | PostgreSQL through EF Core |
| Users, teams, workspace memberships, documents, document access, notifications, and audit logs | In-memory repositories |
| Uploaded file contents | Local filesystem under the application output directory's `storage/uploads` folder |

In-memory data is cleared when the application process restarts. Local file storage is not a substitute for durable, shared production storage. Plan a persistence and storage strategy before deploying or running multiple API instances.

## Tests

Run the xUnit suite with:

```bash
dotnet test Application/Tests/Tests.csproj
```

The repository's GitHub Actions workflow restores, builds, tests, and publishes the API on pushes and pull requests to `main` or `master`.

## Configuration and secrets

`appsettings.example.json` is a template. Local `appsettings*.json` files, User Secrets, and credentials should not be committed. Use a managed secret store or environment variables for deployed environments.

## Further documentation

- [Testing strategy](docs/testing-strategy.md)
- [CI workflow](.github/workflows/dotnet-ci.yml)
