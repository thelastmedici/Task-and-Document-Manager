# TaskAndDocumentManager

A workspace-aware ASP.NET Core application for task, document, and user management.

## Current project status

The project is in an active development phase and is functionally close to a complete backend feature set, but it is not yet production-ready. The solution builds successfully and the test suite is mostly green, with one failing regression that still needs to be resolved.

### Verified status

The current project status was validated with:

```bash
dotnet test TaskAndDocumentManager.sln --nologo --verbosity minimal
```

Result: 166 tests total, 165 passed, 1 failed.

The failing test is:

- `DocumentConcurrencyTests.LinkToTask_ShouldReturnConflict_WhenSaveHitsConcurrencyError`

Failure cause:

- `UploadDocument` cannot be proxied in a mock because it does not expose a parameterless constructor.
- This is a real blocking issue in the test setup and indicates a test or class-construction issue that still needs to be fixed before the suite can be considered clean.

## Implemented areas

### Authentication and authorization
- user registration
- login and authentication
- current-user lookup
- role-based and policy-based authorization
- admin-only and workspace-scoped access checks

### Password management
- forgot-password request flow
- reset-password flow
- hashing and token validation
- password strength validation
- inactive-user protection

### Workspace-aware feature set
- user management by workspace
- workspace membership checks
- role changes across workspace boundaries
- access restrictions based on workspace membership

### Task management
- create tasks
- list tasks by context
- update task state
- delete tasks
- assignment and ownership tracking

### Document management
- document upload
- validation and document metadata handling
- secure download flow
- access checks tied to workspace/user permissions
- concurrency-sensitive document linking

### Notifications and audit trail
- notification records
- read/unread tracking
- audit event logging for key user actions
- audit actions for role and user changes

### Operational features
- health endpoints
- rate limiting on sensitive auth routes
- structured logging support
- middleware and API conventions
- background job infrastructure for reminders and cleanup

## Architecture

The project follows a layered architecture with a .NET web API front end and application/domain separation.

```text
src/Api/
  Controllers/
  Authorization/
  Security/
  Routing/

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
```

## Stack

- ASP.NET Core
- .NET 10 preview
- PostgreSQL via EF Core provider
- JWT-based authentication
- custom authorization policies
- REST API patterns
- test project using xUnit

## Current blockers

The project is close to feature-complete, but several items still need attention before it can be treated as a stable application:

- one failing unit test in the document concurrency area
- some repository and persistence behavior still needs to be confirmed for durable production usage
- deployment and infrastructure hardening are not fully prepared yet
- production-grade security and operational configuration still need a final pass

## Local development

```bash
dotnet restore
dotnet build TaskAndDocumentManager.sln
dotnet test TaskAndDocumentManager.sln --nologo --verbosity minimal
dotnet run
```

## Summary

This project is a substantial backend application with a broad set of implemented features and a mostly passing test suite, but it should currently be considered a feature-rich in-progress project rather than a fully completed or production-hardened system.

The main focus now is resolving the remaining failing concurrency-related document test and then doing a final validation pass for stability, persistence, and deployment readiness.

Author: christian Joshua
license:MIT license
