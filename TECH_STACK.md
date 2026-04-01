# Bulletproof .NET React - Technology Stack

## Project Overview
**Purpose**: Portfolio/presentation project demonstrating full-stack development expertise  
**Last Updated**: April 2, 2026

---

## Backend Stack (.NET Core)

### Core Framework
- **.NET 8** (LTS) - Latest long-term support version
- **ASP.NET Core Web API** - RESTful API

### Architecture & Patterns
- **Clean Architecture** - Domain-centric layered architecture
  - API Layer (Controllers, Middleware)
  - Application Layer (CQRS, DTOs, Interfaces)
  - Domain Layer (Entities, Value Objects, Domain Events)
  - Infrastructure Layer (Data Access, External Services)
- **CQRS Pattern** - Command Query Responsibility Segregation
- **MediatR** - In-process messaging for CQRS implementation
- **Repository Pattern** - Data access abstraction
- **Unit of Work** - Transaction management

### Database & ORM
- **Database**: [TO BE DECIDED - PostgreSQL or SQL Server]
- **Entity Framework Core 8** - ORM with Code-First approach
- **EF Core Migrations** - Database versioning

### Authentication & Security
- **JWT (JSON Web Tokens)** - Stateless authentication
- **ASP.NET Core Identity** - User management
- **Role-based Authorization** - Access control
- **HTTPS Enforcement** - Secure communication
- **CORS Policy** - Cross-origin configuration

### Validation & Mapping
- **FluentValidation** - Request/command validation
- **AutoMapper** - Object-to-object mapping (DTOs ↔ Entities)

### Logging & Monitoring
- **Serilog** - Structured logging
  - Console sink
  - File sink
  - [Optional: Seq/ELK for production]

### API Documentation
- **Swagger/OpenAPI** - Interactive API documentation
- **Swashbuckle** - Swagger implementation for .NET

### Testing
- **xUnit** - Unit testing framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library
- **Integration Tests** - API endpoint testing

### Additional Libraries
- **Polly** - Resilience and transient fault handling
- **MediatR.Extensions** - Pipeline behaviors (validation, logging)

---

## Frontend Stack (React)

### Core Framework
- **React 18** - Latest React with concurrent features
- **TypeScript** - Type-safe JavaScript
- **Vite** - Fast build tool and dev server

### Routing
- **React Router v6** - Client-side routing

### State Management
- **TanStack Query (React Query)** - Server state management (caching, synchronization)
- **[TO BE DECIDED]**: 
  - Zustand (lightweight, minimal boilerplate) OR
  - Redux Toolkit (enterprise standard, more boilerplate)

### HTTP Client
- **Axios** - HTTP requests with interceptors
  - Request/Response interceptors
  - JWT token management
  - Error handling

### Forms & Validation
- **React Hook Form** - Performant form handling
- **Zod** - TypeScript-first schema validation
- **React Hook Form + Zod integration**

### UI Framework
- **[TO BE DECIDED]**:
  - TailwindCSS (utility-first, highly customizable) OR
  - Material-UI (component library, faster development)
- **Headless UI** or **Radix UI** - Unstyled accessible components (if using Tailwind)

### Testing
- **Vitest** - Fast unit test runner (Vite-native)
- **React Testing Library** - Component testing
- **MSW (Mock Service Worker)** - API mocking for tests

### Development Tools
- **ESLint** - Code linting
- **Prettier** - Code formatting
- **Husky** - Git hooks
- **TypeScript ESLint** - TypeScript linting rules

---

## Database

**Options under consideration:**
1. **PostgreSQL** 
   - ✅ Cross-platform, open-source
   - ✅ Advanced features, JSON support
   - ✅ Popular in modern web development
   
2. **SQL Server** 
   - ✅ Deep .NET ecosystem integration
   - ✅ Excellent tooling (SSMS)
   - ✅ Enterprise standard

**Decision**: [TO BE DECIDED]

---

## DevOps & Infrastructure

### Containerization
- **Docker** - Application containerization
- **Docker Compose** - Multi-container orchestration
  - API container
  - Database container
  - [Optional: Redis container]

### CI/CD
- **GitHub Actions** - Automated build, test, deploy pipeline
  - Build and test on PR
  - Docker image creation
  - [Optional: Deployment to Azure/AWS]

### Caching (Optional but Impressive)
- **Redis** - Distributed caching
  - Response caching
  - Session storage
  - Rate limiting

### Cloud Platform (Future)
- **[Optional]**: Azure App Service, AWS ECS, or other cloud hosting

---

## Project Domain

**Application Type**: [TO BE DECIDED]

**Suggested Options:**
1. **Task/Project Management** (like Jira/Trello)
2. **E-commerce Platform** (products, cart, orders)
3. **Blog/CMS** (articles, comments, categories)
4. **Social Media Clone** (posts, likes, follows)
5. **Other**: _______________________

---

## Architecture Decisions

### Backend Structure
```
src/
├── API/                    # ASP.NET Core Web API
├── Application/           # Business logic, CQRS handlers
├── Domain/                # Entities, Value Objects, Interfaces
└── Infrastructure/        # EF Core, External Services
tests/
├── UnitTests/
├── IntegrationTests/
└── ArchitectureTests/
```

### Frontend Structure
```
src/
├── assets/               # Images, fonts, static files
├── components/           # Reusable UI components
│   ├── common/          # Shared components
│   └── features/        # Feature-specific components
├── features/            # Feature modules
│   └── [feature]/
│       ├── api/         # API calls
│       ├── components/  # Feature components
│       ├── hooks/       # Custom hooks
│       └── types/       # TypeScript types
├── hooks/               # Global custom hooks
├── lib/                 # Utilities, helpers
├── routes/              # Route definitions
├── services/            # API service layer
├── stores/              # State management
└── types/               # Global TypeScript types
```

---

## Next Steps

- [ ] Decide on database (PostgreSQL vs SQL Server)
- [ ] Choose UI framework (TailwindCSS vs Material-UI)
- [ ] Select state management (Zustand vs Redux Toolkit)
- [ ] Define project domain/application type
- [ ] Set up project structure
- [ ] Initialize Git repository
- [ ] Create Docker configurations
- [ ] Set up CI/CD pipeline

---

## Notes

Add any additional decisions or notes here as the project evolves.
