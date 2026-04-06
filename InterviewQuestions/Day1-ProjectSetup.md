# Day 1: Project Setup & Architecture

[← Back to Index](./README.md)

---

## Q: "Why did you choose Clean Architecture for this project?"

**A:** "I chose Clean Architecture because it provides clear separation of concerns and makes the codebase maintainable and testable. The dependency rule—where inner layers have no knowledge of outer layers—means my business logic (Domain) is completely independent of frameworks or databases.

**In detail:** Clean Architecture is a software design pattern created by Robert C. Martin (Uncle Bob) that organizes code into layers with strict dependency rules, making applications maintainable, testable, and independent of frameworks, databases, and UI.

### Core Principle: The Dependency Rule

**Dependencies point inward only** - outer layers can depend on inner layers, but inner layers cannot depend on outer layers.

```
┌─────────────────────────────────────┐
│         API Layer (Web)             │ ← Outermost
├─────────────────────────────────────┤
│    Infrastructure (Repositories)    │
├─────────────────────────────────────┤
│   Application (Business Logic)      │
├─────────────────────────────────────┤
│      Domain (Entities/Rules)        │ ← Innermost (no dependencies)
└─────────────────────────────────────┘
```

### The 4 Layers in My Project

**1. Domain Layer (Core/Innermost)**

- **What**: Entities, business rules, enums
- **Dependencies**: NONE (pure C# classes)
- **Example**: `Booking`, `Resource`, `User`, `BookingStatus`
- **Why pure**: Business logic shouldn't care about databases or frameworks

**2. Application Layer (Use Cases)**

- **What**: CQRS commands/queries, handlers, validators, DTOs, interfaces
- **Dependencies**: Only Domain
- **Example**: `CreateBookingCommand`, `IResourceRepository`, `BookingValidator`
- **Why**: Defines 'what the app does' without 'how it's done'

**3. Infrastructure Layer (Implementation Details)**

- **What**: Database access (Dapper), external services, file systems
- **Dependencies**: Application + Domain
- **Example**: `BookingRepository`, `JwtTokenService`, `PasswordHasher`
- **Why**: Keeps technical details replaceable (swap Dapper for EF Core later)

**4. API Layer (Presentation/Entry Point)**

- **What**: Controllers, middleware, Program.cs, HTTP concerns
- **Dependencies**: Application + Infrastructure (wires everything together)
- **Example**: `BookingsController`, `TenantResolutionMiddleware`
- **Why**: UI/API can be replaced (add mobile app, console app, etc.)

### Key Benefits

1. **Testability**: Business logic (Domain/Application) has no framework dependencies → easy unit tests
2. **Flexibility**: Swap PostgreSQL for SQL Server? Only change Infrastructure layer
3. **Independence**: Domain entities don't know about HTTP, databases, or UI
4. **Maintainability**: Changes in one layer don't break others (if dependencies are correct)
5. **Interview Appeal**: Shows understanding of enterprise architecture patterns

### Why Assembly Scanning Matters

My `AssemblyReference.cs` pattern follows Clean Architecture:

- **Application defines contracts** (`IValidator`, `IRequestHandler`, `Profile`)
- **Infrastructure registers implementations** via `typeof(AssemblyReference).Assembly`
- **Keeps API layer thin** - just wires dependencies, doesn't implement logic

### Interview Talking Points

- **'Why not just MVC?'** - Clean Architecture separates concerns. MVC mixes business logic with controllers.
- **'What if requirements change?'** - Swap Dapper for EF Core without touching Domain/Application.
- **'How do you test this?'** - Unit test handlers without databases (mock `IRepository<T>`).
- **'Isn't it over-engineering?'** - For large projects, this prevents 'big ball of mud' code."

---

## Q: "Why did you use Docker and PostgreSQL?"

**A:** "I chose **PostgreSQL** because it's enterprise-grade, open-source, and has advanced features like JSONB and row-level security—perfect for multi-tenant architecture.

I used **Docker** for consistency and portability. The same `docker-compose.yml` works in development, CI/CD, and production. It eliminates 'works on my machine' issues and lets anyone clone the repo and start the database with one command.

This mirrors real-world microservices architecture where each service runs in containers orchestrated by Kubernetes or Docker Swarm."

---

## Q: "Explain the dependency flow in Clean Architecture."

**A:** "The dependency flow is: API → Infrastructure → Application → Domain.

- **Domain** has zero dependencies—it's pure business entities and rules
- **Application** depends only on Domain—it contains business logic and CQRS handlers
- **Infrastructure** depends on Application—it implements interfaces like repositories
- **API** depends on everything—it's the entry point that wires up dependency injection

This ensures the business logic is protected from framework changes and external dependencies."

---

## Q: "Why use VS Code instead of Visual Studio?"

**A:** "I use VS Code with C# Dev Kit for modern .NET development because it's lightweight, cross-platform, and encourages a CLI-first workflow. This gives me deeper understanding of what's happening under the hood rather than relying on GUI wizards. It's also the industry trend—most modern teams use VS Code for microservices and containerized applications. Plus, my workflow is portable across Windows, Linux, and Mac."

---

## Q: "What's the difference between dotnet new classlib and dotnet new webapi?"

**A:** "Both create .NET projects, but:

- **`dotnet new classlib`** creates a class library—just .NET code that compiles to a DLL. It's not executable on its own. I use this for Domain, Application, and Infrastructure layers.
- **`dotnet new webapi`** creates an ASP.NET Core Web API project—an executable application with controllers, middleware, and a web server. This is my API layer entry point.

In Clean Architecture, only the API layer is executable; the others are libraries it depends on."

---

## Q: "Why did you choose Dapper over Entity Framework?"

**A:** "I chose **Dapper** for several reasons:

1. **Performance** - Dapper is much faster than EF Core because it's a micro-ORM with minimal overhead
2. **Control** - I write SQL directly, so I understand exactly what queries hit the database. No hidden SQL generation or N+1 query surprises
3. **Learning** - It keeps my SQL skills sharp, which is valuable for database optimization and troubleshooting
4. **Simplicity** - For this project's size, Dapper's lightweight approach is perfect. No complex migrations or DbContext configuration

However, I acknowledge EF Core's advantages for rapid prototyping and change tracking. For enterprise apps with complex domain models, EF Core might be better. Dapper is ideal when you need performance and control."

---

## Q: "What is CQRS and why use MediatR?"

**A:** "**CQRS** (Command Query Responsibility Segregation) separates read operations (Queries) from write operations (Commands).

**Benefits:**

- **Clarity** - Clear separation between 'what changes data' and 'what reads data'
- **Scalability** - Can optimize read and write paths separately
- **Single Responsibility** - Each handler does one thing

**MediatR** implements the mediator pattern for CQRS:

- Controllers send commands/queries to MediatR: `await mediator.Send(new CreateBookingCommand(...))`
- MediatR finds the appropriate handler and executes it
- This decouples controllers from business logic—controllers become thin orchestrators

Example flow: **Controller → MediatR → UserCommandHandler → Repository → Database**"

---

## Q: "Explain your logging strategy with Serilog."

**A:** "I use **Serilog** for structured logging with multiple sinks:

1. **Console sink** - For development, see logs in real-time while running `dotnet run`
2. **File sink** - For persistence, daily rolling logs (`logs/log-20260403.txt`)

**Why structured logging?** Instead of string concatenation like `$"User {userId} failed"`, I use:

```csharp
Log.Information("User {UserId} login failed", userId);
```

This logs as JSON with properties, making it queryable in production logging systems like Elasticsearch or Seq. I can search 'all failures for UserId=123' instantly.

I also wrap startup in try-catch-finally to log fatal errors: `Log.Fatal(ex, "Application terminated")`"

---

## Q: "What is FluentValidation and why use it?"

**A:** "**FluentValidation** is a validation library that separates validation rules from models.

**Instead of attributes:**

```csharp
public class CreateBookingCommand {
    [Required] public int ResourceId { get; set; }
}
```

**I write validator classes:**

```csharp
public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand> {
    public CreateBookingCommandValidator() {
        RuleFor(x => x.ResourceId).GreaterThan(0);
    }
}
```

**Benefits:**

- **Reusable** - Validators can be tested independently
- **Complex rules** - Can validate against database, check business rules, call services
- **Better errors** - Custom error messages per language/context
- **SOLID** - Separates validation concerns from commands

MediatR + FluentValidation work together: validate before the handler executes."

---

## Q: "Why use AutoMapper?"

**A:** "**AutoMapper** eliminates repetitive DTO-to-Entity mapping code.

**Without AutoMapper:**

```csharp
var entity = new Booking {
    Id = dto.Id,
    ResourceId = dto.ResourceId,
    StartTime = dto.StartTime,
    // ... 15 more lines
};
```

**With AutoMapper:**

```csharp
var entity = _mapper.Map<Booking>(dto);
```

**Benefits:**

- **DRY principle** - Define mappings once, use everywhere
- **Less boilerplate** - Reduces error-prone manual mapping
- **Convention-based** - Auto-maps properties with same names
- **Testable** - Can verify mapping configurations

**Trade-off:** Adds magic/indirection. I use it for simple mappings, but write manual mapping for complex transformations."

---

## Q: "How do you manage database connection strings securely?"

**A:** "I use ASP.NET Core's configuration system with multiple layers:

1. **Development**: `appsettings.Development.json` (gitignored) contains local credentials
2. **Production**: Environment variables or Azure Key Vault for secrets
3. **Connection string** retrieved via `Configuration.GetConnectionString("DefaultConnection")`

**Security:**

- Never commit production credentials to git
- Use Azure Managed Identity or AWS IAM roles in production
- appsettings.Development.json is in .gitignore
- Rotate credentials regularly

For this project, `postgres/postgres` is fine locally, but in production I'd use:

- Separate database user per service
- Least privilege permissions
- Connection string from environment variables"

---

[← Back to Index](./README.md) | [Next: Day 2 →](./Day2-Infrastructure.md)
