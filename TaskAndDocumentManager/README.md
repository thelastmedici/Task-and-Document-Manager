# TaskAndDocumentManager

An ASP.NET Core API for task and document management, with workspace-aware access control, audit trails, notifications, and background processing.

## Current status

This project is in a feature-rich backend development phase. The core application architecture is in place, the authentication flow is working, and the password reset flow has been implemented and validated. The solution is not yet production-ready because several repository layers still use in-memory storage and deployment hardening is still incomplete.

| Area | Status |
|---|---|
| Runtime | ASP.NET Core on .NET 10 preview |
| API style | REST API with versioned routes under `/api/v1` |
| Auth | Implemented: register, login, current-user, roles, policy checks |
| Password reset | Implemented: forgot-password and reset-password flow with secure token handling |
| Workspace model | Implemented in app logic and routes |
| Tasks | Implemented task create/list/update/delete and assignment flows |
| Documents | Implemented upload, validation, access checks, and secure download flow |
| Notifications | Implemented workspace-aware notifications and read/unread handling |
| Audit logs | Implemented structured audit actions |
| Search | Implemented query/search patterns for tasks and documents |
| Background jobs | Implemented host-based reminder and cleanup jobs |
| SignalR | Implemented notification and presence delivery hooks |
| Test status | Targeted auth/reset regression tests are passing |
| Persistence | Partially in-memory; production persistence remains the main blocker |
| Deployment | Not yet hardened for production hosting |

## What is implemented

- Authentication and authorization
  - register
  - login
  - current-user lookup
  - role-based and policy-based authorization
- Password reset flow
  - forgot-password request
  - secure reset token generation and hashing
  - expiry and single-use enforcement
  - inactive-user protection
  - weak-password validation
  - explicit HTTP semantics for invalid-token and missing-email cases
- Task management
  - create, list, update, delete
  - ownership checks and workspace filtering
  - assignment and completion patterns
- Document management
  - upload validation
  - metadata flow and security checks
  - secure streaming download
  - document sharing and revocation
- Notifications and presence
  - notification records
  - read/unread tracking
  - realtime event dispatch support
- Operational foundation
  - health checks
  - rate limiting
  - structured logging
  - middleware and resilience patterns

## Architecture

The solution follows a layered design:

```text
API
  -> Application
      -> Domain

API
  -> Infrastructure

Infrastructure
  -> Application abstractions
```

Key folders:

```text
Application/
  Auth/
  Tasks/
  Documents/
  Notifications/
  Search/
  Tests/

Domain/
  Auth/
  Tasks/
  Documents/
  Workspaces/

Infrastructure/
  Auth/
  Documents/
  Notifications/
  Persistence/
  Storage/

src/Api/
  Controllers/
  Authorization/
  Security/
  Routing/
  Health/
```

## API surface

The app exposes REST routes under:

```text
/api/v1
```

Examples:

- `/api/v1/auth/register`
- `/api/v1/auth/login`
- `/api/v1/auth/me`
- `/api/v1/auth/forgot-password`
- `/api/v1/auth/reset-password`
- `/api/v1/tasks`
- `/api/v1/documents`
- `/api/v1/notifications`
- `/api/v1/search`
- `/health/live`
- `/health/ready`

## Security and quality notes

The app already includes several security-oriented patterns:

- JWT-based authentication
- policy-based authorization
- workspace-bound access checks
- password reset hashing and expiry validation
- rate limiting on sensitive auth endpoints
- validation for uploaded files and secure storage paths
- generic forgot-password responses to reduce user enumeration risk

## Current blockers before production

The main gaps are not in the feature core; they are in production readiness:

- several repositories still use in-memory storage instead of durable persistence
- document metadata and user/workspace state are not safely persisted across restarts
- EF migrations and durable persistence setup are incomplete
- deployment, secrets management, and operational hardening are still unfinished
- a real production data store and deployment target should be introduced before treating the app as production-grade

## Local development

```bash
dotnet restore
dotnet build TaskAndDocumentManager.sln
dotnet test Application/Tests/Tests.csproj
dotnet run
```

## Bottom line

The project has a solid backend foundation and a working auth-reset implementation, but it is best described as a feature-rich application prototype with a clear path toward production readiness rather than a fully production-hardened system yet.

Author: christian Joshua
