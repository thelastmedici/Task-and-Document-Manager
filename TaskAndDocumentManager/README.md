# TaskAndDocumentManager

TaskAndDocumentManager is an ASP.NET Core project that is currently focused on two main areas:

1. user authentication and account management
2. task management domain logic and application use cases

The repository name includes "DocumentManager", but document-management features are not implemented yet in the codebase. At the moment, the strongest parts of the project are the authentication flow foundations, the task domain model, the task use-case layer, and the unit tests around the task application layer.

This README is intentionally written around the repository as it exists today, not around a future target architecture.

## Current State Of The Project

What is already in place:

- a runnable ASP.NET Core web project targeting `net10.0`
- JWT token generation logic
- user registration, login, current-user lookup, and deactivate-user use cases
- password hashing and password-strength validation
- email validation
- a `TaskItem` domain entity with clear business rules
- task use cases for create, list, update, delete, and assign
- a PostgreSQL-backed task repository using Entity Framework Core
- application-layer tests for the main task use cases

What is only partially completed or still needs wiring:

- authentication middleware is started, but JWT bearer validation is not fully configured with `AddJwtBearer(...)`
- `DeactivateUser` exists, but it is not registered in dependency injection in `Program.cs`
- users are not stored in PostgreSQL yet; the current user repository is an in-memory static list
- task HTTP endpoints/controllers are not present yet, even though the task application logic exists
- document-management features are not present yet
- Entity Framework migrations are not included in the repository
- there are MVC views and scaffolded pages in the repo, but the current startup path is API-oriented and uses `AddControllers()`

Because of that, this project is best described as a backend foundation / work-in-progress rather than a finished production API.

## Tech Stack

- .NET SDK: `10.0.100-rc.2.25502.107` from `global.json`
- Target framework: `net10.0`
- Web framework: ASP.NET Core
- Data access: Entity Framework Core with Npgsql
- Database: PostgreSQL
- Authentication token format: JWT
- API docs: Swagger / Swashbuckle
- Testing: xUnit + Moq

Nuance worth calling out:

- the root web app is in `TaskAndDocumentManager.csproj`
- `src/Api/Program.cs` is the startup entry point currently being used by the web app
- separate `Application` and `Domain` class library projects also exist and are used by the test project

## Repository Structure

The repository is organized in a clean-architecture style, even though the runnable app is still a single web project at the top level.

### Root Web App

- `TaskAndDocumentManager.csproj`
  - the main runnable ASP.NET Core project
  - pulls in the API, domain, infrastructure, and view files that live inside this repository tree

### `src/Api`

- `src/Api/Program.cs`
  - application startup
  - service registration
  - database registration
  - authentication/authorization setup
  - controller mapping
  - Swagger setup in development

- `src/Api/Controllers/AuthController.cs`
  - exposes the authentication-related HTTP endpoints currently defined in the app

- `src/Api/Controllers/ClaimsPrincipalExtensions.cs`
  - helper for reading the authenticated user id claim

### `Domain`

- `Domain/Entities/User.cs`
  - user model used by authentication logic
  - stores `Id`, `Email`, `PasswordHash`, `Role`, and `IsActive`

- `Domain/Entities/TaskItem.cs`
  - the main task aggregate/entity
  - contains business rules for title/description validation, assignment, updates, and completion protection

### `Application`

- `Application/Auth`
  - DTOs, interfaces, and use cases for authentication and user management

- `Application/Tasks`
  - DTOs, repository abstraction, and use cases for task operations

- `Application/Tests`
  - xUnit tests covering the task application layer

### `Infrastructure`

- `Infrastructure/Auth`
  - password hashing
  - password validation
  - email validation
  - JWT token creation

- `Infrastructure/Persistence`
  - `TaskDbContext`
  - task repository implementation
  - user repository implementation

### MVC Template Artifacts Still In The Repo

The repository still contains:

- `Controllers/HomeController.cs`
- `Views/...`
- `Views/Auth/...`
- `wwwroot/...`

These are useful to know about, but they are not the main active direction of the current startup configuration. The current startup registers controllers for API usage, not full MVC with views.

## What Has Been Built So Far

## 1. Authentication And User Management

The authentication flow is already modeled in code and consists of:

- registering a user
- validating the email format
- validating password strength
- hashing the password before storage
- logging a user in
- generating a JWT token
- retrieving the currently authenticated user
- deactivating a user account

### User Model

The `User` entity currently contains:

- `Id` as `int`
- `Email`
- `PasswordHash`
- `Role`
- `IsActive`

Defaults in the current code:

- role defaults to `"User"`
- active status defaults to `true`

### Registration Rules

The `RegisterUser` use case enforces:

- email must pass `MailAddress` format validation
- password must be considered strong by the password validator
- email must be unique inside the current repository implementation

Current password-strength rule:

- minimum length of 8 characters
- must contain at least one digit
- must contain at least one uppercase letter

### Password Handling

Passwords are not stored in plain text.

The project currently uses:

- PBKDF2
- SHA-256
- 100,000 iterations
- 16-byte random salt
- 32-byte derived hash

The stored format is:

```text
{base64Salt}.{base64Hash}
```

### Login Flow

The login use case:

- normalizes email to lowercase
- checks whether the user exists
- blocks login if the account is deactivated
- verifies the password hash
- issues a JWT token if the credentials are valid

### JWT Token Contents

The token service places these claims into the token:

- subject / user id
- name identifier
- email
- role
- JWT id (`jti`)

The token settings are read from `appsettings.json`:

- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiresMinutes`

Default expiration in the current config:

- 60 minutes

### Important Limitation In The Current Auth Setup

Authentication is not fully wired end-to-end yet.

Specifically:

- `builder.Services.AddAuthentication(...)` is called
- but `AddJwtBearer(...)` is not configured
- so JWT validation for protected endpoints is not fully set up yet

There is also a dependency injection gap:

- `AuthController` requires `DeactivateUser`
- `DeactivateUser` is not currently registered in `src/Api/Program.cs`

That means the authentication controller is defined in code, but the runtime setup still needs finishing before all auth endpoints can be relied on as fully working.

### User Persistence Today

User persistence is currently not backed by PostgreSQL.

`Infrastructure/Persistence/Repositories/UserRepository.cs` uses:

- a static in-memory `List<User>`

That means:

- registered users do not survive an application restart
- this is suitable for early development only
- the auth side is not yet using Entity Framework persistence

## 2. Task Domain And Application Layer

The task side of the project is one of the stronger and more complete areas from a domain/use-case perspective.

### Task Entity

The `TaskItem` entity currently contains:

- `Id` as `Guid`
- `Title`
- `Description`
- `AssignedToUserId` as nullable `Guid`
- `CreatedByUserId` as `Guid`
- `CreatedAt`
- `UpdatedAt`
- `IsCompleted`
- `CompletedAt`

### Task Business Rules

The task entity enforces the following rules:

- `CreatedByUserId` cannot be empty
- `Title` is required
- `Description` is required
- `Title` cannot exceed 200 characters
- `Description` cannot exceed 4000 characters
- completed tasks cannot be modified
- assigning the same user again is treated as a no-op
- updating with the same title and description is treated as a no-op

### Task Behaviors Already Implemented

- create a task
- update a task
- delete a task
- assign a task to a user
- unassign a task
- mark a task as completed
- list tasks
- search tasks
- filter tasks by completion state
- filter tasks by assigned user
- paginate task results

### Task Use Cases

The application layer currently includes these use cases:

- `CreateTask`
- `UpdateTask`
- `DeleteTask`
- `AssignTask`
- `ListTasks`

Important clarification:

- these use cases exist and are tested
- but there is not yet a task API controller exposing them over HTTP

So the task system is currently more complete in the domain/application layers than in the presentation/API layer.

## 3. Database And Persistence

### Task Persistence

Task persistence is implemented with Entity Framework Core and PostgreSQL.

`TaskDbContext` currently maps:

- `Tasks`

The repository implementation supports:

- create
- get by id
- get all
- search with pagination
- update
- delete

### Search Behavior

The task repository supports:

- pagination via page number and page size
- completion-state filtering
- assigned-user filtering
- text search over title and description

Text search is implemented with PostgreSQL `ILIKE` through EF Core.

### Connection Strings

The repository currently ships with these config files:

- `appsettings.json`
- `appsettings.Development.json`

Configured databases:

- production/default: `taskanddocumentmanager`
- development: `taskanddocumentmanager_dev`

Default local credentials currently shown in config:

```json
"Host=localhost;Port=5432;Database=taskanddocumentmanager;Username=postgres;Password=postgres"
```

You will almost certainly want to replace these values in your own environment.

### Current Persistence Limitation

There are no migrations checked into the repository yet.

That means:

- the `TaskDbContext` exists
- the task repository exists
- but database schema creation / migration workflow is not yet documented or committed here

## HTTP Endpoints Currently Defined

These are the HTTP endpoints currently implemented in `AuthController`.

## `POST /api/auth/register`

Registers a new user.

Request body:

```json
{
  "email": "user@example.com",
  "password": "Password1"
}
```

Success response:

```json
{
  "message": "User registered successfully"
}
```

Possible failure cases:

- `400 Bad Request`
  - invalid email
  - weak password
- `409 Conflict`
  - user already exists
- `500 Internal Server Error`
  - unexpected error

## `POST /api/auth/login`

Authenticates a user and returns a JWT plus user profile information.

Request body:

```json
{
  "email": "user@example.com",
  "password": "Password1"
}
```

Success response shape:

```json
{
  "token": "jwt-token-here",
  "expiresAtUtc": "2026-04-02T12:34:56Z",
  "user": {
    "id": 1,
    "email": "user@example.com",
    "role": "User",
    "isActive": true
  }
}
```

Failure case:

- `401 Unauthorized`
  - invalid credentials
  - deactivated account

## `GET /api/auth/me`

Returns the currently authenticated user.

Intended access:

- authenticated user only

Response shape:

```json
{
  "id": 1,
  "email": "user@example.com",
  "role": "User",
  "isActive": true
}
```

Important note:

- this endpoint depends on JWT bearer auth being fully configured

## `PUT /api/auth/users/{id}/deactivate`

Deactivates a user account.

Intended access:

- admin only

Success response:

- `204 No Content`

Possible failure cases:

- `404 Not Found`
  - user does not exist
- `500 Internal Server Error`
  - unexpected error

Important note:

- this endpoint depends on role-based auth being fully configured
- it also depends on the missing `DeactivateUser` DI registration being added

## What Is Not Exposed Over HTTP Yet

Even though the task system has application logic and persistence, the following are not yet exposed through a controller in the current repository:

- create task endpoint
- list task endpoint
- update task endpoint
- delete task endpoint
- assign task endpoint

If someone clones this repo today, they should think of the task feature as "implemented in the core/backend layers, but not yet finished as a public API surface."

## Testing Status

The repository contains application-layer tests for the task use cases.

Currently covered areas:

- task creation
- task listing
- pagination/search normalization
- task update
- task deletion

The tests are written with:

- xUnit
- Moq

Current observed result from the repository:

- `dotnet test Application/Tests/Tests.csproj` passes
- total passing tests observed: `9`

That includes the task use-case tests plus one placeholder test file.

## How To Run The Project Locally

## Prerequisites

You need:

- .NET 10 SDK
- PostgreSQL
- a local PostgreSQL user/password that match your connection string

Because `global.json` is pinned to a release-candidate SDK, using the same SDK version is the safest option for consistency.

## Restore

```bash
dotnet restore
```

## Build

```bash
dotnet build TaskAndDocumentManager.sln
```

## Run Tests

```bash
dotnet test Application/Tests/Tests.csproj
```

## Run The App

```bash
dotnet run --project TaskAndDocumentManager.csproj
```

Launch settings currently point to:

- `http://localhost:5121`
- `https://localhost:7216`

Swagger is enabled in the Development environment.

## Configuration Checklist Before Running For Real

Before relying on the app end-to-end, you should review:

1. PostgreSQL connection strings in `appsettings.json` and `appsettings.Development.json`
2. JWT settings in `appsettings.json`
3. dependency injection setup in `src/Api/Program.cs`
4. JWT bearer middleware configuration in `src/Api/Program.cs`
5. database migration/schema setup for tasks

## Known Gaps And Honest Caveats

This section is here on purpose so anyone reading the repo understands what is done and what still needs attention.

- the project builds successfully
- the task application-layer tests pass
- the authentication controller exists
- the task use cases exist
- task persistence exists for tasks only
- user persistence is still in-memory
- task endpoints are not yet added
- JWT generation exists, but JWT bearer validation is not fully configured
- the auth controller depends on `DeactivateUser`, which still needs DI registration
- document-management functionality is not yet implemented despite the project name
- some MVC/template files remain in the repo, but the current startup direction is API-first

## Suggested Next Steps

If you want to continue this project from where it currently stands, the most natural next steps are:

1. register `DeactivateUser` in dependency injection
2. configure `AddJwtBearer(...)` so `[Authorize]` endpoints work properly
3. move user persistence from the in-memory repository to Entity Framework/PostgreSQL
4. add a task controller to expose the existing task use cases over HTTP
5. create and commit EF Core migrations
6. decide what the document-management module should include and then implement it explicitly

## Summary

So far, this project has a meaningful backend foundation:

- auth use cases are written
- JWT token creation is implemented
- password hashing and validation are in place
- task business rules are modeled well
- task use cases are implemented and tested
- PostgreSQL-backed task persistence is present

The repo is not finished yet, but it already contains a solid base for turning this into a proper task and document management API once the remaining wiring and missing features are completed.
