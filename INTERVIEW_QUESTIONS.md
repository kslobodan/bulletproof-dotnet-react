# Interview Questions & Answers

**Purpose**: Common interview questions about this project, organized by development day.

**How to Use**: Review these before interviews to explain your technical decisions confidently.

---

## Day 1: Project Setup & Architecture

### Q: "Why did you choose Clean Architecture for this project?"

**A:** "I chose Clean Architecture because it provides clear separation of concerns and makes the codebase maintainable and testable. The dependency rule—where inner layers have no knowledge of outer layers—means my business logic (Domain) is completely independent of frameworks or databases. This makes it easy to swap implementations, like changing from Dapper to Entity Framework, without touching business logic. It also makes testing easier since I can mock dependencies at layer boundaries."

---

### Q: "Why did you use Docker and PostgreSQL?"

**A:** "I chose **PostgreSQL** because it's enterprise-grade, open-source, and has advanced features like JSONB and row-level security—perfect for multi-tenant architecture.

I used **Docker** for consistency and portability. The same `docker-compose.yml` works in development, CI/CD, and production. It eliminates 'works on my machine' issues and lets anyone clone the repo and start the database with one command.

This mirrors real-world microservices architecture where each service runs in containers orchestrated by Kubernetes or Docker Swarm."

---

### Q: "Explain the dependency flow in Clean Architecture."

**A:** "The dependency flow is: API → Infrastructure → Application → Domain.

- **Domain** has zero dependencies—it's pure business entities and rules
- **Application** depends only on Domain—it contains business logic and CQRS handlers
- **Infrastructure** depends on Application—it implements interfaces like repositories
- **API** depends on everything—it's the entry point that wires up dependency injection

This ensures the business logic is protected from framework changes and external dependencies."

---

### Q: "Why use VS Code instead of Visual Studio?"

**A:** "I use VS Code with C# Dev Kit for modern .NET development because it's lightweight, cross-platform, and encourages a CLI-first workflow. This gives me deeper understanding of what's happening under the hood rather than relying on GUI wizards. It's also the industry trend—most modern teams use VS Code for microservices and containerized applications. Plus, my workflow is portable across Windows, Linux, and Mac."

---

### Q: "What's the difference between dotnet new classlib and dotnet new webapi?"

**A:** "Both create .NET projects, but:

- **`dotnet new classlib`** creates a class library—just .NET code that compiles to a DLL. It's not executable on its own. I use this for Domain, Application, and Infrastructure layers.
- **`dotnet new webapi`** creates an ASP.NET Core Web API project—an executable application with controllers, middleware, and a web server. This is my API layer entry point.

In Clean Architecture, only the API layer is executable; the others are libraries it depends on."

---

## Day 2: Core Infrastructure & Multi-tenancy

(To be filled as we progress)

---

## Day 3: Authentication & Authorization

(To be filled as we progress)

---

## Day 4: Resources CRUD

(To be filled as we progress)

---

## Day 5: Bookings CRUD & Business Logic

(To be filled as we progress)

---

## Day 6: Advanced Features & Audit Logging

(To be filled as we progress)

---

## Day 7: Backend Testing

(To be filled as we progress)

---

## General Architecture Questions

### Q: "How would you scale this application?"

**A:** (To be filled after understanding the full architecture)

---

### Q: "How do you ensure tenant data isolation?"

**A:** (To be filled on Day 2 when implementing multi-tenancy)

---

### Q: "Explain your testing strategy."

**A:** (To be filled on Day 7 when implementing tests)

---

## Technical Deep Dives

(Add specific technical questions as they come up during development)
