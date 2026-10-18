# Personal Expense Tracker System

A beginner-friendly, production-grade **Personal Expense Tracker System** built with **ASP.NET Core MVC (C#)**, **Razor Views**, **Entity Framework Core (EF Core)**, **Microsoft SQL Server**, and **genuine JWT Bearer Authentication**.

---

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [End-to-End Request Flow](#end-to-end-request-flow)
3. [MVC Explained for Junior Developers](#mvc-explained-for-junior-developers)
4. [JWT Authentication Deep Dive](#jwt-authentication-deep-dive)
5. [Dependency Injection & Service Layer](#dependency-injection--service-layer)
6. [EF Core, DbContext & Database Migrations](#ef-core-dbcontext--database-migrations)
7. [API Endpoints Reference](#api-endpoints-reference)
8. [Setup & Running Instructions](#setup--running-instructions)
9. [Project File Structure](#project-file-structure)
10. [Junior .NET Developer Interview Q&A](#junior-net-developer-interview-qa)

---

## Architecture Overview

This project implements a hybrid architecture:
- **Presentation Layer**: ASP.NET Core MVC serving dynamic Razor Views styled with Bootstrap 5.
- **API Component**: RESTful endpoints protected with `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`.
- **Business Logic Layer**: Injected Scoped Services (`IExpenseService`, `IAuthService`).
- **Data Access Layer**: EF Core `ApplicationDbContext` communicating with Microsoft SQL Server.

```mermaid
graph TD
    User["User Browser / Client"] -->|1. HTTP GET /Expense/Index| MC["MVC Controller (ExpenseController)"]
    MC -->|2. Renders Razor View| RV["Razor View (Index.cshtml)"]
    RV -->|3. Browser loads auth.js & expense.js| JS["Client-side JavaScript"]
    JS -->|4. fetch('/api/expenses') with Bearer Token| AC["API Controller (ExpensesApiController)"]
    AC -->|5. Validates JWT & Claims| MW["JWT Bearer Middleware"]
    MW -->|6. Injects current UserId| ES["ExpenseService (IExpenseService)"]
    ES -->|7. LINQ Query: Where(e => e.UserId == currentUserId)| EF["EF Core DbContext (ApplicationDbContext)"]
    EF -->|8. SQL SELECT ... FROM Expenses WHERE UserId = @id| DB[("Microsoft SQL Server")]
    DB -->|9. Returns Rows| EF
    EF -->|10. Entity to DTO Mapping| ES
    ES -->|11. ExpenseResponseDto List| AC
    AC -->|12. JSON Response 200 OK| JS
    JS -->|13. Updates DOM (Table, Badges, Total)| User
```

---

## End-to-End Request Flow

The complete journey of an HTTP request:

```
Browser / View 
    └──► Controller (receives HTTP request, validates ModelState)
          └──► Service (business rules & user isolation)
                └──► EF Core DbContext (translates LINQ to SQL)
                      └──► SQL Server (executes query, returns records)
```

1. **Client Action**: The user opens the Dashboard or submits an expense form.
2. **API Controller**: The controller action is decorated with `[Authorize]`. The framework checks the incoming HTTP `Authorization: Bearer <token>` header.
3. **Claims Extraction**: The authenticated user's ID is extracted safely from `User.FindFirst(ClaimTypes.NameIdentifier)`.
4. **Service Execution**: The controller delegates work to `IExpenseService.GetExpensesByUserAsync(userId, category)`.
5. **EF Core Translation**: EF Core translates the LINQ query `.Where(e => e.UserId == userId)` into an optimized SQL command with parameterized values (preventing SQL injection).
6. **SQL Server Execution**: SQL Server executes the query and returns the matching data.
7. **DTO Transformation**: Entities are projected into DTOs (`ExpenseResponseDto`) so sensitive database details are never leaked.
8. **HTTP Response**: The controller returns `200 OK` (or `201 Created`, `400 Bad Request`, `404 Not Found`).

---

## MVC Explained for Junior Developers

MVC stands for **Model-View-Controller**, an architectural pattern that separates an application into three interconnected parts:

1. **Model** (`Models/User.cs`, `Models/Expense.cs`):
   - Represents the data and schema of the system.
   - Contains business entity definitions, validation rules (e.g., `[Required]`, `[Range]`), and database mapping configurations.

2. **View** (`Views/Expense/Index.cshtml`, `Views/Account/Login.cshtml`):
   - The user interface that renders data on the screen.
   - Built using Razor (`.cshtml`), combining HTML with C# syntax (`@`).
   - Does not perform database operations directly.

3. **Controller** (`Controllers/AccountController.cs`, `Controllers/ExpenseController.cs`):
   - The coordinator and traffic controller.
   - Accepts incoming HTTP requests, coordinates with services, and decides whether to return a View or JSON data.

---

## JWT Authentication Deep Dive

### What is a JSON Web Token (JWT)?
A JWT is a compact, URL-safe means of representing claims to be transferred between two parties. It is composed of three parts separated by dots (`.`):
```
header.payload.signature
```

1. **Header**: Contains the token type (`JWT`) and signing algorithm (`HS256`).
2. **Payload (Claims)**: Contains claims about the user:
   - `sub / NameIdentifier`: User's primary key (`user.Id`).
   - `name`: User's username (`user.Username`).
   - `email`: User's email address (`user.Email`).
   - `jti`: Unique token identifier (`Guid.NewGuid()`).
   - `exp`: Expiration timestamp in epoch seconds.
3. **Signature**: Cryptographic hash created by signing `HMACSHA256(Base64(header) + "." + Base64(payload), secretKey)`.

### How Token Generation Works in `AuthService.cs`
```csharp
var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Name, user.Username),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
};

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

var tokenDescriptor = new SecurityTokenDescriptor
{
    Subject = new ClaimsIdentity(claims),
    Expires = DateTime.UtcNow.AddMinutes(120),
    Issuer = _configuration["Jwt:Issuer"],
    Audience = _configuration["Jwt:Audience"],
    SigningCredentials = credentials
};
```

### How the MVC Interface Communicates with the Protected API
1. **Login (`/Account/Login`)**: User submits credentials to `/api/auth/login`. On validation, the API responds with `{ token: "..." }`.
2. **Storage**: The token is stored in browser `sessionStorage`:
   ```javascript
   sessionStorage.setItem("expense_tracker_jwt", token);
   ```
   *Why `sessionStorage`?* Unlike `localStorage`, `sessionStorage` is automatically wiped when the user closes their browser tab or window, preventing credential residue on shared or school computers.
3. **Supplying the Token**: Whenever an API request is made:
   ```javascript
   fetch("/api/expenses", {
       headers: {
           "Content-Type": "application/json",
           "Authorization": "Bearer " + sessionStorage.getItem("expense_tracker_jwt")
       }
   });
   ```
4. **Validation & Pipeline**: In `Program.cs`, `AddJwtBearer` checks the signature using the server's private secret key. If valid, it populates `HttpContext.User`. If missing or invalid, it immediately halts the request with `401 Unauthorized`.
5. **Expiration Handling**: If an API call receives a `401`, `auth.js` automatically clears the session and redirects the user to `/Account/Login`.

---

## Dependency Injection & Service Layer

### What is Dependency Injection (DI)?
Dependency Injection is a software design pattern where objects receive their dependencies from an external assembler (ASP.NET Core's built-in IoC container) rather than hardcoding `new MyService()` inside each controller.

### Why Use an Interface (`IExpenseService`)?
1. **Decoupling**: The controller depends on an abstraction, not a concrete implementation.
2. **Testability**: In unit tests, we can supply a mock implementation (`Mock<IExpenseService>`) without needing an active SQL database.
3. **Maintainability**: Business logic can be changed without modifying controller action signatures.

### Service Lifetimes in ASP.NET Core
In `Program.cs`, we register:
```csharp
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IAuthService, AuthService>();
```
- **Scoped (`AddScoped`)**: A single instance is created once per HTTP request and shared across all classes participating in that request. This is the recommended lifetime for EF Core `DbContext` and business services.
- **Transient (`AddTransient`)**: A new instance is instantiated every single time it is requested.
- **Singleton (`AddSingleton`)**: A single instance is created once at application startup and shared across all incoming requests throughout the application lifetime.

---

## EF Core, DbContext & Database Migrations

- **DbContext** (`ApplicationDbContext`): The primary class that coordinates Entity Framework functionality for data models. It represents a session with the database.
- **DbSet** (`DbSet<Expense>`): Represents a collection of entities that can be queried with LINQ or saved back to the database table.
- **Migrations**: Code-first migrations let developers modify C# entity models and automatically generate SQL scripts that keep the database schema synchronized.
- **Multi-Tenant User Isolation**: Every LINQ query executed by `ExpenseService` includes `.Where(e => e.UserId == userId)`. This ensures that even if user A knows user B's expense ID, user A cannot read, edit, or delete it (returns `404 Not Found`).

---

## API Endpoints Reference

### Authentication Endpoints (`/api/auth`)

| Method | Endpoint | Access | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | Public | Registers a new user, hashes password, returns JWT |
| `POST` | `/api/auth/login` | Public | Validates credentials, issues JWT token |
| `GET` | `/api/auth/me` | Protected (JWT) | Validates token and returns current user claims |

### Expense Management Endpoints (`/api/expenses`)

| Method | Endpoint | Access | Description |
|---|---|---|---|
| `GET` | `/api/expenses` | Protected (JWT) | Retrieves all expenses for authenticated user |
| `GET` | `/api/expenses?category={name}` | Protected (JWT) | Retrieves expenses filtered by category |
| `GET` | `/api/expenses/{id}` | Protected (JWT) | Retrieves single expense by ID (user isolated) |
| `POST` | `/api/expenses` | Protected (JWT) | Creates new expense for authenticated user |
| `PUT` | `/api/expenses/{id}` | Protected (JWT) | Updates expense details (user isolated) |
| `DELETE` | `/api/expenses/{id}` | Protected (JWT) | Deletes expense record (user isolated) |
| `GET` | `/api/expenses/summary` | Protected (JWT) | Returns total spent, count, monthly sum, breakdown |
| `GET` | `/api/expenses/categories` | Public | Returns predefined category list |

---

## Setup & Running Instructions

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server (`MSSQLSERVER` or LocalDB)
- EF Core CLI tool (`dotnet tool install --global dotnet-ef`)

### Configuration
Open `appsettings.json` and verify the SQL Server connection string:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=ExpenseTrackerDb;Integrated Security=True;TrustServerCertificate=True;"
}
```

### Commands to Run the Application
1. **Restore dependencies**:
   ```bash
   dotnet restore
   ```
2. **Apply Database Migrations**:
   ```bash
   dotnet ef database update
   ```
3. **Run the Application**:
   ```bash
   dotnet run
   ```
4. **Open in Browser**:
   Navigate to `http://localhost:5247` (or the port displayed in your terminal).

### Default Demo Account
The database automatically seeds a demo account on startup:
- **Username**: `demouser`
- **Password**: `Password123!`
- **Sample Data**: 5 pre-populated transactions across groceries, transportation, utilities, and entertainment.

---

## Project File Structure

```
PersonalExpenseTracker/
├── Controllers/
│   ├── Api/
│   │   ├── AuthApiController.cs        # Register, Login, Me endpoints
│   │   └── ExpensesApiController.cs    # JWT-secured CRUD & summary endpoints
│   ├── AccountController.cs            # Serves Login & Register Razor views
│   ├── ExpenseController.cs            # Serves Dashboard, Create, Edit Razor views
│   └── HomeController.cs               # Serves Landing welcome view
├── Data/
│   ├── ApplicationDbContext.cs         # EF Core DbContext with User & Expense DbSets
│   └── DbInitializer.cs                # Automatic migration and seed helper
├── DTOs/
│   ├── AuthDtos.cs                     # RegisterDto, LoginDto, AuthResponseDto
│   └── ExpenseDtos.cs                  # Create, Update, Response, Summary DTOs
├── Models/
│   ├── User.cs                         # User entity with navigation to Expenses
│   └── Expense.cs                      # Expense entity with decimal precision & foreign key
├── Services/
│   ├── IAuthService.cs & AuthService.cs       # Password hashing & JWT token issuance
│   └── IExpenseService.cs & ExpenseService.cs # Expense logic & multi-tenant isolation
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml                # Sign-in form calling Auth API
│   │   └── Register.cshtml             # Sign-up form calling Auth API
│   ├── Expense/
│   │   ├── Index.cshtml                # Dashboard: stats cards, category filter, expense table
│   │   ├── Create.cshtml               # Dedicated Add Expense form
│   │   └── Edit.cshtml                 # Dedicated Edit Expense form
│   ├── Home/
│   │   └── Index.cshtml                # Landing overview page
│   ├── Shared/
│   │   ├── _Layout.cshtml              # Bootstrap 5 navbar with dynamic auth status
│   │   └── _ValidationScriptsPartial.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
├── wwwroot/
│   ├── css/site.css                    # Professional styles for cards, tables, badges
│   └── js/
│       ├── auth.js                     # JWT storage, token validation, auth headers
│       └── expense.js                  # Fetching expenses, filters, delete, summary cards
├── appsettings.json                    # ConnectionStrings & JWT secret key configuration
├── Program.cs                          # Dependency injection, JWT setup, middleware pipeline
└── README.md                           # Documentation & interview guide
```

---

## Junior .NET Developer Interview Q&A

### Q1: Why do we use DTOs (Data Transfer Objects) instead of returning EF Core entity models directly?
**Answer**:
1. **Security (Over-posting)**: Exposing entity models directly allows malicious users to modify properties they shouldn't (e.g., `UserId` or `IsAdmin`).
2. **Preventing Circular References**: EF Core entities often contain navigation properties (e.g., `User` has `List<Expense>`, and `Expense` has `User`). Serializing this to JSON causes infinite recursion exceptions.
3. **Decoupling**: The internal database schema can evolve independently without breaking clients that consume the API.

### Q2: How does the server know which user is making a request with a JWT?
**Answer**:
When the JWT is generated, we embed user identifiers as **Claims** (specifically `ClaimTypes.NameIdentifier` with the user's ID). When a protected request arrives, the `AddJwtBearer` middleware validates the signature and reconstructs `HttpContext.User`. In our controller, we call `User.FindFirst(ClaimTypes.NameIdentifier)` to extract the authenticated user ID.

### Q3: Why is password hashing important, and why shouldn't we use plain MD5 or SHA256?
**Answer**:
Plain MD5 or SHA256 are fast mathematical hashing algorithms. Attackers using modern GPUs can compute billions of MD5 hashes per second using precomputed rainbow tables. In our project, we use `PasswordHasher<User>`, which utilizes **PBKDF2** (Password-Based Key Derivation Function 2) with a unique cryptographically random salt and thousands of iteration rounds, making brute-force and dictionary attacks computationally infeasible.

### Q4: What is the difference between `AddScoped`, `AddTransient`, and `AddSingleton`?
**Answer**:
- **Transient**: Created each time requested. Useful for lightweight, stateless services.
- **Scoped**: Created once per HTTP request lifecycle. Best for EF Core `DbContext` and services that perform unit-of-work operations.
- **Singleton**: Created once when the app starts and reused everywhere. Best for caching or application-wide configuration.

### Q5: How do you prevent User A from deleting User B's expenses?
**Answer**:
We enforce **multi-tenant data isolation** at the service layer:
```csharp
var expense = await _context.Expenses
    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
if (expense == null) return false; // Returns 404 Not Found
```
Because the query filters by both `Id` AND the authenticated `UserId`, an unauthorized user's query returns null, effectively preventing data leakage or tampering.
