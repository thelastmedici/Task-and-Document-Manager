# TaskAndDocumentManager
TaskAndDocumentManager is an ASP.NET Core backend that implements authenticated, role-based user management, a task lifecycle with ownership and scoped queries, and a secure document upload/link/share workflow. The solution uses a feature-based clean architecture with `Application`, `Domain`, `Infrastructure`, and `src/Api` layers so responsibilities (HTTP surface, use cases, domain invariants, and persistence) are separated.
## Current status
This repository is functional for local development and demos. Key implemented pieces:
- JWT bearer authentication with role claims and standard validation
- Seeded roles: `Admin`, `Manager`, `User` (fixed GUIDs in `Infrastructure/Persistence/BuiltInRoles.cs`)
- Admin user-management endpoints (create, change role, deactivate, delete)
- Tasks persisted via EF Core to PostgreSQL
- Ownership-based task authorization and query-level task scoping in the application layer
- Protected document access (files are served only through API endpoints)
- Local per-user filesystem storage for uploaded file bytes
- In-memory document metadata and access-grant repositories
- Automated unit/integration tests covering auth, tasks, and document flows
## Implemented features
### Production readiness (caveats)
The API and feature surface are implemented, but persistence and storage are intentionally partial to keep the project simple for demos. Known caveats:
- Users are stored in-memory via `UserRepository` and do not survive process restarts.
- Document metadata and document access grants are in-memory and lost on restart.
- Uploaded file bytes are stored on local disk under `storage/uploads/{userId}/` rather than an object store.
- EF Core migrations are not committed in the repository.
- `TaskDbContext` currently models tasks, users, and roles (consider renaming for clarity).
Summary of persistence:
- tasks: persisted to PostgreSQL via EF Core
- roles: modeled in EF Core and seeded into the database
- users: in-memory
- documents: file bytes on disk, metadata and grants in-memory
## Project structure
### High-level responsibilities
## Authentication and roles
### Password rules
Passwords are validated for a minimum length and basic complexity (uppercase and numeric requirements). Passwords are hashed before storage; plaintext passwords are never persisted.
### JWT
Issued tokens include the user's id, email, role, and a JTI. Token validation supports issuer, audience, signing key and lifetime checks. The following configuration keys are expected:
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiresMinutes`
## Auth endpoints
## Task module
### Task rules
Domain invariants enforced by `TaskItem` and application use cases include required owner, title and description, length limits (title 200 / description 4000), immutable completed tasks, and idempotent assign/update operations.
### Query-level protection
The `ListTasks` use case scopes database queries by the caller's role before the repository query is executed. This prevents fetching all rows and filtering in memory. Scope rules:
- `Admin`: all matching tasks
- `Manager`: owned tasks and assigned tasks
- `User`: owned tasks only
## Document module
### Document security and access
Files are not exposed via public URLs. All file access is mediated by API endpoints (for example `GET /api/documents/{id}/download`) where the backend enforces access checks such as role, ownership, explicit sharing, and optional manager/team access.
### Document storage
Uploaded file bytes are stored on the local filesystem under `storage/uploads/{userId}/{guid}.ext`. The original client filename is retained in metadata for user-facing downloads while the on-disk filename is a GUID-based safe filename. Files are stored outside of `wwwroot` and are never served directly.
### Upload security
Uploads require an authenticated uploader (owner set from JWT), a non-empty file, a 10 MB size limit, and an extension allowlist. Dangerous types are blocked by omission. Allowed extensions:
- `.pdf`
- `.doc`
- `.docx`
- `.txt`
- `.png`
- `.jpg`
- `.jpeg`
### Upload flow
When a user uploads a file the API validates existence, size and extension, reads the uploader id from the JWT, generates a GUID-based safe on-disk filename, saves the file under the uploader's folder, and records metadata linked to `UploadedByUserId` in the document repository (currently in-memory).
### Document endpoints
### Document ownership rules
Non-admin operations are ownership-driven:
- upload: owner set from JWT
- link to task: owner-only
- share document: owner-only
- share task-linked document: owner-only and target must be a task participant
- delete: owner-only
## Persistence overview
### Configuration
Required configuration keys:
- `ConnectionStrings:DefaultConnection` (PostgreSQL)
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiresMinutes`
## running locally
### requirements
- .NET SDK 10 preview
- PostgreSQL
- valid connection string and JWT settings in configuration
### start the API
```bash
dotnet run
```
### build
```bash
dotnet build TaskAndDocumentManager.sln
```
### test
```bash
dotnet test Application/Tests/Tests.csproj
```
## test coverage
The application test project covers authentication, task CRUD and listing, and document upload/access/share/download flows. Latest verified result: 37 passing tests.
## current caveats
- user accounts and document metadata/grants do not survive process restarts (in-memory repositories)
- uploaded files may remain on disk even when metadata is lost
- `TaskDbContext` currently models users and roles in addition to tasks
## recommended next steps
1. Move users to EF Core / PostgreSQL persistence and commit EF migrations.
2. Persist document metadata and access grants to the database (avoid in-memory-only state).
3. Rename `TaskDbContext` to a broader application-level context if you consolidate models.
4. Add controller and integration tests that exercise auth, task authorization, and document authorization.
5. Migrate local file storage to a production-grade object store (S3, Azure Blob, GCS) or a secure managed file store.
If you'd like, I can open a PR with these README changes or prepare a migration plan and starter EF migrations for users and documents.
# TaskAndDocumentManager

TaskAndDocumentManager is an ASP.NET Core backend for:

1. authentication and role-based user management
2. task creation, ownership, assignment, and query protection
3. secure document upload, linking, sharing, metadata access, download, and deletion

The solution follows a feature-based clean architecture layout with `Application`, `Domain`, `Infrastructure`, and `src/Api` layers.

## Current State

This repository is no longer just a skeleton. It already includes:

- JWT-based authentication with bearer validation configured in ASP.NET Core
- role claims in tokens
- seeded roles: `Admin`, `Manager`, `User`
- admin user-management endpoints
- PostgreSQL-backed task persistence via EF Core
- ownership-based task authorization
- query-level task filtering in the application layer
- protected document access through API endpoints only
- local filesystem document storage organized per user
- in-memory document metadata and access-grant repositories
- automated tests covering auth, tasks, document upload, document access, and sharing rules

## What Is Implemented

### Auth

- register user
- login user
- get current user profile
- list users
- create user as admin
- change a user's role
- deactivate user
- delete user

### Tasks

- create task
- list tasks with paging and filtering
- get task by id
- update task
- delete task
- assign task to a user

### Documents

- upload document
- list accessible documents
- link document to a task
- share document directly with another user
- share task-linked document with a task participant
- get document metadata
- download document
- delete document

## What Is Still Not Fully Production-Ready

- user persistence is still in-memory through `UserRepository`
- document metadata and document access grants are still in-memory
- uploaded files are stored locally on disk rather than in cloud/object storage
- EF Core migrations are not committed in this repo yet
- `TaskDbContext` currently includes task, user, and role tables even though its name is still task-specific

So the API behavior is strong enough for local development and feature demonstration, but persistence is mixed:

- tasks: database-backed
- roles: seeded in database
- users: in-memory
- documents: metadata in-memory, file bytes on local disk

## Tech Stack

- .NET `net10.0`
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL with Npgsql
- JWT bearer authentication
- Swagger / Swashbuckle
- xUnit + Moq

## Project Structure

```text
src/
├── Api/
│   ├── Authorization/
│   └── Controllers/
│
Application/
├── Auth/
├── Tasks/
├── Documents/
└── Tests/
│
Domain/
├── Auth/
├── Tasks/
├── Documents/
└── Entities/
│
Infrastructure/
├── Auth/
├── Tasks/
├── Documents/
└── Persistence/
```

### High-Level Responsibilities

- `src/Api`
  - HTTP endpoints
  - authentication / authorization wiring
  - claim extraction from JWT

- `Application`
  - use cases
  - DTOs
  - interfaces
  - application-layer access decisions

- `Domain`
  - core entities and invariants

- `Infrastructure`
  - repositories
  - EF Core context
  - local file storage
  - password hashing
  - JWT token creation
  - seeded role catalog

## Authentication And Roles

### User Model

The current `User` entity contains:

- `Id` as `Guid`
- `Email`
- `PasswordHash`
- `RoleId`
- `Role`
- `IsActive`

### Role Model

Roles are modeled as a separate entity:

- `Id`
- `Name`

The system seeds three fixed roles:

- `Admin`
- `Manager`
- `User`

Those seeded role IDs are defined in `Infrastructure/Persistence/BuiltInRoles.cs`.

### Password Rules

Passwords are validated for:

- minimum length
- uppercase requirement
- numeric requirement

Passwords are hashed before storage. The app never stores plaintext passwords.

### JWT

JWTs include:

- user id
- email
- role
- JWT id

JWT validation is configured with:

- issuer validation
- audience validation
- signing key validation
- lifetime validation

Configuration keys used by the app:

- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiresMinutes`

### Auth Authorization Policies

The API defines these policies:

- `Authenticated`
- `AdminOnly`
- `ManagerOrAdmin`

These are used across the controllers to protect routes by role.

## Auth Endpoints

### `POST /api/auth/register`

Registers a normal user.

Behavior:

- validates email
- validates password
- assigns the default role as `User`
- stores the user in the in-memory user repository

Request body:

```json
{
  "email": "user@example.com",
  "password": "Password1"
}
```

### `POST /api/auth/login`

Authenticates a user and returns a JWT plus user profile.

Request body:

```json
{
  "email": "user@example.com",
  "password": "Password1"
}
```

### `GET /api/auth/me`

Returns the current authenticated user's profile.

Access:

- authenticated users only

### `GET /api/auth/users`

Returns the user list.

Access:

- `Manager` or `Admin`

### `POST /api/auth/users`

Creates a user with an explicit role.

Access:

- `Admin` only

Request body:

```json
{
  "email": "manager@example.com",
  "password": "Password1",
  "roleId": "22222222-2222-2222-2222-222222222222"
}
```

### `PUT /api/auth/users/{id}/role`

Changes a user's role.

Access:

- `Admin` only

Request body:

```json
{
  "roleId": "11111111-1111-1111-1111-111111111111"
}
```

### `PUT /api/auth/users/{id}/deactivate`

Deactivates a user.

Access:

- `Admin` only

### `DELETE /api/auth/users/{id}`

Deletes a user.

Access:

- `Admin` only

## Task Module

### Task Model

`TaskItem` contains:

- `Id`
- `Title`
- `Description`
- `AssignedToUserId`
- `OwnerId`
- `CreatedAt`
- `UpdatedAt`
- `IsCompleted`
- `CompletedAt`

### Task Rules

The task domain currently enforces:

- owner is required
- title is required
- description is required
- title max length is 200
- description max length is 4000
- completed tasks cannot be modified
- assigning the same user again is a no-op
- saving the same title and description is a no-op

### Ownership And Authorization

Task ownership is backend-controlled.

The client does not send `OwnerId` during task creation. The server:

1. reads the current authenticated user from JWT
2. passes that user id into the create use case
3. stores it as `OwnerId`

Sensitive task operations are protected like this:

- `GET /api/tasks/{id}`
  - `Admin` or task owner
- `PUT /api/tasks/{id}`
  - `Admin` or task owner
- `DELETE /api/tasks/{id}`
  - `Admin` or task owner
- `POST /api/tasks/{id}/assign`
  - `Manager` or `Admin`
  - managers are also scoped by task ownership/assignment rules

### Query-Level Protection

Task list protection is implemented in the application layer, not in the frontend and not by fetching all tasks and filtering later.

The `ListTasks` use case applies access scope before querying the repository:

- `Admin`
  - all matching tasks
- `Manager`
  - owned tasks and assigned tasks
- `User`
  - owned tasks only

This means the controller does not fetch all tasks and then trim them in memory anymore.

### Task Endpoints

#### `POST /api/tasks`

Creates a task.

Request body:

```json
{
  "title": "Prepare weekly report",
  "description": "Compile updates for the team"
}
```

#### `GET /api/tasks`

Lists tasks with optional filters.

Query parameters:

- `pageNumber`
- `pageSize`
- `searchTerm`
- `isCompleted`
- `assignedToUserId`

#### `GET /api/tasks/{id}`

Returns a single task if the current user is allowed to read it.

#### `PUT /api/tasks/{id}`

Updates a task if the current user owns it or is admin.

Request body:

```json
{
  "title": "Updated title",
  "description": "Updated description"
}
```

#### `DELETE /api/tasks/{id}`

Deletes a task if the current user owns it or is admin.

#### `POST /api/tasks/{id}/assign`

Assigns the task to another user.

Request body:

```json
{
  "userId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
}
```

## Document Module

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

### Document Security Model

Protected file access goes through the API only.

The app does **not** rely on public file URLs like:

```text
/uploads/file.pdf
```

Instead, secure file access goes through:

```text
GET /api/documents/{id}/download
```

The backend then checks:

- role
- ownership
- explicit sharing
- optional manager/team access

### Current Access Rules

#### User

Can access:

- their own files
- files explicitly shared with them

#### Admin

Can access:

- all files

#### Manager

Can access:

- their own files
- files explicitly shared with them
- team/task-linked files when manager task participation access is enabled by the backend

### Document Storage Strategy

The project currently uses a private local filesystem strategy for uploaded document bytes.

Stored file structure:

```text
storage/uploads/{userId}/{guid}.ext
```

Example:

```text
storage/uploads/11111111-1111-1111-1111-111111111111/9f8a7c2d0f4d4d4dbf2c3f9e4d5a6b7c.pdf
```

Important notes:

- files are not stored under `wwwroot`
- files are not directly public
- the original client filename is not used as the stored filename
- a GUID-based safe filename is generated for disk storage
- the original filename is still kept in metadata for download display

### Upload Security Rules

The current upload pipeline enforces:

- authenticated uploader only
- owner comes from JWT, not client input
- non-empty file required
- file size limit of 10 MB
- extension allowlist
- blocked dangerous types by omission from the allowlist
- GUID-based stored filename

Currently allowed extensions:

- `.pdf`
- `.doc`
- `.docx`
- `.txt`
- `.png`
- `.jpg`
- `.jpeg`

### Upload Flow

When a user uploads:

1. the API validates the file exists
2. the API validates size
3. the API validates allowed extension
4. the backend gets the uploader id from JWT
5. the file storage service generates a safe GUID-based disk filename
6. the file is saved to disk under the uploader's folder
7. document metadata is saved in the document repository
8. the metadata is linked to `UploadedByUserId`

### Document Endpoints

#### `POST /api/documents`

Uploads a document.

Content type:

- `multipart/form-data`

Form field:

- `file`

#### `GET /api/documents`

Returns accessible documents for the current user.

Behavior:

- `Admin` gets all documents
- non-admins get only documents allowed by the document access evaluator

#### `POST /api/documents/{id}/link-task`

Links a document to a task.

Request body:

```json
{
  "taskId": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
}
```

#### `POST /api/documents/{id}/share`

Shares a document with another user.

Request body:

```json
{
  "targetUserId": "cccccccc-cccc-cccc-cccc-cccccccccccc"
}
```

#### `POST /api/documents/{id}/tasks/{taskId}/share`

Shares a task-linked document with a user who must also be a participant in the linked task.

Request body:

```json
{
  "targetUserId": "cccccccc-cccc-cccc-cccc-cccccccccccc"
}
```

#### `GET /api/documents/{id}`

Returns document metadata if the current user is allowed to view it.

#### `GET /api/documents/{id}/download`

Returns the document file stream if the current user is allowed to download it.

#### `DELETE /api/documents/{id}`

Deletes a document if the current user is allowed to delete it.

### Document Ownership Rules

Current non-admin ownership behavior:

- upload
  - owner comes from JWT
- link to task
  - owner-only
- share document
  - owner-only
- share task-linked document
  - owner-only plus linked-task participant checks
- delete
  - owner-only

## Persistence Overview

### Tasks

- persisted with EF Core
- stored in PostgreSQL
- configured in `TaskDbContext`

### Roles

- modeled in EF Core
- seeded through `BuiltInRoles`
- `Admin`, `Manager`, and `User` are seeded with fixed GUIDs

### Users

- currently stored in an in-memory static list
- role information is attached using role ids and the built-in role catalog

### Documents

- file bytes stored on local disk
- document metadata stored in-memory
- document access grants stored in-memory

## Configuration

The app expects:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiresMinutes`

## Running Locally

### Requirements

- .NET SDK 10 preview
- PostgreSQL
- valid connection string
- JWT settings in configuration

### Start The API

```bash
dotnet run
```

### Build

```bash
dotnet build TaskAndDocumentManager.sln
```

### Test

```bash
dotnet test Application/Tests/Tests.csproj
```

## Test Coverage

The application test project currently covers:

- authenticate user
- create task
- update task
- delete task
- list tasks
- upload document
- get document metadata
- list accessible documents
- share document
- share task-linked document
- download document

Latest verified result:

- `37` passing tests

## Current Caveats

- user accounts do not survive app restart
- document metadata does not survive app restart
- document access grants do not survive app restart
- uploaded files can remain on disk even though document metadata is in-memory
- the task context currently also models users and roles
- the app is API-first, but some repo history may still reflect older template structure

## Recommended Next Steps

- move users to EF Core / PostgreSQL persistence
- move document metadata and document access grants to EF Core / PostgreSQL persistence
- add committed migrations
- consider renaming `TaskDbContext` to a broader app-level context name
- add controller/integration tests for auth, task authorization, and document authorization
- move local file storage to a production-grade storage backend when needed
