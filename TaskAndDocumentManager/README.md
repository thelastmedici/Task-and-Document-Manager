# TaskAndDocumentManager

TaskAndDocumentManager is a .NET 10 application for role-based task management, secure document handling, realtime notifications, and basic collaboration presence.

The codebase is organized as a layered application:

- `src/Api`
  HTTP endpoints, auth wiring, SignalR hubs, hosted services, and the minimal MVC shell
- `Application`
  use cases, DTOs, interfaces, background jobs, and application-level authorization decisions
- `Domain`
  domain entities and invariants
- `Infrastructure`
  repositories, EF Core persistence, storage, hashing, and integration implementations

This is not a greenfield skeleton. It already contains a meaningful feature set, automated tests, and a working local development story. It is also intentionally mixed in its persistence model, so some parts are production-oriented and some remain demo-grade. That trade-off is important to understand before extending the system.

## What The Application Does

The current implementation supports:

- JWT-based authentication and role-aware authorization
- user registration and login
- admin-driven user management
- ownership-based task CRUD with scoped queries
- secure document upload, download, linking, sharing, revocation, and deletion
- notifications persisted to PostgreSQL and delivered over SignalR
- realtime presence updates for online users and active document editing state
- global search across task and document datasets
- audit logging for sensitive operations
- scheduled background jobs for orphaned file cleanup and task deadline reminders
- a minimal MVC/JavaScript dashboard that demonstrates API-backed realtime behavior

## Current State

The project is functionally useful for local development and demos. The most important caveat is that persistence is intentionally uneven:

- tasks are persisted in PostgreSQL through EF Core
- notifications are persisted in PostgreSQL through EF Core
- roles are modeled and seeded through EF Core
- users are currently stored in-memory
- document metadata is currently stored in-memory
- document access grants are currently stored in-memory
- audit logs are currently stored in-memory
- document file bytes are stored on local disk under `storage/uploads`
- presence state and connection tracking are in-memory

This means the behavior is solid enough to build against, but not all data survives an application restart.

## Architecture

### API Layer

`src/Api` owns:

- controller endpoints under `api/*`
- JWT bearer configuration
- SignalR hubs
- authorization policies
- the hosted background job scheduler
- the lightweight MVC shell under `Views/` and `wwwroot/`

### Application Layer

`Application` owns:

- use-case orchestration
- pagination and query DTOs
- interfaces for repositories and integrations
- business-level authorization decisions
- background job contracts and implementations

This layer is where most cross-cutting rules live, for example:

- who can see which tasks
- who can access which documents
- how search results are scoped
- when task reminders are created

### Domain Layer

`Domain` owns entity invariants.

Examples:

- `TaskItem` requires an `OwnerId`
- task title and description length limits are enforced in the entity
- completed tasks cannot be modified
- document ownership is explicit via `OwnerId`

### Infrastructure Layer

`Infrastructure` owns concrete implementations:

- EF Core repositories for tasks and notifications
- in-memory repositories for users, documents, access grants, and audit logs
- local filesystem storage
- password hashing
- JWT generation
- seeded role catalog

## Security Model

### Authentication

The API uses JWT bearer authentication.

Expected configuration keys:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`

The bearer middleware also accepts hub access tokens via the `access_token` query parameter for `/hubs/*`.

### Password Storage

Passwords are not stored in plaintext.

The current hasher uses ASP.NET Core Identity V3 format with a configured iteration count. Legacy custom PBKDF2 hashes are still accepted and transparently upgraded on successful login.

### Roles

The system has three built-in roles:

- `Admin`
- `Manager`
- `User`

Role IDs and names are seeded from `Infrastructure/Persistence/BuiltInRoles.cs`.

### Ownership And Resource Access

The project is moving toward explicit resource ownership rather than implicit role-based access.

Current examples:

- tasks have an `OwnerId`
- documents have an `OwnerId`
- document download is only available through `GET /api/documents/{id}/download`
- task and document access is validated per request rather than assumed from role alone

In practice:

- `Admin` can access all tasks and documents
- `Manager` has broader scope than `User`, including team-oriented document access
- `User` is scoped to owned tasks and owned/shared documents

## Feature Summary

### Auth And User Management

Implemented endpoints:

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/auth/users`
- `POST /api/auth/users`
- `PUT /api/auth/users/{id}/deactivate`
- `PUT /api/auth/users/{id}/role`
- `DELETE /api/auth/users/{id}`

Behavior notes:

- user creation through self-registration defaults to the `User` role
- admins can create users with an explicit role
- managers can list users, but cannot create, delete, or change roles
- login returns a JWT plus the current user payload

### Task Management

Implemented endpoints:

- `POST /api/tasks`
- `GET /api/tasks`
- `GET /api/tasks/{id}`
- `PUT /api/tasks/{id}`
- `DELETE /api/tasks/{id}`
- `POST /api/tasks/{id}/assign`

Current task model includes:

- `OwnerId`
- optional assignment
- completion state
- due date
- priority
- deadline reminder tracking

Task querying supports:

- pagination
- free-text search
- completion filters
- owner filters
- assignee filters
- status filters
- priority filters
- due date range filters
- sort field and direction

Authorization behavior:

- `Admin` can view all tasks
- `Manager` sees owned tasks and assigned tasks
- `User` sees owned tasks only
- update and delete are ownership-driven
- assignment is restricted to `Manager` and `Admin`

### Document Management

Implemented endpoints:

- `POST /api/documents`
- `GET /api/documents`
- `GET /api/documents/shared-with-me`
- `GET /api/documents/{id}`
- `GET /api/documents/{id}/download`
- `POST /api/documents/{id}/link-task`
- `POST /api/documents/{id}/share`
- `POST /api/documents/{id}/tasks/{taskId}/share`
- `DELETE /api/documents/{id}/share/{userId}`
- `DELETE /api/documents/{id}`

Document security model:

- files are never served from `wwwroot`
- file bytes live on local disk outside the public web root
- all file reads go through the API
- access checks happen before metadata or file bytes are returned

Upload protections:

- request size limit: 20 MB
- extension and content-type allowlist
- allowed file types currently include:
  - `.pdf`
  - `.png`
  - `.jpg`
  - `.jpeg`
  - `.docx`

Document access behavior:

- `Admin` can access all documents
- `User` can access owned documents and explicitly shared documents
- `Manager` can also access broader team/task-related documents where the application rules allow it
- sharing and revocation are enforced through the application layer, not by direct file URL exposure

### Notifications

Implemented endpoints:

- `GET /api/notifications`
- `PATCH /api/notifications/{id}/read`

Notifications are persisted in PostgreSQL and dispatched in realtime using SignalR.

Current notification sources include:

- task deadline reminder jobs
- document-related actions where the use case dispatches notifications

### Realtime And Presence

SignalR hubs are mapped at:

- `/hubs/notifications`
- `/hubs/realtime`

Currently implemented realtime behavior:

- notification delivery
- online/offline presence updates
- connection tracking per user
- task group join/leave
- document editing presence state

Presence currently tracks:

- whether a user is online
- `ConnectedAtUtc`
- `DisconnectedAtUtc`
- the current document being edited, if any

Current presence implementation is in-memory, so it is process-local and non-durable.

### Search

Implemented endpoint:

- `GET /api/search`

This performs a scoped search across tasks and documents using the caller's authorization context.

### Audit Logs

Implemented endpoint:

- `GET /api/audit-logs`

This endpoint is admin-only.

Audit logs are currently stored in-memory. They are useful for behavior verification and tests, but they are not yet durable.

### Background Jobs

The app includes a hosted scheduler that runs registered `IBackgroundJob` implementations.

Current jobs:

- `CleanupOrphanedDocumentFiles`
  removes on-disk files that no longer have matching in-memory document metadata
- `SendTaskDeadlineReminders`
  creates notifications for tasks due within the reminder window and dispatches them over SignalR

Background jobs are controlled through the `BackgroundJobs` configuration section.

## Frontend

The project now includes a minimal MVC shell instead of being API-only.

The home page is a small realtime workspace that demonstrates:

- login and token persistence
- profile refresh from `/api/auth/me`
- notifications from `/api/notifications`
- SignalR subscription to notification and presence hubs
- API reconciliation after reconnects and important events

This frontend is intentionally light. It is a proof of integration, not a full product UI.

## Persistence Model

The table below is the practical persistence picture today.

| Area | Implementation | Durability |
|---|---|---|
| Tasks | EF Core + PostgreSQL | durable |
| Notifications | EF Core + PostgreSQL | durable |
| Roles | EF Core seed data | durable |
| Users | in-memory repository | lost on restart |
| Documents metadata | in-memory repository | lost on restart |
| Document access grants | in-memory repository | lost on restart |
| Audit logs | in-memory repository | lost on restart |
| Presence | in-memory service | lost on restart |
| Uploaded file bytes | local filesystem | durable on disk, but can drift from metadata |

## Project Layout

```text
Application/
  Auth/
  Audit/
  BackgroundJobs/
  Common/
  Documents/
  Notifications/
  Presence/
  Search/
  Tasks/
  Tests/

Domain/
  Auth/
  Documents/
  Entities/
  Tasks/

Infrastructure/
  Audit/
  Auth/
  Documents/
  Notifications/
  Persistence/
  Storage/
  Tasks/

src/Api/
  Authorization/
  BackgroundJobs/
  Controllers/
  Hubs/
  Realtime/

Views/
wwwroot/
```

## Running Locally

### Requirements

- .NET SDK 10 preview
- PostgreSQL
- valid JWT configuration

### Configuration

At minimum, configure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;Username=...;Password=..."
  },
  "Jwt": {
    "Key": "your-signing-key",
    "Issuer": "TaskAndDocumentManager",
    "Audience": "TaskAndDocumentManager.Client"
  },
  "BackgroundJobs": {
    "Enabled": true,
    "RunOnStartup": true,
    "InitialDelay": "00:00:30",
    "Interval": "01:00:00"
  }
}
```

### Start The Application

```bash
dotnet run
```

The application hosts:

- MVC pages
- API controllers
- Swagger in development
- SignalR hubs

### Build

```bash
dotnet build TaskAndDocumentManager.sln
```

### Test

```bash
dotnet test Application/Tests/Tests.csproj
```

Latest verified result:

- `93` tests passed

## Known Limitations

The following are the most important limitations in the current codebase:

- users are not persisted to PostgreSQL yet
- document metadata and access grants are not persisted yet
- audit logs are not persisted yet
- presence is process-local and in-memory
- uploaded files are durable on disk, but their metadata is not durable
- there are no committed EF Core migrations in the repository
- the MVC frontend is intentionally minimal and not a complete product UI
- local filesystem storage is suitable for development, not for multi-instance production deployment

## Recommended Next Steps

If this codebase is going to move beyond local development, these are the highest-value improvements:

1. move users, documents, document access grants, and audit logs into durable database persistence
2. add and commit EF Core migrations
3. replace local filesystem storage with object storage
4. make presence distributable if the app will ever run on multiple instances
5. add API integration tests around authorization boundaries and realtime flows
6. formalize configuration and environment setup for deployment

## Repository Notes

The project currently mixes two usage modes:

- backend/API development
- a thin MVC shell used as a realtime integration surface

That is a reasonable place to be for an evolving internal application, but if the frontend grows materially, it would be worth deciding whether to keep it server-rendered or split it into a dedicated client application.
