# TaskAndDocumentManager

TaskAndDocumentManager is a versioned ASP.NET Core API for task management, secure document handling, workspace-based collaboration, notifications, realtime updates, audit logging, search, and background processing.

The project is API-first. A small MVC/JavaScript page is included only as a lightweight local client for login, notifications, and SignalR checks.

## Current Status

Latest verified state:

- Target framework: `.NET 10` preview
- API base path: `/api/v1`
- Realtime hubs: `/hubs/notifications` and `/hubs/realtime`
- Test suite: `145/145` passing
- Main remaining milestone: replace remaining in-memory repositories with database-backed persistence

## Implemented Architecture

| Area | Status |
|---|---|
| Authentication | Implemented with JWT login/register flow |
| Authorization | Implemented with ASP.NET Core policies |
| System RBAC | Implemented with `Admin`, `Manager`, `User` |
| Workspace roles | Implemented with `Owner`, `Admin`, `Manager`, `Member` |
| Ownership | Implemented on tasks and documents through `OwnerId` |
| Documents | Upload, metadata, download, delete, link-to-task, share, revoke |
| Sharing | Explicit `DocumentAccess` model with workspace scope |
| Notifications | DB-backed via EF repository, unread/read support, realtime dispatch |
| Audit logs | Structured audit actions and workspace-aware query model |
| SignalR | Notification and realtime/presence hubs wired at runtime |
| Background jobs | Hosted service wired with task reminders and orphan-file cleanup |
| Search | Task search, document search, audit search, global search |
| Workspaces | Workspace entity, membership, roles, request workspace context |
| Teams | Team entity, team membership, create/list/add/remove endpoints |
| Tenant isolation | Workspace-scoped requests, query filters, and workspace-aware use cases |
| API versioning | URL versioning via `/api/v1` route constants |
| Performance guardrails | Paginated list responses, DTO returns, repository-level filtering |
| Caching | Built-in memory cache for stable reference data |
| Resilience | Safe failure responses, internal exception logging, storage/realtime timeouts |

## Performance Guardrails

The project now treats list endpoints as query operations, not "load everything" operations.

- Tasks, documents, notifications, and audit logs use paginated responses.
- Use cases return DTOs/results instead of exposing full domain entities to API clients.
- Repositories apply filtering, sorting, ownership, workspace scope, `Skip`, and `Take` before materializing results.
- Query objects are used where filters can grow over time, such as `TaskQuery`, `DocumentQuery`, `AuditQuery`, and `NotificationQuery`.

This keeps the current implementation simple while avoiding common scaling issues like unbounded reads, in-memory filtering, and accidental N+1-style access patterns.

## Caching

The project uses ASP.NET Core's built-in memory cache for stable reference data:

- built-in system role catalog
- allowed document upload types

The project intentionally does not cache volatile user data yet:

- current notifications
- user tasks
- audit logs

If the app later runs across multiple servers, this cache should move to a distributed cache such as Redis.

## Resilience

The project now expects common infrastructure failures and avoids exposing technical details to clients.

- File storage operations have configurable timeouts through `FileStorage:OperationTimeout`.
- Realtime notification dispatch has a configurable timeout through `RealtimeDispatch:OperationTimeout`.
- Upload failures return a safe message: `The document could not be uploaded. Please try again.`
- Technical details are logged internally instead of being returned in API responses.
- Partial uploaded files are cleaned up when storage fails or times out.
- Metadata save failures still trigger compensating cleanup of the already-saved file.
- A global API exception middleware returns a generic failure response for unexpected errors.

The app does not add blind retries around unsafe operations like creating tasks or uploading files, because retrying those without idempotency can create duplicates.

## Tech Stack

- ASP.NET Core
- EF Core with Npgsql
- PostgreSQL
- JWT bearer authentication
- SignalR
- Memory cache
- Hosted services for background jobs
- xUnit and Moq

## Project Layout

```text
Domain/
  Auth/
  Documents/
  Entities/
  Tasks/
  Workspaces/

Application/
  Audit/
  Auth/
  BackgroundJobs/
  Common/
  Documents/
  Notifications/
  Presence/
  Search/
  Tasks/
  Workspaces/

Infrastructure/
  Audit/
  Auth/
  Documents/
  Notifications/
  Persistence/
  Storage/
  Tasks/
  Workspaces/

src/Api/
  Authorization/
  BackgroundJobs/
  Controllers/
  Hubs/
  Realtime/
  Routing/

wwwroot/
Views/
Application/Tests/
```

## Configuration

The app expects a PostgreSQL connection string and JWT settings.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=taskanddocumentmanager;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_A_LONG_RANDOM_SECRET_KEY_FOR_PRODUCTION_123456789",
    "Issuer": "TaskAndDocumentManager",
    "Audience": "TaskAndDocumentManager.Client",
    "ExpiresMinutes": 60
  },
  "BackgroundJobs": {
    "Enabled": true,
    "RunOnStartup": true,
    "InitialDelay": "00:00:30",
    "Interval": "01:00:00"
  }
}
```

## Running Locally

```bash
dotnet restore
dotnet build TaskAndDocumentManager.sln
dotnet test Application/Tests/Tests.csproj
dotnet run
```

Swagger is enabled in development.

## API Versioning

All REST endpoints are versioned under:

```text
/api/v1
```

Routes are centralized in:

```text
src/Api/Routing/ApiRoutes.cs
```

SignalR hubs are not versioned through the REST route prefix. They remain:

```text
/hubs/notifications
/hubs/realtime
```

## Authentication Flow

1. Register a user.
2. Log in to receive a JWT.
3. Send the JWT on protected requests.

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password1"
}
```

Use the token like this:

```http
Authorization: Bearer <jwt>
```

For SignalR browser clients, the JWT can be sent as `access_token` in the hub query string.

## REST Endpoints

### Auth

| Method | Route | Access |
|---|---|---|
| `POST` | `/api/v1/auth/register` | anonymous |
| `POST` | `/api/v1/auth/login` | anonymous |
| `GET` | `/api/v1/auth/me` | authenticated |
| `GET` | `/api/v1/auth/users` | manager/admin |
| `POST` | `/api/v1/auth/users` | admin |
| `PUT` | `/api/v1/auth/users/{id}/deactivate` | admin |
| `PUT` | `/api/v1/auth/users/{id}/role` | admin |
| `DELETE` | `/api/v1/auth/users/{id}` | admin |

### Tasks

| Method | Route | Access |
|---|---|---|
| `POST` | `/api/v1/tasks` | authenticated |
| `GET` | `/api/v1/tasks` | authenticated, scoped |
| `GET` | `/api/v1/tasks/{id}` | authenticated, scoped |
| `PUT` | `/api/v1/tasks/{id}` | owner/admin pattern |
| `DELETE` | `/api/v1/tasks/{id}` | owner/admin pattern |
| `POST` | `/api/v1/tasks/{id}/assign` | manager/admin |

Task listing supports pagination, search, status, priority, due-date filters, owner/assignee filters, and sorting.

### Documents

| Method | Route | Access |
|---|---|---|
| `POST` | `/api/v1/documents` | authenticated |
| `GET` | `/api/v1/documents` | authenticated, scoped |
| `GET` | `/api/v1/documents/shared-with-me` | authenticated, scoped |
| `GET` | `/api/v1/documents/{id}` | owner/admin/shared |
| `GET` | `/api/v1/documents/{id}/download` | owner/admin/shared |
| `POST` | `/api/v1/documents/{id}/link-task` | owner |
| `POST` | `/api/v1/documents/{id}/share` | owner/admin |
| `POST` | `/api/v1/documents/{id}/tasks/{taskId}/share` | owner/admin/task participant rules |
| `DELETE` | `/api/v1/documents/{id}/share/{userId}` | owner/admin |
| `DELETE` | `/api/v1/documents/{id}` | owner/admin pattern |

Upload validation currently allows:

- `.pdf`
- `.png`
- `.jpg`
- `.jpeg`
- `.docx`

The max upload size is `20 MB`. The API validates both extension and content type. Original filenames are stored as metadata only; stored disk filenames are generated safely.

### Teams

| Method | Route | Access |
|---|---|---|
| `POST` | `/api/v1/teams` | workspace owner/admin/manager |
| `GET` | `/api/v1/teams` | workspace member |
| `POST` | `/api/v1/teams/{teamId}/members` | workspace owner/admin/manager |
| `DELETE` | `/api/v1/teams/{teamId}/members/{userId}` | workspace owner/admin/manager |

Team operations are scoped to the current workspace from the JWT.

### Notifications

| Method | Route | Access |
|---|---|---|
| `GET` | `/api/v1/notifications?pageNumber=1&pageSize=20` | authenticated |
| `PATCH` | `/api/v1/notifications/{id}/read` | notification owner |

Notifications are workspace-aware, paginated, and are also pushed through SignalR.

### Search

| Method | Route | Access |
|---|---|---|
| `GET` | `/api/v1/search` | authenticated |

Global search currently searches tasks and documents. Results are scoped by workspace and resource access.

### Audit Logs

| Method | Route | Access |
|---|---|---|
| `GET` | `/api/v1/audit-logs` | admin |

Audit search supports user, action, date range, pagination, and workspace scoping.

## Realtime

Two SignalR hubs are wired:

| Hub | Purpose |
|---|---|
| `/hubs/notifications` | private notification delivery |
| `/hubs/realtime` | presence and task/document collaboration events |

Realtime event constants currently include:

- `NotificationCreated`
- `DocumentShared`
- `DocumentDeleted`
- `TaskAssigned`
- `TaskCompleted`
- `UserPresenceUpdated`

Events actively sent today include notification creation and presence updates. Some task/document event constants are present as the intended event vocabulary for future dispatchers.

The client should still refresh from REST APIs after reconnects or important realtime events. Realtime is a delivery channel, not the source of truth.

## Background Jobs

The app uses a hosted service:

```text
src/Api/BackgroundJobs/ScheduledBackgroundJobService.cs
```

Registered jobs:

- `SendTaskDeadlineReminders`
- `CleanupOrphanedDocumentFiles`

The job runner is configurable through `BackgroundJobs` settings.

## Workspace And Tenant Isolation

The JWT contains the current workspace ID. API requests resolve:

- current user
- current system role
- current workspace

The DbContext has a `CurrentWorkspaceId` that is set per authenticated request. Workspace-scoped EF entities use query filters, and application use cases also pass workspace IDs explicitly for authorization-sensitive operations.

Implemented workspace concepts:

- `Workspace`
- `WorkspaceMember`
- `WorkspaceRoles`
- `Team`
- `TeamMember`

System roles and workspace roles are intentionally separate.

## Persistence Status

This is the most important current limitation.

Durable through EF/PostgreSQL at runtime:

- tasks
- notifications
- built-in role seed/configuration

Configured in EF but not yet used by runtime repositories:

- users
- workspaces
- workspace members
- teams
- team members

Still in-memory at runtime:

- users
- document metadata
- document access grants
- audit logs
- workspaces
- workspace memberships
- teams
- team memberships
- realtime connection tracking
- presence state

Filesystem-backed:

- uploaded file bytes

Important implication:

- uploaded files may remain on disk after app restart
- document metadata and shares reset on app restart
- users reset on app restart
- audit logs reset on app restart
- workspace/team runtime state resets on app restart

## Security Model

Current security rules include:

- JWT authentication on protected routes
- policy-based authorization for admin/manager actions
- backend-only ownership checks
- workspace-scoped access checks
- secure document download through API streaming
- no public document file URLs
- file size and type validation in the upload use case
- workspace-aware sharing and notifications
- audit logging after successful critical actions

## Tests

The test suite covers:

- authentication and password behavior
- role changes
- task creation, listing, updating, deletion, and reminders
- document upload validation, download authorization, deletion, sharing, revocation, and metadata
- search and pagination
- notifications
- audit logs
- SignalR helper behavior
- presence tracking
- workspace/team domain behavior
- team use cases
- API route versioning
- memory-cached reference catalogs
- resilience middleware and storage timeout behavior

Run:

```bash
dotnet test Application/Tests/Tests.csproj
```

Latest verified result:

```text
145 passed
```

## Minimal Frontend Shell

The repo includes a small frontend shell under `wwwroot/js/site.js` and MVC views. It can:

- log in
- save a JWT locally
- load the current user profile
- list the first page of notifications
- connect to SignalR hubs
- reconcile realtime events with REST API refreshes

It is a test harness, not a full product UI.

## What Is Left

The main remaining engineering milestone is database-backed persistence for the repositories that still use static in-memory lists.

Recommended next work:

1. Replace `UserRepository` with EF persistence.
2. Add EF persistence for documents and document access grants.
3. Add EF persistence for audit logs.
4. Replace in-memory workspace/team repositories with EF-backed repositories.
5. Add committed EF Core migrations.
6. Move uploaded file storage to object storage for production.
7. Make SignalR presence/connection tracking distributed for multi-instance deployments.

Once those are complete, the architecture will be much closer to a production-ready multi-tenant platform.
