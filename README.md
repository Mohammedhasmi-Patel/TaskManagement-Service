# TaskManagement API

> Clean Architecture .NET 10 Web API powering the TaskManagement platform — built to streamline daily to-do tasks, project hierarchies, and work session details.

---

## 💡 Motivation & Overview

Standard to-do and project management tools often force rigid workflows:
- Simple tasks lack crucial organization (company, client projects, technologies).
- Tracking how much time is actually spent working on tasks is either absent or overly complex.
- Recording retroactive work (tasks completed in the past) or stopping active timers with reason descriptions is rarely supported cleanly.

This backend provides the robust, domain-driven engine for **TaskManagement**, featuring:
- **JWT Bearer Authentication** with secure password hashing
- **Hierarchical Domain Entities**: Companies → Projects → Tasks → Associated Technologies
- **Status Lifecycle & Transitions**: `Todo`, `InProgress`, `InReview`, `Completed`, `Blocked`, `Cancelled`
- **Flexible Time Tracking**: Active live timers, retroactive completed sessions, duration calculations, and stop descriptions
- **Date Range Querying & Aggregations**: Query and filter by date windows (Today, Week, Month, Year, Custom)

---

## 🛠️ Architecture & Tech Stack

- **Framework**: [.NET 10 Web API](https://dotnet.microsoft.com/)
- **Pattern**: Clean Architecture (Domain, Application, Infrastructure, API)
- **Database / ORM**: PostgreSQL / In-Memory with Entity Framework Core
- **Validation**: FluentValidation pipeline behaviors
- **Logging**: Serilog structured logging
- **API Documentation**: OpenAPI / Swagger UI
- **Testing**: xUnit, FluentAssertions, Moq, Custom In-Memory Test Fixtures

---

## 🚀 Getting Started

### 1. Restore & Build
```bash
dotnet restore
dotnet build
```

### 2. Run Test Suite
```bash
dotnet test -c Release
```
*(Runs 161 Unit Tests and 67 Integration Tests)*

### 3. Run the API
```bash
dotnet run --project src/TaskManagement.API
```

- **Swagger UI**: Navigate to `https://localhost:59131/swagger` (or `http://localhost:5000/swagger`)
- **Health Check**: Navigate to `https://localhost:59131/health`

---

## 🗄️ Database Setup & Migrations

### Apply Migrations
```bash
dotnet ef database update \
  --project src/TaskManagement.Infrastructure \
  --startup-project src/TaskManagement.API
```

### Add New Migration
```bash
dotnet ef migrations add <MigrationName> \
  --project src/TaskManagement.Infrastructure \
  --startup-project src/TaskManagement.API
```

---

## ⚙️ Configuration

Configure `src/TaskManagement.API/appsettings.json` or use environment variables:

- `ConnectionStrings:DefaultConnection` — PostgreSQL connection string
- `Database:Provider` — `PostgreSql` or `SqlServer`
- `Jwt:SecretKey` — Secret key for signing tokens (minimum 32 characters)
- `Jwt:Issuer` & `Jwt:Audience` — Token metadata
- `Cors:AllowedOrigins` — Array of allowed frontend origins (e.g. `http://localhost:3000`)

---

## 📝 License

This project is licensed under the MIT License.
