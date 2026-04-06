# TaskAndDocumentManager

TaskAndDocumentManager is an ASP.NET Core backend for:

1. authentication and user management
2. task management
3. document upload, linking, sharing, download, delete, and metadata access

The project is organized in a feature-based clean-architecture style, with separate `Application`, `Domain`, `Infrastructure`, and `src/Api` layers.

This README is written to match the repository as it exists now.

## Current Status

What is already implemented:

- ASP.NET Core web app targeting `net10.0`
- authentication use cases for register, login, current user, and deactivate user
- JWT token generation
- password hashing and password-strength validation
- task domain model, task use cases, task repository, and task API controller
- document domain model, document use cases, document repositories, local file storage, and document API controller
- document sharing and task-linked document sharing between users
- PostgreSQL-backed task persistence with EF Core
- application-layer test coverage for task and document use cases

What is still incomplete:

- JWT bearer validation is not fully configured with `AddJwtBearer(...)`
- user persistence is still in-memory, not PostgreSQL-backed
- document persistence is currently in-memory plus local filesystem storage, not EF Core/database-backed
- EF Core migrations are not committed yet
- there are still MVC template files in the repo even though the active direction is API-first

So the app is usable for local API work, but it is not yet production-ready from an auth/persistence/infrastructure perspective.

## Tech Stack

- .NET SDK: `10.0.100-rc.2.25502.107`
- Target framework: `net10.0`
- ASP.NET Core
- Entity Framework Core
- PostgreSQL via Npgsql
- JWT
- Swagger / Swashbuckle
- xUnit + Moq

## Project Structure

The repository is organized by feature and layer.

### `src/Api`

- `src/Api/Program.cs`
  - startup, dependency injection, Swagger, controller registration

- `src/Api/Controllers/AuthController.cs`
  - auth endpoints

- `src/Api/Controllers/TaskController.cs`
  - task endpoints

- `src/Api/Controllers/DocumentsController.cs`
  - document endpoints

### `Application`

- `Application/Auth`
  - auth DTOs, interfaces, and use cases

- `Application/Tasks`
  - task DTOs, interfaces, and use cases

- `Application/Documents`
  - document DTOs, interfaces, and use cases

- `Application/Tests`
  - application-layer tests

### `Domain`

- `Domain/Auth`
  - `User`

- `Domain/Tasks`
  - `TaskItem`

- `Domain/Documents`
  - `Document`
  - `DocumentAccess`

### `Infrastructure`

- `Infrastructure/Auth`
  - email validation
  - password hashing
  - password validation
  - JWT token service
  - in-memory user repository

- `Infrastructure/Tasks`
  - `TaskDbContext`
  - PostgreSQL-backed task repository

- `Infrastructure/Documents`
  - in-memory document repository
  - in-memory document access repository
  - local filesystem storage service

## Authentication

The auth flow currently supports:

- register a user
- login a user
- return current user details
- deactivate a user

### User Model

The current `User` model contains:

- `Id` as `int`
- `Email`
- `PasswordHash`
- `Role`
- `IsActive`

Defaults:

- role defaults to `"User"`
- active defaults to `true`

### Password Rules

The current password validator requires:

- at least 8 characters
- at least one digit
- at least one uppercase letter

### Password Storage

Passwords are hashed using:

- PBKDF2
- SHA-256
- 100,000 iterations
- random salt

Stored format:

```text
{base64Salt}.{base64Hash}
```

### JWT

JWTs are generated with:

- user id
- email
- role
- standard JWT id claim

Configuration comes from:

- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiresMinutes`

Important limitation:

- token generation is implemented
- token validation middleware is not fully finished because `AddJwtBearer(...)` is still missing

### Auth Persistence Today

User data is still stored in an in-memory static list.

That means:

- users do not survive app restarts
- auth is fine for local development
- auth is not yet production-grade persistence-wise

## Tasks

The task module is implemented end to end through domain logic, use cases, repository, and API controller.

### Task Model

`TaskItem` contains:

- `Id`
- `Title`
- `Description`
- `AssignedToUserId`
- `CreatedByUserId`
- `CreatedAt`
- `UpdatedAt`
- `IsCompleted`
- `CompletedAt`

### Task Rules

The task entity enforces:

- `CreatedByUserId` must not be empty
- `Title` is required
- `Description` is required
- `Title` max length is 200
- `Description` max length is 4000
- completed tasks cannot be modified
- reassigning the same user is a no-op
- saving the same title and description is a no-op

### Task API Endpoints

#### `POST /api/tasks`

Creates a task.

Request body:

```json
{
  "title": "Prepare weekly report",
  "description": "Compile updates for the team",
  "createdByUserId": "11111111-1111-1111-1111-111111111111"
}
```

Success response:

```json
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "message": "Task created successfully"
}
```

#### `GET /api/tasks`

Lists tasks with optional filtering and search.

Query params:

- `pageNumber`
- `pageSize`
- `searchTerm`
- `isCompleted`
- `assignedToUserId`

Example:

```text
/api/tasks?pageNumber=1&pageSize=20&searchTerm=report
```

#### `PUT /api/tasks/{id}`

Updates a task.

Request body:

```json
{
  "title": "Prepare final report",
  "description": "Compile final updates for the team"
}
```

Success response:

- `204 No Content`

#### `DELETE /api/tasks/{id}`

Deletes a task.

Success response:

- `204 No Content`

#### `POST /api/tasks/{id}/assign`

Assigns a task to a user.

Request body:

```json
{
  "userId": "22222222-2222-2222-2222-222222222222"
}
```

Success response:

- `204 No Content`

## Documents

The document module is now implemented through domain, application, infrastructure, and API layers.

### Document Model

`Document` contains:

- `Id`
- `FileName`
- `ContentType`
- `SizeInBytes`
- `StoragePath`
- `UploadedByUserId`
- `UploadedAtUtc`
- `LinkedTaskId`

`DocumentAccess` contains:

- `DocumentId`
- `UserId`
- `GrantedByUserId`
- `GrantedAtUtc`

### Document Capabilities

The current document module supports:

- upload document
- link document to a task
- share document directly with another user
- share a task-linked document with another task participant
- download document
- delete document
- view document metadata

### Document Storage Today

Documents currently use:

- in-memory metadata repository
- in-memory document access repository
- local filesystem storage under the app directory

That means:

- uploaded file contents are written locally
- metadata and share grants are not persisted across app restarts
- document infrastructure is functional for development, but not production-ready yet

### Document API Endpoints

#### `POST /api/documents`

Uploads a document.

Content type:

- `multipart/form-data`

Form fields:

- `file`
- `uploadedByUserId`

Success response:

```json
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "message": "Document uploaded successfully"
}
```

#### `POST /api/documents/{id}/link-task`

Links a document to a task.

Request body:

```json
{
  "taskId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  "requestedByUserId": "11111111-1111-1111-1111-111111111111"
}
```

Success response:

- `204 No Content`

#### `POST /api/documents/{id}/share`

Shares a document directly with another user.

Request body:

```json
{
  "targetUserId": "22222222-2222-2222-2222-222222222222",
  "grantedByUserId": "11111111-1111-1111-1111-111111111111"
}
```

Rules:

- only the document owner can share
- you cannot share with yourself

Success response:

- `204 No Content`

#### `POST /api/documents/{id}/tasks/{taskId}/share`

Shares a task-linked document with another user in the same task context.

Request body:

```json
{
  "targetUserId": "22222222-2222-2222-2222-222222222222",
  "grantedByUserId": "11111111-1111-1111-1111-111111111111"
}
```

Rules:

- only the owner can share
- the document must already be linked to that exact task
- the sharer must be a participant in the task
- the target user must also be a participant in the task
- you cannot share with yourself

Success response:

- `204 No Content`

#### `GET /api/documents/{id}`

Returns document metadata.

Query param:

- `requestedByUserId`

Success response:

```json
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "fileName": "report.pdf",
  "contentType": "application/pdf",
  "sizeInBytes": 1024,
  "uploadedByUserId": "11111111-1111-1111-1111-111111111111",
  "uploadedAtUtc": "2026-04-06T12:34:56Z",
  "linkedTaskId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
}
```

Access:

- owner can view metadata
- directly shared users can view metadata
- task-linked shared users can view metadata once access has been granted

#### `GET /api/documents/{id}/download`

Downloads a document.

Query param:

- `requestedByUserId`

Access:

- owner can download
- shared users can download

#### `DELETE /api/documents/{id}`

Deletes a document.

Query param:

- `requestedByUserId`

Rules:

- only the owner can delete

Success response:

- `204 No Content`

## Database And Persistence

### Tasks

Task persistence is implemented with:

- EF Core
- PostgreSQL
- `TaskDbContext`

Task repository supports:

- create
- get by id
- get all
- search with filtering
- pagination
- update
- delete

### Users

Users are still stored in-memory.

### Documents

Documents are stored with:

- in-memory metadata/access storage
- local file storage

### Current Persistence Gap

The project still needs:

- EF Core migrations
- persistent user storage
- persistent document metadata/access storage

## Testing Status

The repository includes application-layer tests for:

- task creation
- task listing
- pagination and search normalization
- task update
- task deletion
- document metadata access
- document linking
- document sharing
- task-linked document sharing

The tests use:

- xUnit
- Moq

Current observed result:

- `dotnet test Application/Tests/Tests.csproj` passes
- total passing tests observed: `19`

## Running Locally

### Prerequisites

You need:

- .NET 10 SDK
- PostgreSQL

Because `global.json` is pinned to a release-candidate SDK, using the same version is the safest option.

### Restore

```bash
dotnet restore
```

### Build

```bash
dotnet build TaskAndDocumentManager.sln
```

### Run Tests

```bash
dotnet test Application/Tests/Tests.csproj
```

### Run The App

```bash
dotnet run --project TaskAndDocumentManager.csproj
```

Launch settings currently expose:

- `http://localhost:5121`
- `https://localhost:7216`

Swagger is enabled in the Development environment.

## Configuration

Review these before real usage:

1. PostgreSQL connection strings in `appsettings.json` and `appsettings.Development.json`
2. JWT settings in `appsettings.json`
3. auth middleware setup in `src/Api/Program.cs`
4. database schema/migration setup
5. local document storage expectations on the machine running the app

## Known Gaps

- JWT bearer validation is still incomplete
- user IDs in auth use `int`, while task/document ownership uses `Guid`
- users are still in-memory only
- documents are not database-backed yet
- document access grants are in-memory only
- no migrations are committed yet
- some MVC template files remain in the repo even though the current runtime path is API-first

## Summary

The repository now contains a functional API foundation for:

- auth
- tasks
- document upload and download
- document-to-task linking
- direct document sharing
- task-linked document sharing
- document metadata access

The app builds, the tests pass, and the main task/document flows are now exposed through controllers. The biggest remaining work is infrastructure hardening: proper JWT validation, persistent user/document storage, and migrations.
