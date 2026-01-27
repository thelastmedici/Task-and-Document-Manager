# TaskAndDocumentManager

A web application for managing tasks and documents, built with ASP.NET Core MVC.

## 🚀 Overview

TaskAndDocumentManager is a .NET 10.0 MVC application designed to streamline the process of tracking tasks and managing associated documents. It leverages the power of ASP.NET Core for the backend.

## 🛠 Tech Stack

- **Framework:** [.NET 10.0](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- **Architecture:** ASP.NET Core MVC
- **Language:** C#

## 📦 Prerequisites

Before you begin, ensure you have the following installed:
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- An IDE like [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

## 🏃‍♂️ Getting Started

1.  **Clone the repository:**
    ```bash
    git clone <repository-url>
    ```

2.  **Navigate to the project directory:**
    ```bash
    cd TaskAndDocumentManager
    ```

3.  **Restore dependencies:**
    ```bash
    dotnet restore
    ```

4.  **Run the application:**
    ```bash
    dotnet run --project TaskAndDocumentManager
    ```

5.  **Open in Browser:**
    Navigate to `https://localhost:7001` (or the port displayed in your terminal) to view the application.

## 📂 Project Structure

- `Controllers/`: Handles user interaction and application logic.
- `Models/`: Domain data objects.
- `Views/`: UI templates using Razor syntax.
- `wwwroot/`: Static assets (CSS, JavaScript, libraries).

## 🤝 Contributing

Contributions are welcome! Please fork the repository and create a pull request with your changes.

## 📄 License

This project is licensed under the MIT License.

## 🔐 Authentication & Authorization Contract

This section outlines the security architecture and user management decisions for the application.

### 1. User Identity & Management
- **Unique Identifier:** Users are identified by their  valid email address.
- **Primary Key:** A globally unique identifier (GUID) serves as the primary ID.
- **Account Status:** Users can be `Active` or `Disabled`. Status management is controlled by Administrators.

### 2. Roles & Permissions
- **User:** Standard access privileges.
- **Admin:** Superuser privileges, including user status management.

### 3. Authentication Flow
- **Login:** Clients must provide a valid email and password.
- **Token:** Successful authentication produces a JSON Web Token (JWT) access token.
- **Authorization:** The frontend sends the token via the `Authorization: Bearer <token>` header. The backend validates the token and applies authorization rules based on the user's role.

### 4. Authentication Use Cases

1.  **Register User**
    -   **Input:** Email, Password.
    -   **Logic:** Validates email and password.
    -   **Output:** Creates a new user with status `Active`.

2.  **Authenticate User**
    -   **Input:** Email, Password.
    -   **Logic:** Checks if user exists, is active, and password matches.
    -   **Output:** Produces a JWT token with user info.

3.  **Get Current User**
    -   **Input:** Valid JWT token.
    -   **Output:** Returns User ID, Email, Role (`User` or `Admin`), and Status (`Active` or `Disabled`).

4.  **Manage User Status** (Admin Only)
    -   **Input:** Target User ID, New Status (`Active` or `Disabled`).
    -   **Logic:** Requires `Admin` role. Updates the target user's status.