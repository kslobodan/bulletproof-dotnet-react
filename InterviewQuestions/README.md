# Interview Questions & Answers

**Purpose**: Common interview questions about this project, organized by development day.

**How to Use**: Review these before interviews to explain your technical decisions confidently.

---

## 📁 Organization by Day

- [Day 1: Project Setup & Architecture](./Day1-ProjectSetup.md)
- [Day 2: Core Infrastructure & Multi-tenancy](./Day2-Infrastructure.md)
- [Day 3: Authentication & Authorization](./Day3-Authentication.md)
- [Day 4: Resources CRUD & Multi-Tenant Isolation](./Day4-Resources.md)
- [Day 5: Bookings CRUD & Business Logic](./Day5-Bookings.md)
- [Day 6: Advanced Features & Audit Logging](./Day6-Advanced.md)
- [Day 7: Backend Testing](./Day7-Testing.md)
- [Day 8: Frontend Setup (React + TypeScript + Vite)](./Day8-Frontend.md)

---

## 💡 Interview Tips

**Before the interview:**

1. Review questions for the days you've completed
2. Practice explaining technical decisions out loud
3. Be ready to draw architecture diagrams
4. Know your trade-offs and why you chose them

**During the interview:**

- Lead with the "why" before the "how"
- Use this project as examples when answering behavioral questions
- Be honest about what you'd improve for production
- Show your thought process, not just the solution

---

## 🎯 Key Topics Covered

### Architecture & Design

- Clean Architecture principles
- CQRS pattern with MediatR
- Repository pattern with Dapper
- Multi-tenant data isolation
- MediatR Pipeline Behaviors for cross-cutting concerns

### Backend Technologies

- .NET 9.0 Web API
- PostgreSQL 16 with Docker
- JWT authentication with RefreshToken mechanism
- BCrypt password hashing
- FluentValidation
- Serilog structured logging
- Dapper for data access

### Advanced Features

- Audit logging with MediatR pipeline
- AvailabilityRules with TIME data types
- Soft delete pattern (IsDeleted/DeletedAt)
- Rate limiting (AspNetCoreRateLimit)
- RefreshToken rotation for secure sessions
- Statistics endpoints with SQL aggregation
- Dynamic WHERE clause building

### DevOps & Tools

- Docker & docker-compose
- DbUp database migrations
- API versioning
- Swagger/OpenAPI documentation

### Testing

- xUnit testing framework
- Unit testing (Domain entities, CQRS handlers, FluentValidation validators)
- Integration testing with Testcontainers (PostgreSQL)
- Architecture tests with NetArchTest.Rules
- AAA pattern (Arrange-Act-Assert)
- Mocking with Moq
- FluentAssertions for readable test assertions
- Code coverage measurement with Coverlet
- Multi-tenant isolation testing
- Test organization and naming conventions

### Frontend Technologies

- React 19 with TypeScript
- Vite 8 (fast dev server, HMR, optimized builds)
- TailwindCSS 4 (utility-first CSS)
- Redux Toolkit 2 (state management)
- React Router 7 (client-side routing)
- Axios (HTTP client with interceptors)
- shadcn/ui (copy-paste component library)
- ESM modules and modern JavaScript
- Path aliases and TypeScript strict mode

### Security

- Multi-tenant isolation (tenant-scoped queries)
- Role-based authorization (TenantAdmin, Manager, User)
- Token rotation for refresh tokens
- Rate limiting for DoS protection
- SQL injection prevention (parameterized queries)
- Password security best practices (BCrypt)
