# Task and Document Manager

A workspace-aware ASP.NET Core application for task, document, authentication, and audit management.

## Current project status

This project is in an active but incomplete state. The solution builds successfully, and the test suite is largely healthy, but there is still one confirmed failing test in the document concurrency area.

### Verified status

I verified the current state with:

```bash
dotnet test TaskAndDocumentManager.sln --nologo --verbosity minimal
```

Current result from the repository as checked:

- Build: succeeded
- Tests: 166 total
- Passed: 165
- Failed: 1
- Failed test: `DocumentConcurrencyTests.LinkToTask_ShouldReturnConflict_WhenSaveHitsConcurrencyError`

The failure is a real test setup issue, not a compile-time problem. The current error is:

- `Can not instantiate proxy of class: TaskAndDocumentManager.Application.Documents.UseCases.UploadDocument`
- `Could not find a parameterless constructor.`

This means the document-upload concurrency path still needs a focused fix in the test or implementation setup before the suite can be considered fully green.

## What is implemented

### Authentication and authorization
- user registration and login
- JWT-based authentication
- current-user retrieval
- role-based and policy-based authorization
- admin-only and workspace-scoped access checks
- password reset and forgot-password flows

### Workspace management
- workspace-aware user access
- workspace membership checks
- role changes across workspace boundaries
- access restrictions tied to workspace context

### Task management
- create, update, list, and delete tasks
- task ownership and assignment handling
- task operations scoped to the active workspace or user context

### Document management
- document upload and validation
- document metadata handling
- secure download flow
- access checks for users and workspaces
- concurrency-sensitive document linking

### Notifications and auditing
- notification records and read state tracking
- audit logs for key operations
- user and role change auditing
- operational logging and API middleware support

### Supporting platform features
- health checks
- Swagger/OpenAPI support
- background job structure
- middleware and API conventions
- rate limiting on sensitive auth routes

## Project structure

```text
TaskAndDocumentManager/
├── Application/
│   ├── Auth/
│   ├── Documents/
│   ├── Notifications/
│   ├── Tasks/
│   ├── Workspaces/
│   └── Tests/
├── Domain/
├── Infrastructure/
├── src/
│   └── Api/
├── Models/
├── Views/
├── wwwroot/
├── TaskAndDocumentManager.csproj
├── TaskAndDocumentManager.sln
├── appsettings.json
├── appsettings.Development.json
├── appsettings.example.json
├── README.md
└── global.json
```

## Technology stack

- ASP.NET Core
- .NET 10 preview
- Entity Framework Core
- PostgreSQL provider for EF Core
- JWT authentication
- Swagger / OpenAPI
- xUnit test project

## Local setup

```bash
dotnet restore
dotnet build TaskAndDocumentManager.sln
dotnet test TaskAndDocumentManager.sln --nologo --verbosity minimal
dotnet run
```

## Current priorities

1. Fix the remaining failing document concurrency test.
2. Validate the upload/link workflow under realistic persistence and concurrency conditions.
3. Harden the project for production readiness and deployment confidence.
4. Finalize a broader regression pass once the outstanding failure is resolved.

## Overall assessment

This is a substantial backend project with a broad set of implemented features and a mostly passing suite. It is best described as a feature-rich in-progress application rather than a production-ready system.

The main blocker today is the single failing concurrency-related document test, and addressing that will bring the project much closer to a stable, releasable state.

## Notes

- The repository is currently using a preview .NET SDK version.
- The project is suitable for continued development and feature work.
- It is not yet considered production-hardened or deployment-complete.
