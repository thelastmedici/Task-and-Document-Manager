# Task and Document Manager API

This project is a workspace-aware ASP.NET Core backend for managing users, tasks, documents, notifications, audit records, and team-level collaboration. The API is designed as a modular monolith with a clear separation between HTTP handlers, application use cases, and persistence concerns.

## API approach

The application follows a layered, domain-driven service approach:

- Controllers expose the HTTP contract and map requests to application operations.
- Use cases encapsulate business logic and validation in the Application layer.
- Domain entities define the business rules and invariants.
- Repositories and infrastructure services handle persistence, storage, notifications, and integration concerns.

This keeps the API surface thin and makes it easier to enforce security, authorization, and workspace context in one place.

## Architecture overview

```text
HTTP Layer
  Controllers / MVC endpoints
    ↓
Application Layer
  Use Cases + DTOs + interfaces
    ↓
Domain Layer
  Entities, enums, rules, access logic
    ↓
Infrastructure Layer
  EF Core, PostgreSQL, file storage, JWT, SignalR, auditing, background jobs
```

## Core design decisions

### 1. Workspace-centric access control

The system is built around the concept of a workspace. Most operations are not global; they are evaluated in the context of the current user’s workspace.

This is enforced by:

- JWT claims that carry user identity and workspace context
- middleware that sets the active workspace before authorization
- authorization policies such as authenticated user, admin-only, and manager-or-admin
- repository queries filtered to the current workspace context

This approach helps prevent cross-workspace leakage and keeps access logic consistent across tasks, documents, and notifications.

### 2. Use-case driven business logic

The App layer intentionally avoids putting business logic directly in controllers. Instead, classes such as:

- RegisterUser
- AuthenticateUser
- CreateTask
- UploadDocument
- ShareDocument
- LinkDocumentToTask
- ListAccessibleDocuments

handle orchestration, validation, and side effects. This keeps controllers focused on HTTP concerns such as model binding, status codes, and response shaping.

### 3. Thin API controllers with strong DTO contracts

Controllers perform validation for request shape and return consistent HTTP responses, while the heavier logic is delegated to application use cases. For example:

- auth endpoints handle login/registration/password reset flows
- task endpoints manage create/update/delete/assignment logic
- document endpoints manage upload, listing, metadata, sharing, and task-linking
- notification and audit endpoints expose read-only operational data

### 4. Persistence with EF Core and PostgreSQL

The API uses PostgreSQL through Entity Framework Core for structured data. The design assumes explicit, well-scoped persistence boundaries:

- users and workspaces
- tasks and assignments
- documents and document access
- notifications and audit history
- team membership and authorization metadata

This allows for transaction-safe writes and the use of database-level checks around concurrency and referential integrity.

## Request flow

A typical authenticated request follows this path:

1. Client authenticates and receives a JWT.
2. Middleware validates the token and sets the current user context.
3. The workspace id is derived from the authenticated identity and attached to the request.
4. Authorization policy checks the user’s role and workspace permissions.
5. The controller resolves the matching action.
6. The action invokes a use case with the actor id and workspace id.
7. The use case validates inputs, calls repositories, and persist changes.
8. The infrastructure layer performs database/file operations and returns a result.
9. The controller translates the outcome into the correct HTTP response.

## Authentication and authorization model

### Authentication

Authentication is implemented with JWT bearer tokens based on configuration values from appsettings and environment settings.

Key characteristics:

- token validation against issuer and audience
- symmetric signing key validation
- expiration checks with zero clock skew
- support for realtime hubs through token extraction from the query string

### Authorization policies

Authorization is policy-based and role-driven. Policies include:

- Authenticated users
- Admin-only access
- Manager or admin access

This pattern gives the app a consistent model for role checks without hard-coding permission logic into every action.

### Workspace-aware identity

The application expects the user to operate within a workspace context. The current workspace is set in middleware before downstream controllers and services run. This is important for functions like:

- document visibility
- task access
- workspace member checks
- audit log scoping

## Main API surface

The API is versioned under the route prefix `api/v1`.

### Authentication

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `GET /api/v1/auth/me`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `PUT /api/v1/auth/users/{id}/deactivate`
- `GET /api/v1/auth/users`
- `POST /api/v1/auth/users`
- `PUT /api/v1/auth/users/{id}/role`

### Tasks

- `POST /api/v1/tasks`
- `GET /api/v1/tasks`
- `GET /api/v1/tasks/{id}`
- `PUT /api/v1/tasks/{id}`
- `DELETE /api/v1/tasks/{id}`
- `POST /api/v1/tasks/{id}/assign`

### Documents

- `POST /api/v1/documents`
- `GET /api/v1/documents`
- `GET /api/v1/documents/shared-with-me`
- `GET /api/v1/documents/{id}/metadata`
- `POST /api/v1/documents/{id}/link-task`
- `POST /api/v1/documents/{id}/share`
- `POST /api/v1/documents/{id}/revoke-access`
- `GET /api/v1/documents/{id}`
- `DELETE /api/v1/documents/{id}`

### Notifications

- `GET /api/v1/notifications`
- `PUT /api/v1/notifications/{id}/read`

### Audit logs

- `GET /api/v1/audit-logs`

### Search

- `GET /api/v1/search`

### Teams

- `POST /api/v1/teams`
- `GET /api/v1/teams`
- `POST /api/v1/teams/{id}/members`
- `DELETE /api/v1/teams/{id}/members/{userId}`

## Document management approach

The document subsystem is designed around safe upload, controlled visibility, and task linkage.

### Upload workflow

The upload flow includes:

- file size and type validation
- content-type normalization
- storage through a file storage service
- metadata persistence in the database
- association with the uploading user and workspace

This reduces the risk of invalid or orphaned document records while keeping the API fast and predictable.

### Access and sharing model

Document visibility is controlled by explicit access logic rather than by simple file permissions alone. The project includes mechanisms for:

- listing documents accessible to a user
- listing shared documents
- sharing by user or task context
- revoking access
- validating ownership and workspace boundaries

This gives a more realistic collaboration model for workspace-based document workflows.

### Task-document linking

Documents can be linked to tasks so that the relationship between work products and assignments stays explicit. The application treats this as a concurrency-sensitive operation and handles conflicts explicitly with `DbUpdateConcurrencyException` responses.

## Real-time and notification approach

The API includes SignalR hubs and notification infrastructure for live updates. This is intended to push events such as:

- task updates
- notifications
- presence tracking
- realtime user state changes

This is useful for collaborative workflows in which multiple users are working within the same workspace and need immediate feedback.

## Background jobs and operational services

The application includes a background job infrastructure to support operational tasks such as:

- reminder dispatch for deadline-sensitive tasks
- cleanup of orphaned document files
- scheduled processing through hosted services

This gives the system a foundation for asynchronous operations without forcing these concerns into request handlers.

## Security and resilience controls

The project includes several protective patterns:

- JWT-based authentication
- role and policy checks
- rate limiting for sensitive auth and upload endpoints
- request metrics middleware
- API exception handling middleware
- security headers middleware
- health checks for database and file storage
- explicit handling of concurrency conflicts and validation failures

## Data and storage patterns

### Relational data

The data layer is centered on PostgreSQL with EF Core and uses patterns such as:

- workspaces as primary tenant boundaries
- soft-delete behavior for documents
- unique indexes for user emails and document sharing rules
- audit log records for operational traceability

### File storage

Document content is stored outside the relational record itself using the file storage service abstraction. This is a practical approach for large or binary-heavy content and keeps the database focused on metadata and access rules.

## Local development setup

```bash
dotnet restore
dotnet build TaskAndDocumentManager.sln
dotnet test TaskAndDocumentManager.sln --nologo --verbosity minimal
dotnet run
```

For local development, ensure your connection string and JWT configuration are set in `appsettings.Development.json` or user secrets.

## Current project status

This solution is feature-rich and mostly healthy, but it is not yet considered fully production-hardened.

Verified status from the current repo state:

- build: successful
- test suite: mostly passing
- known issue: one concurrency-related document test remains failing and requires additional validation

The current API architecture is solid for a modular backend, and the primary remaining work is around stabilizing the document linking and concurrency path before broad production confidence is reached.

## Summary

The API approach for this project is a secure, workspace-oriented, modular ASP.NET Core backend built around use cases, role policies, and repository-based persistence. It balances business logic clarity with operational concerns such as auditing, realtime notifications, background jobs, and file storage. The result is a backend well-suited to a collaborative document-and-task platform, with further hardening needed before full production readiness.

