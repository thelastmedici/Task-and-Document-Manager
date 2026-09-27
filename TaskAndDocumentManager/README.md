# TaskAndDocumentManager

TaskAndDocumentManager is an API-first ASP.NET Core application for secure task management, document handling, workspace collaboration, notifications, audit logging, realtime updates, search, and scheduled background processing.

The project is currently a feature-rich backend prototype with a clean-architecture direction. It has many production-minded patterns already in place, but it is not yet production-ready because several repositories still use in-memory storage and deployment is not fully configured.

## Current Status

| Area | Status |
|---|---|
| Runtime | ASP.NET Core on `.NET 10` preview, pinned by `global.json` |
| API style | REST API under `/api/v1` |
| Realtime | SignalR hubs at `/hubs/notifications` and `/hubs/realtime` |
| Database provider | EF Core configured with PostgreSQL/Npgsql |
| Testing | xUnit test suite, currently `152` passing tests |
| CI/CD | GitHub Actions restore, build, test, publish, and deployment handoff workflow |
| Production readiness | Not production-ready yet; durable persistence and deployment hardening remain |

## What Is Implemented

| Capability | Current Implementation |
|---|---|
| Authentication | Register, login, current-user endpoint, JWT bearer authentication |
| Authorization | Policy-based authorization with admin, manager, and user roles |
| RBAC | System roles through built-in role catalog |
| Workspace roles | Workspace owner, admin, manager, and member roles |
| Ownership | Tasks and documents use backend-owned `OwnerId` checks |
| Tenant isolation | Workspace ID is resolved from JWT and applied to scoped queries |
| Tasks | Create, list, update, delete, assign, complete/reminder-ready task flows |
| Documents | Upload, metadata, secure download streaming, delete, link to task |
| File security | Safe stored filenames, 20 MB upload limit, extension and content-type validation |
| Sharing | Explicit `DocumentAccess` grants with share and revoke flows |
| Notifications | Workspace-aware notifications with read/unread support |
| Audit logs | Structured audit actions with workspace-aware querying |
| SignalR | Notification delivery, user connection tracking, presence tracking |
| Background jobs | Hosted service runner for task reminders and orphaned document file cleanup |
| Search | Task search, document search, audit search, and global search |
| Pagination | Reusable paginated response model for list/query endpoints |
| Caching | Memory cache for stable reference data such as roles and allowed upload types |
| Observability | Structured logs, request metrics middleware, business metrics, health checks |
| Resilience | Safe error responses, timeout-aware storage/realtime operations, cleanup on upload failure |
| Security hardening | Rate limiting, security headers, HSTS outside development, Kestrel server header suppression |
| Testing strategy | Documented unit, integration, and end-to-end testing strategy |

## Architecture

The codebase is organized around domain, application, infrastructure, and API boundaries.

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
  Observability/
  Persistence/
  Storage/
  Tasks/
  Workspaces/

src/Api/
  Authorization/
  BackgroundJobs/
  Controllers/
  Health/
  Hubs/
  Middleware/
  Realtime/
  Routing/
  Security/

Application/Tests/
docs/
wwwroot/
Views/
```

The intended dependency direction is:

```text
API -> Application -> Domain
API -> Infrastructure
Infrastructure -> Application abstractions
```

Controllers should stay thin. Business rules belong in application use cases. Infrastructure details such as EF Core, file storage, SignalR delivery, and caching stay behind abstractions where practical.

## API Surface

All REST routes are versioned under:

```text
/api/v1
```

Route constants are centralized in:

```text
src/Api/Routing/ApiRoutes.cs
```

High-level API areas:

| Area | Example Routes |
|---|---|
| Auth | `/api/v1/auth/register`, `/api/v1/auth/login`, `/api/v1/auth/me` |
| Users | `/api/v1/auth/users`, `/api/v1/auth/users/{id}/role` |
| Tasks | `/api/v1/tasks`, `/api/v1/tasks/{id}`, `/api/v1/tasks/{id}/assign` |
| Documents | `/api/v1/documents`, `/api/v1/documents/{id}/download`, `/api/v1/documents/{id}/share` |
| Shared documents | `/api/v1/documents/shared-with-me` |
| Teams | `/api/v1/teams`, `/api/v1/teams/{teamId}/members` |
| Notifications | `/api/v1/notifications`, `/api/v1/notifications/{id}/read` |
| Search | `/api/v1/search` |
| Audit logs | `/api/v1/audit-logs` |
| Health | `/health/live`, `/health/ready` |

SignalR hubs are intentionally not under the REST version prefix:

```text
/hubs/notifications
/hubs/realtime
```

## Security Model

Implemented security rules:

- Protected routes require JWT authentication.
- Admin and manager operations use policy-based authorization.
- Users never submit `OwnerId` or workspace ownership claims directly for protected resource decisions.
- The backend extracts user identity and workspace context from JWT claims.
- Document downloads are streamed through the API after authorization checks.
- Uploaded files are not exposed through public static URLs.
- Original filenames are metadata only; physical storage names are generated safely.
- Upload validation checks file size, extension, and content type in the application layer.
- Sharing does not change ownership; it creates explicit access grants.
- Audit logs are written after successful critical business actions.
- Rate limits protect sensitive auth routes and document upload routes.
- Security headers reduce common browser-edge risks.

Security work still needed before production:

- Move secrets out of local config and into environment variables or a secret manager.
- Add refresh-token/session management and token revocation strategy if long-lived sessions are needed.
- Add account lockout or stronger throttling for repeated failed login attempts.
- Add production-grade file scanning for malware and deeper content inspection.
- Add centralized authorization policies/handlers for repeated owner/admin/shared-user rules.
- Add distributed rate limiting if the app runs on more than one instance.

## Persistence Status

This is the largest current production gap.

| Data Area | Current Runtime Persistence |
|---|---|
| Tasks | EF Core/PostgreSQL-backed |
| Notifications | EF Core/PostgreSQL-backed |
| Roles | Built-in EF seed/configuration |
| Users | In-memory runtime repository |
| Document metadata | In-memory runtime repository |
| Document access grants | In-memory runtime repository |
| Audit logs | In-memory runtime repository |
| Workspaces | In-memory runtime repository |
| Workspace memberships | In-memory runtime repository |
| Teams | In-memory runtime repository |
| Team memberships | In-memory runtime repository |
| Uploaded file bytes | Filesystem-backed |
| SignalR connection tracking | In-memory runtime state |
| Presence state | In-memory runtime state |

Important implications:

- User accounts do not survive app restart.
- Document metadata does not survive app restart.
- Document shares do not survive app restart.
- Audit logs do not survive app restart.
- Workspace and team runtime state does not survive app restart.
- Uploaded files may remain on disk even when their document metadata is lost.
- Multi-instance deployments would not share presence or SignalR connection state.

EF model configuration already exists for several entities that are not yet wired to durable runtime repositories. Committed EF Core migrations are still missing.

## Realtime

Realtime uses SignalR.

| Hub | Purpose |
|---|---|
| `/hubs/notifications` | Private notification delivery |
| `/hubs/realtime` | Presence and collaboration-style realtime events |

Current realtime design:

- Users can have multiple active SignalR connections.
- Hubs should not contain business logic.
- Application use cases create durable notification records first.
- Realtime delivery happens after successful business actions.
- Realtime is not treated as the source of truth; clients should refresh from REST APIs when needed.

Current event vocabulary includes:

- `NotificationCreated`
- `DocumentShared`
- `DocumentDeleted`
- `TaskAssigned`
- `TaskCompleted`
- `UserPresenceUpdated`

Production work still needed:

- Add a distributed SignalR backplane or managed SignalR service for multi-instance deployments.
- Move presence and connection state out of in-memory storage.
- Add reconnect/replay strategy for missed notifications if the frontend becomes more serious.

## Background Jobs

The app uses an ASP.NET Core hosted service:

```text
src/Api/BackgroundJobs/ScheduledBackgroundJobService.cs
```

Registered jobs:

- `SendTaskDeadlineReminders`
- `CleanupOrphanedDocumentFiles`

The runner is configured through the `BackgroundJobs` settings section.

Production work still needed:

- Add durable job execution if jobs must survive restarts.
- Add retries and dead-letter behavior for future email or external integrations.
- Consider Hangfire, Quartz.NET, or a queue-backed worker once background work grows.
- Add operational visibility for job duration, failure count, and last successful run.

## Search And Querying

Implemented query patterns:

- `TaskQuery`
- `DocumentQuery`
- `AuditQuery`
- `NotificationQuery`
- `GlobalSearchQuery`
- `PaginatedResult<T>`

Current search behavior:

- Tasks support search, status/completion, priority, due-date, owner/assignee filters, sorting, and pagination.
- Documents support filename/content-type/date filtering and pagination.
- Audit logs support user, action, date range, workspace scope, and pagination.
- Global search searches tasks and documents.
- Normal users see only accessible workspace data.
- Admins can access broader results depending on the use case.

Production work still needed:

- Add database indexes for the most common query paths.
- Add full-text search only after database-backed search is stable.
- Consider OpenSearch, Elasticsearch, or Azure AI Search later, not before the relational query layer is solid.

## Observability

Implemented:

- Structured logging for important business and infrastructure events.
- Request metrics for duration, status codes, and server-side failures.
- Business metrics for registrations, logins, task creation, upload success/failure, and upload size.
- Health checks for liveness, readiness, database connectivity, and file storage writability.

Health endpoints:

```text
GET /health/live
GET /health/ready
```

Production work still needed:

- Export logs, metrics, and traces to a real observability backend.
- Add dashboards and alerts for error rate, latency, failed uploads, failed logins, and background job failures.
- Add correlation IDs across requests, background jobs, and realtime dispatch.
- Review all logs for sensitive data leakage.

## CI/CD

GitHub Actions workflow:

```text
.github/workflows/dotnet-ci.yml
```

The workflow runs on pushes, pull requests, and manual dispatch.

Current pipeline:

```text
Restore API
Restore tests
Build API in Release
Run xUnit tests
Publish API artifact
Upload test results
Upload published artifact
Deployment handoff placeholder
```

Production work still needed:

- Replace the deployment placeholder with a real deployment target.
- Add environment-specific deployment stages.
- Add migration execution strategy.
- Add secret injection through the chosen hosting platform.
- Add rollback strategy.
- Add deployment smoke tests.

## Testing

Automated tests live in:

```text
Application/Tests/
```

Run tests:

```bash
dotnet test Application/Tests/Tests.csproj
```

Current verified result:

```text
152 passed
```

The current suite covers:

- authentication and password behavior
- role changes
- task create/list/update/delete flows
- task reminders
- document upload validation
- document download authorization
- document delete/share/revoke/metadata flows
- task-linked document sharing
- notifications
- audit logs
- search and pagination
- workspace and team use cases
- SignalR helpers
- presence tracking
- API route versioning
- caching
- exception handling middleware
- request metrics middleware
- health checks
- security headers and rate-limit policy attributes

Testing strategy:

```text
docs/testing-strategy.md
```

Production work still needed:

- Add database-backed integration tests.
- Add controller/API integration tests using `WebApplicationFactory`.
- Add end-to-end tests for register, login, upload, share, download, and revoke flows.
- Split CI test stages once integration and end-to-end suites become slower.

## Minimal Frontend Shell

The project includes a small MVC/JavaScript client under `Views/` and `wwwroot/`.

It is useful for local checks:

- login
- store a JWT locally
- load current user profile
- list notifications
- connect to SignalR hubs
- reconcile realtime events with API refreshes

It is not a production frontend.

## Configuration

The app expects PostgreSQL and JWT configuration.

Example shape:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=taskanddocumentmanager;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "REPLACE_WITH_A_LONG_RANDOM_SECRET_KEY_FOR_PRODUCTION",
    "Issuer": "TaskAndDocumentManager",
    "Audience": "TaskAndDocumentManager.Client",
    "ExpiresMinutes": 60
  },
  "FileStorage": {
    "RootPath": "storage/uploads",
    "OperationTimeout": "00:00:10"
  },
  "RealtimeDispatch": {
    "OperationTimeout": "00:00:05"
  },
  "BackgroundJobs": {
    "Enabled": true,
    "RunOnStartup": true,
    "InitialDelay": "00:00:30",
    "Interval": "01:00:00"
  }
}
```

Do not use development secrets or placeholder JWT keys in production.

## Running Locally

Restore dependencies:

```bash
dotnet restore
```

Build:

```bash
dotnet build TaskAndDocumentManager.sln
```

Run tests:

```bash
dotnet test Application/Tests/Tests.csproj
```

Run the API:

```bash
dotnet run
```

Swagger is enabled in development.

## Production Readiness Roadmap

### Priority 0: Production Blockers

These should be completed before the system is treated as production-grade.

- Replace in-memory `UserRepository` with EF/PostgreSQL persistence.
- Replace in-memory document metadata repository with EF/PostgreSQL persistence.
- Replace in-memory document access repository with EF/PostgreSQL persistence.
- Replace in-memory audit log repository with EF/PostgreSQL persistence.
- Replace in-memory workspace and team repositories with EF/PostgreSQL persistence.
- Add EF Core migrations and a safe migration deployment process.
- Move file storage to production-grade storage such as object storage or a durable mounted volume.
- Add a real deployment target to CI/CD.
- Move secrets to environment variables or a secret manager.
- Add integration tests against real persistence.

### Priority 1: Production Hardening

- Add full API integration tests.
- Add end-to-end tests for core user journeys.
- Add refresh-token/session strategy if needed.
- Add distributed cache/rate limiting for multi-instance deployments.
- Add distributed SignalR backplane or managed SignalR service.
- Add durable background job processing for retryable external work.
- Add file malware scanning.
- Add stronger audit retention and export/archive strategy.
- Add centralized authorization handlers for repeated access patterns.
- Add observability export, dashboards, alerts, and correlation IDs.

### Priority 2: Scale And Product Maturity

- Add database indexes based on query patterns.
- Add full-text search or external search service when needed.
- Add email/push notification delivery pipeline.
- Add saved filters and richer search UX.
- Add workspace/team administration flows.
- Add formal OpenAPI contract publishing.
- Add deployment smoke tests and rollback automation.
- Add a production frontend if the product moves beyond API-first.

## Bottom Line

The project already demonstrates a strong backend architecture foundation:

- authentication and authorization
- role and ownership enforcement
- secure document upload/download flows
- workspace-aware collaboration
- notifications, audit logs, realtime events, background jobs, search, observability, CI, and tests

The main remaining step is persistence maturity. Once the in-memory repositories are replaced with durable database-backed implementations, migrations are added, and deployment is wired to a real environment, the application will be much closer to a production-grade multi-tenant task and document platform.
