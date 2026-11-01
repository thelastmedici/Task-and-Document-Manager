# Task and Document Manager

A workspace-aware ASP.NET Core application for managing users, tasks, documents, notifications, audit logs, and collaboration within a multi-tenant workspace model.

## Overview

This solution is designed as a modular backend with a clear separation between:

- API controllers
- application use cases
- domain rules and entities
- infrastructure services and persistence

The system is centered on the concept of a workspace. Most actions are scoped to the identity and workspace of the current user, which helps enforce isolation between users, teams, documents, and tasks.

## What the app does

- user registration, login, and profile access
- JWT-based authentication and role-aware authorization
- workspace creation and membership tracking
- task creation, assignment, listing, and updates
- document upload, metadata access, sharing, and linking to tasks
- audit logging and notification workflows
- background jobs for recurring operational tasks
- health checks and API monitoring support

## Architecture

The project follows a layered architecture:

```text
API Layer
  Controllers and routing
    ↓
Application Layer
  Use cases, DTOs, interfaces, validation
    ↓
Domain Layer
  Entities, enums, business rules
    ↓
Infrastructure Layer
  EF Core, PostgreSQL, file storage, JWT, SignalR, auditing, background jobs
```

### API layer

Controllers live under the API project and expose the HTTP contract for the application. They are responsible for request binding, status codes, validation, and translating business results to responses.

### Application layer

Use-case classes encapsulate business workflows such as:

- registration and authentication
- task lifecycle management
- document access and sharing
- workspace membership operations
- audit and notification queries

This keeps controllers thin and makes business logic easier to test.

### Domain layer

Domain entities define the operational model of the product, including workspaces, users, tasks, documents, and access rules. This layer expresses the core business concepts and invariants rather than HTTP concerns.

### Infrastructure layer

Infrastructure handles persistence, security, storage, and platform services.

Common responsibilities include:

- PostgreSQL access via Entity Framework Core
- JWT token generation and validation
- file storage for uploaded documents
- audit log persistence
- notification delivery over SignalR
- hosted background jobs

## Authentication and authorization

The API uses JWT bearer authentication and role-based authorization policies. The auth model includes:

- user registration
- login
- current-user retrieval
- password reset and forgot-password flows
- role checks for admin and manager access
- workspace-aware identity resolution

Authorization policies are defined for:

- authenticated users
- admin-only actions
- manager-or-admin access

The app also sets the active workspace for each request from the authenticated user context before authorization and endpoint execution.

## API route structure

The application is versioned under `api/v1` and includes the following route groups:

### Authentication

- `POST /api/v1/auth/register`
- `POST /api/v1/auth/login`
- `GET /api/v1/auth/me`
- `POST /api/v1/auth/forgot-password`
- `POST /api/v1/auth/reset-password`
- `GET /api/v1/auth/users`
- `POST /api/v1/auth/users`
- `PUT /api/v1/auth/users/{id}/role`
- `PUT /api/v1/auth/users/{id}/deactivate`

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
- `GET /api/v1/documents/{id}`
- `GET /api/v1/documents/{id}/metadata`
- `POST /api/v1/documents/{id}/link-task`
- `POST /api/v1/documents/{id}/share`
- `POST /api/v1/documents/{id}/revoke-access`
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

## Document workflow

The document subsystem is built around safe handling of uploads and controlled visibility across workspace members.

Key behaviors include:

- file upload validation
- size and file-type checks
- document metadata persistence
- document access rules
- sharing with users or tasks
- task-to-document linking
- secure retrieval and deletion flows

This is intentionally enforced through application use cases instead of placing file logic directly in the API layer.

## Notification and real-time model

The project includes SignalR-based infrastructure for live notifications and realtime updates. This supports collaborative usage patterns such as:

- notification delivery
- live workspace updates
- presence awareness
- operational event monitoring

## Background jobs

The app contains a hosted background service and reusable job abstractions for scheduled tasks such as:

- task reminder delivery
- orphaned file cleanup

This keeps operational maintenance isolated from request processing.

## Security and resilience

The solution includes several hardening patterns:

- JWT validation
- policy-based authorization
- rate limiting for sensitive auth and upload endpoints
- middleware for error handling and request metrics
- security headers
- database and file-storage health checks
- conflict handling for concurrency-sensitive operations

## Tech stack

- ASP.NET Core
- .NET 10 preview
- Entity Framework Core
- PostgreSQL
- JWT authentication
- SignalR
- OpenAPI / Swagger
- xUnit test project

## Project structure

```text
TaskAndDocumentManager/
├── Application/
│   ├── Auth/
│   ├── Documents/
│   ├── Notifications/
│   ├── Search/
│   ├── Tasks/
│   ├── Tests/
│   └── Workspaces/
├── Domain/
│   ├── Auth/
│   ├── Documents/
│   ├── Tasks/
│   └── Workspaces/
├── Infrastructure/
│   ├── Audit/
│   ├── Auth/
│   ├── Documents/
│   ├── Notifications/
│   ├── Observability/
│   ├── Persistence/
│   ├── Storage/
│   ├── Tasks/
│   └── Workspaces/
├── src/
│   └── Api/
├── appsettings.json
├── appsettings.Development.json
├── appsettings.example.json
├── global.json
├── TaskAndDocumentManager.csproj
├── TaskAndDocumentManager.sln
├── README.md
└── .gitignore
```

## Local setup

### Prerequisites

- .NET SDK 10 preview
- PostgreSQL instance
- access to JWT configuration values

### Restore and build

```bash
dotnet restore
dotnet build TaskAndDocumentManager.sln
```

### Run the app

```bash
dotnet run
```

### Run tests

```bash
dotnet test TaskAndDocumentManager.sln --nologo --verbosity minimal
```

## Configuration

The application expects configuration values for:

- database connection strings
- JWT issuer / audience / signing key
- optional file-storage settings
- optional email settings

Use the appsettings files and local user secrets for environment-specific configuration.

## Current status

This project is a functional ASP.NET Core backend with a broad set of features and a largely healthy build/test baseline. It is not treated as a fully production-hardened system yet, but it is structurally solid and suitable for further feature work, validation, and hardening.

## Summary

Task and Document Manager is a workspace-centric business application backend that combines authentication, task management, document workflows, notifications, and auditability in a single modular service. It is built around a clean layering strategy and is designed to support real collaboration within a secure workspace model.

