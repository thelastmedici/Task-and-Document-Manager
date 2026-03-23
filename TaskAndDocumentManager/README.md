# TaskAndDocumentManager API

[![.NET](https://img.shields.io/badge/.NET-10.0-512bd4)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A high-performance, scalable RESTful API for task tracking and document management, engineered using **ASP.NET Core 10.0** and adhering to **Clean Architecture** principles.

## 🏗 Architecture

The solution is architected to enforce separation of concerns and dependency inversion, ensuring maintainability and testability.

*   **`src/Domain`**: The core of the application containing enterprise logic, entities, and value objects. No external dependencies.
*   **`src/Application`**: Orchestrates application logic, defining use cases, interfaces (abstractions), and DTOs.
*   **`src/Infrastructure`**: Implements interfaces defined in Application (e.g., Data Access, Identity, File Storage).
*   **`src/Api`**: The entry point (Presentation Layer), responsible for handling HTTP requests, dependency injection configuration, and middleware pipelines.

## 🔧 Tech Stack

*   **Framework**: .NET 10.0 (LTS)
*   **Platform**: ASP.NET Core Web API
*   **Language**: C#
*   **Authentication**: JWT (JSON Web Tokens) with Bearer Schema
*   **Authorization**: Role-Based Access Control (RBAC)

## 🚀 Getting Started

### Prerequisites

*   .NET 10.0 SDK
*   IDE: Visual Studio 2022 / JetBrains Rider / VS Code

### Installation & Execution

1.  **Clone the repository**
    ```bash
    git clone https://github.com/your-username/TaskAndDocumentManager.git
    cd TaskAndDocumentManager
    ```

2.  **Restore dependencies**
    ```bash
    dotnet restore
    ```

3.  **Run the API**
    Navigate to the API project and run:
    ```bash
    cd src/Api
    dotnet run
    ```
    The API will initialize on `https://localhost:7001` (default).

## 🔐 Security & Identity Management

The application implements a stateless authentication mechanism using JWTs.

### Identity Model
*   **Identifier**: `Guid` (UUID v4)
*   **Constraints**: Registration is restricted to emails ending in `.com`.
*   **Lifecycle**: Accounts utilize a state machine (`Active` | `Disabled`).

### Authorization Strategy
Access control is enforced via Claims-based authorization.

*   **Roles**:
    *   `USER`: Standard access scope.
    *   `ADMIN`: Elevated privileges (User status management).

### Token Specification
*   **Format**: JWT (RFC 7519)
*   **Transport**: `Authorization: Bearer <token>` header.
*   **Payload Claims**:
    *   `sub` / `UserId`: User GUID
    *   `email`: User Email
    *   `role`: Assigned Role
    *   `status`: Account Status

## 📡 USER API Endpoints

| Method | Endpoint                     | Access Level | Description                                      |
| :----- | :--------------------------- | :----------- | :----------------------------------------------- |
| `POST` | `/api/auth/register`         | Public       | Registers a new `USER` (Default status: `Active`).|
| `POST` | `/api/auth/login`            | Public       | Authenticates credentials; issues JWT.           |
| `GET`  | `/api/users/me`              | Authenticated| Retrieves context for the current identity.      |
| `PUT`  | `/api/users/{id}/status`     | `ADMIN`      | Modifies user account status.                    |



### Task Domain Model
*   **Identifier**: `Guid` (UUID v4)
*   **Validation**: Enforces max lengths for `Title` (200) and `Description` (4000).
*   **Lifecycle**: Tracks assignment, updates, and completion timestamps.
*   **Constraints**: Completed tasks are immutable (cannot be updated or re-assigned).

## TASK API Endpoints
The Task API module supports task creation, retrieval, updating, deletion, and assignment.

#### Task Management Endpoints
| Method   | Endpoint            | Description                                 |
| :------- | :------------------ | :------------------------------------------ |
| `POST`   | `/api/tasks`        | Creates a new task.                        |
| `GET`    | `/api/tasks`        | Retrieves all tasks.                        |
| `PUT`    | `/api/tasks/{id}`   | Updates a specific task by ID.              |
| `DELETE` | `/api/tasks/{id}`   | Deletes a specific task by ID.              |
| `POST`   | `/api/tasks/{id}/assign` | Assigns a task to a user, specified by task ID. |

## 🤝 Contribution

Pull requests are welcome. Please adhere to the existing code style and ensure all unit tests pass before submitting.

## 📄 License

Distributed under the MIT License.
