# TaskAndDocumentManager API

TaskAndDocumentManager exposes a JWT-protected API for:

- user registration and login
- role-based user administration
- task ownership, assignment, and scoped querying
- secure document upload, download, sharing, and revocation
- notifications and realtime updates
- global search across tasks and documents

This README is written for API consumers: frontend developers, integrators, and anyone building against the HTTP and SignalR surface.

## Quick Start

### Requirements

- .NET SDK 10 preview
- PostgreSQL
- valid JWT configuration

### Required Configuration

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

### Run Locally

```bash
dotnet run
```

### Verify The Build

```bash
dotnet build TaskAndDocumentManager.sln
dotnet test Application/Tests/Tests.csproj
```

Latest verified test run: `132/132` passing.

## Authentication

The API uses JWT bearer authentication.

### Login Flow

1. Register with `POST /api/v1/auth/register` or use an existing account.
2. Authenticate with `POST /api/v1/auth/login`.
3. Store the returned token.
4. Send the token on protected requests:

```http
Authorization: Bearer <jwt>
```

### Example Login Request

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "Password1"
}
```

### Example Login Response

```json
{
  "token": "<jwt>",
  "expiresAtUtc": "2026-06-17T12:34:56Z",
  "user": {
    "id": "11111111-1111-1111-1111-111111111111",
    "email": "user@example.com",
    "role": "User",
    "isActive": true
  }
}
```

### Password Storage

Passwords are hashed, not stored in plaintext.

The current implementation uses ASP.NET Core Identity V3 password hashing. Older legacy hashes are still accepted and upgraded transparently on successful login.

## Roles And Access Model

The system defines three roles:

- `Admin`
- `Manager`
- `User`

### Practical Access Rules

- `Admin`
  can access all tasks and all documents, and can manage users
- `Manager`
  has broader scope than a normal user, including manager-level task access and broader team document access where the application rules allow it
- `User`
  is limited to owned tasks and owned/shared documents

### Ownership Rules

Ownership is explicit and enforced per request:

- tasks use `OwnerId`
- documents use `OwnerId`
- downloads are only served through `GET /api/v1/documents/{id}/download`
- access is validated in the backend before metadata or file bytes are returned

## API Overview

All versioned REST API endpoints are rooted under `/api/v1`.

### Auth

| Method | Route | Notes |
|---|---|---|
| `POST` | `/api/v1/auth/register` | self-service registration |
| `POST` | `/api/v1/auth/login` | returns JWT and user payload |
| `GET` | `/api/v1/auth/me` | current user profile |
| `GET` | `/api/v1/auth/users` | manager/admin |
| `POST` | `/api/v1/auth/users` | admin only |
| `PUT` | `/api/v1/auth/users/{id}/deactivate` | admin only |
| `PUT` | `/api/v1/auth/users/{id}/role` | admin only |
| `DELETE` | `/api/v1/auth/users/{id}` | admin only |

### Tasks

| Method | Route | Notes |
|---|---|---|
| `POST` | `/api/v1/tasks` | create task |
| `GET` | `/api/v1/tasks` | paginated, filtered, scoped to caller |
| `GET` | `/api/v1/tasks/{id}` | task detail with access check |
| `PUT` | `/api/v1/tasks/{id}` | owner/admin |
| `DELETE` | `/api/v1/tasks/{id}` | owner/admin |
| `POST` | `/api/v1/tasks/{id}/assign` | manager/admin |

### Documents

| Method | Route | Notes |
|---|---|---|
| `POST` | `/api/v1/documents` | multipart upload |
| `GET` | `/api/v1/documents` | paginated, scoped document list |
| `GET` | `/api/v1/documents/shared-with-me` | explicitly shared documents |
| `GET` | `/api/v1/documents/{id}` | metadata with access check |
| `GET` | `/api/v1/documents/{id}/download` | file download with access check |
| `POST` | `/api/v1/documents/{id}/link-task` | link document to task |
| `POST` | `/api/v1/documents/{id}/share` | direct share |
| `POST` | `/api/v1/documents/{id}/tasks/{taskId}/share` | share task-linked document |
| `DELETE` | `/api/v1/documents/{id}/share/{userId}` | revoke share |
| `DELETE` | `/api/v1/documents/{id}` | delete document |

### Notifications

| Method | Route | Notes |
|---|---|---|
| `GET` | `/api/v1/notifications` | list current user's notifications |
| `PATCH` | `/api/v1/notifications/{id}/read` | mark notification as read |

### Search

| Method | Route | Notes |
|---|---|---|
| `GET` | `/api/v1/search` | scoped search across tasks and documents |

### Audit Logs

| Method | Route | Notes |
|---|---|---|
| `GET` | `/api/v1/audit-logs` | admin only |

### Teams

| Method | Route | Notes |
|---|---|---|
| `POST` | `/api/v1/teams` | workspace owner/admin/manager |
| `GET` | `/api/v1/teams` | workspace member |
| `POST` | `/api/v1/teams/{teamId}/members` | workspace owner/admin/manager |
| `DELETE` | `/api/v1/teams/{teamId}/members/{userId}` | workspace owner/admin/manager |

## Task API

### Task Shape

The task model currently includes:

- `id`
- `title`
- `description`
- `ownerId`
- `assignedToUserId`
- `createdAt`
- `updatedAt`
- `dueAtUtc`
- `deadlineReminderSentAtUtc`
- `priority`
- `isCompleted`
- `completedAt`

### Create Task

```http
POST /api/v1/tasks
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "title": "Prepare release notes",
  "description": "Draft and review v1.3 release notes",
  "dueAtUtc": "2026-06-20T17:00:00Z",
  "priority": "High"
}
```

### List Task Query Parameters

`GET /api/v1/tasks` supports:

- `pageNumber`
- `pageSize`
- `searchTerm`
- `isCompleted`
- `status`
- `priority`
- `dueAfterUtc`
- `dueBeforeUtc`
- `ownerId`
- `assignedToUserId`
- `sortBy`
- `sortDirection`

### Task Access Semantics

- `Admin` can list all tasks
- `Manager` can list owned tasks and assigned tasks
- `User` can list owned tasks only
- `GET /api/v1/tasks/{id}` validates access before returning the resource
- update/delete are ownership-based unless the caller is `Admin`

## Document API

### Security Model

The document module is intentionally backend-mediated.

Do not assume files are publicly addressable. There is no supported direct file URL such as:

```text
/uploads/file.pdf
```

All document access must go through API endpoints such as:

```text
GET /api/v1/documents/{id}/download
```

The backend enforces:

- authentication
- role rules
- ownership
- explicit sharing
- manager/team access rules where applicable

### Upload Constraints

Current upload limits:

- max file size: `20 MB`
- allowed extensions:
  - `.pdf`
  - `.png`
  - `.jpg`
  - `.jpeg`
  - `.docx`

The server also validates content type against extension.

### Upload Example

Use `multipart/form-data` with a `file` field.

Example response:

```json
{
  "documentId": "22222222-2222-2222-2222-222222222222",
  "fileName": "report.pdf"
}
```

### Document Listing Query Parameters

`GET /api/v1/documents` and `GET /api/v1/documents/shared-with-me` support:

- `searchTerm`
- `contentType`
- `uploadedFromUtc`
- `uploadedToUtc`
- `pageNumber`
- `pageSize`

### Sharing Semantics

- direct share grants one user access to one document
- task-linked document sharing enforces task participation rules
- revocation removes a previously granted document access entry

## Notifications And Realtime

### Notification API

The REST API is the source of truth for notification state.

Use:

- `GET /api/v1/notifications`
- `PATCH /api/v1/notifications/{id}/read`

### SignalR Hubs

Two hubs are exposed:

- `/hubs/notifications`
- `/hubs/realtime`

The JWT may be sent via `access_token` query string when connecting to hubs.

### Current Realtime Event Categories

The codebase defines realtime events for:

- notification creation
- presence updates
- online/offline state
- task group membership
- document editing presence

### Recommended Client Pattern

Clients should:

1. subscribe to SignalR for instant updates
2. update UI optimistically when safe
3. re-fetch from the REST API after reconnects or important events

Realtime is assistive. The API remains authoritative.

## Search API

`GET /api/v1/search` performs scoped search across tasks and documents.

Query parameters:

- `searchTerm`
- `pageNumber`
- `pageSize`

The results are filtered according to the caller's role and resource access.

## Error Handling

The API generally uses conventional status codes:

- `200 OK`
- `201 Created`
- `204 No Content`
- `400 Bad Request`
- `401 Unauthorized`
- `403 Forbidden`
- `404 Not Found`
- `409 Conflict`
- `500 Internal Server Error`

Most validation and authorization failures return a JSON payload in the shape:

```json
{
  "message": "Human-readable error"
}
```

## Persistence Caveats

API consumers should know that the current implementation is mixed in durability.

Durable today:

- tasks
- notifications
- roles

Not durable today:

- users
- document metadata
- document access grants
- audit logs
- presence state

Implication:

- some data survives restarts
- some data is process-local and reset when the application restarts

This matters especially if you are building a client that expects document metadata, shares, or presence to be durable across restarts.

## Development Notes

### Tech Stack

- .NET `net10.0`
- ASP.NET Core
- EF Core
- PostgreSQL / Npgsql
- JWT bearer auth
- SignalR
- xUnit + Moq

### Repo Structure

```text
Application/
Domain/
Infrastructure/
src/Api/
Views/
wwwroot/
```

### Minimal Frontend Shell

The repository includes a small MVC/JavaScript dashboard used to exercise:

- login
- profile refresh
- notifications
- SignalR connectivity
- API reconciliation after realtime events

It is a consumer of the API, not the primary product UI.

## Known Limitations

- users are stored in-memory
- document metadata is stored in-memory
- document access grants are stored in-memory
- audit logs are stored in-memory
- presence is in-memory and single-process
- uploaded files are stored on the local filesystem
- EF Core migrations are not committed in the repository
- the included frontend is intentionally minimal

## Recommended Next Steps

If you are integrating seriously with this API, the highest-value backend improvements would be:

1. persist users, document metadata, document shares, and audit logs in PostgreSQL
2. add committed EF Core migrations
3. move file storage to object storage
4. make presence distributable for multi-instance deployments
5. add broader integration tests around authorization and realtime flows
