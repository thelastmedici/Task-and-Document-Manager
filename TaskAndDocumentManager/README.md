# TaskAndDocumentManager

A web application for managing tasks and documents, built with ASP.NET Core.

## 🚀 Overview

TaskAndDocumentManager is a robust .NET application designed to streamline task tracking and document management. It features a secure, role-based API for handling user authentication and data access.

## 🛠 Tech Stack

- **Framework:** [.NET 10.0 (LTS)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- **Architecture:** ASP.NET Core Web API
- **Language:** C#

## 📦 Prerequisites

Before you begin, ensure you have the following installed:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An IDE like [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

## 🏃‍♂️ Getting Started

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/your-username/TaskAndDocumentManager.git
    ```
    *(Note: Replace with the actual repository URL)*

2.  **Navigate to the project directory:**
    ```bash
    cd TaskAndDocumentManager/src/Api
    ```

3.  **Restore dependencies:**
    ```bash
    dotnet restore
    ```

4.  **Run the application:**
    ```bash
    dotnet run
    ```

5.  **Open in Browser:**
    The API will be available at `https://localhost:7001` (or the port specified in your terminal).

## 📂 Project Structure

The project is structured to follow Clean Architecture principles, promoting separation of concerns.

- `src/`
    - `Api/`: The main ASP.NET Core project, containing controllers and API endpoints.
    - `Application/`: Core application logic, services, and use cases.
    - `Domain/`: Business models, entities, and domain-specific logic.
    - `Infrastructure/`: Data access, external services, and other implementation details.
- `TaskAndDocumentManager.sln`: The Visual Studio solution file.

## 🤝 Contributing

Contributions are welcome! Please fork the repository and create a pull request with your changes.

## 📄 License

This project is licensed under the **MIT License**.

## 🔐 Authentication & Authorization

This section outlines the application's security model, including user identity, roles, and the authentication process.

### User Identity
- **Primary Identifier**: Each user is uniquely identified by a **GUID** (`UserId`).
- **Login Credential**: Users authenticate using their email address and password.
- **Email Constraint**: User email addresses must end with `.com`.
- **Account Status**: Each user account has a status of either `Active` or `Disabled`.

### Roles and Permissions
The application defines two user roles:
- **`USER`**: Standard users with access to core task and document management features.
- **`ADMIN`**: Administrators with elevated privileges, including the ability to manage user accounts (e.g., changing a user's status).

### Authentication and Token Handling
- **Protocol**: The application uses a token-based authentication system.
- **Token Type**: A **JSON Web Token (JWT)** is issued upon successful authentication. No refresh tokens are used.
- **Authorization Header**: For authenticated requests, the JWT must be included in the `Authorization` header using the Bearer schema: `Authorization: Bearer <token>`.
- **Token Claims**: The JWT payload contains the following claims to identify and authorize the user:
    - `UserId` (GUID)
    - `Email`
    - `Role` (`USER` or `ADMIN`)
    - `Status` (`Active` or `Disabled`)

### API Use Cases

| Use Case             | Endpoint (Example)           | Input                  | Role Required      | Description                                                              |
| :------------------- | :--------------------------- | :--------------------- | :----------------- | :----------------------------------------------------------------------- |
| **Register User**      | `POST /api/auth/register`    | Email, Password        | Public             | Creates a new user with the `USER` role and an `Active` status.          |
| **Authenticate User**  | `POST /api/auth/login`       | Email, Password        | Public             | Validates credentials and returns a JWT if the user is `Active`.         |
| **Get Current User**   | `GET /api/users/me`          | Valid JWT              | `USER` or `ADMIN`  | Returns the profile of the currently authenticated user.                 |
| **Manage User Status** | `PUT /api/users/{id}/status` | New Status             | `ADMIN`            | Updates a user's status to `Active` or `Disabled`.                       |