# Bulletproof .NET React - Technology Stack

## Project Overview

**Purpose**: Portfolio/presentation project demonstrating full-stack development expertise  
**Philosophy**: Open-source and license-free technologies wherever possible  
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
- **SOLID Principles** - Applied throughout the codebase
  - Single Responsibility
  - Open/Closed
  - Liskov Substitution
  - Interface Segregation
  - Dependency Inversion

### Database & Data Access

- **PostgreSQL 16** - Open-source relational database
  - Cross-platform, enterprise-grade
  - Advanced JSON support (JSONB)
  - Excellent performance and scalability
  - `postgres:16-alpine` Docker image
- **Dapper** - Lightweight micro-ORM for high-performance data access
- **DbUp** or **FluentMigrator** - Database versioning and migrations
- **Npgsql** - PostgreSQL data provider for .NET

### Authentication & Security

- **JWT (JSON Web Tokens)** - Stateless authentication
- **ASP.NET Core Identity** - User management
- **Role-based Authorization** - Access control
- **HTTPS Enforcement** - Secure communication
- **CORS Policy** - Cross-origin configuration
- **Rate Limiting** - Throttling and DDoS protection
  - IP-based rate limiting
  - User-based rate limiting
  - Endpoint-specific limits

### Validation & Mapping

- **FluentValidation** - Request/command validation
- **AutoMapper** - Object-to-object mapping (DTOs ↔ Entities)

### Logging & Monitoring

- **Serilog** - Structured logging
  - Console sink
  - File sink
  - [Optional: Seq/ELK for production]

### API Features

- **API Versioning** - URL-based versioning (v1, v2)
  - Microsoft.AspNetCore.Mvc.Versioning
  - Version-specific controllers and endpoints
- **Pagination Pattern** - `PagedResult<T>` wrapper
  - Consistent pagination across endpoints
  - Page number and page size parameters
  - Total count and metadata
- **Global Error Handling** - Centralized exception handling
  - Custom exception middleware
  - Consistent error response format
  - Detailed logging for debugging
  - User-friendly error messages

### API Documentation

- **Swagger/OpenAPI** - Interactive API documentation
- **Swashbuckle** - Swagger implementation for .NET
- **Swagger with Authentication** - JWT bearer token support in Swagger UI
  - Authorize button for token input
  - Test authenticated endpoints directly in Swagger

### Testing

- **xUnit** - Unit testing framework
- **Moq** - Mocking framework
- **FluentAssertions** - Assertion library
- **WebApplicationFactory** - Integration testing for API endpoints
- **Testcontainers** - Containerized test dependencies (database)

**Testing Strategy:**

- **Unit Tests** - Test business logic in isolation
  - Domain entities and value objects
  - Application layer (CQRS handlers, validators)
  - Service classes
  - Target: 80%+ code coverage
- **Integration Tests** - Test API endpoints end-to-end
  - WebApplicationFactory with test database
  - Full request/response pipeline
  - Authentication and authorization flows
  - Database interactions
- **Architecture Tests** - Enforce architectural rules
  - Layer dependency validation
  - Naming conventions
  - Ensure Clean Architecture boundaries

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
- **Redux Toolkit** - Client state management
  - RTK Async Thunks - Asynchronous actions
  - RTK Slices - State management patterns
  - Redux DevTools - Time-travel debugging

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

- **TailwindCSS** - Utility-first CSS framework
- **shadcn/ui** - Re-usable component library built with Radix UI and Tailwind
  - Copy-paste components (not an npm dependency)
  - Fully customizable and accessible
  - Built on Radix UI primitives

### Testing

- **Vitest** - Fast unit test runner (Vite-native)
- **React Testing Library** - Component testing
- **MSW (Mock Service Worker)** - API mocking for tests
- **Playwright** or **Cypress** - End-to-end testing

**Testing Strategy:**

- **Unit Tests** - Test components and utilities in isolation
  - React components (user interactions, rendering)
  - Custom hooks
  - Utility functions
  - Redux slices and thunks
  - Target: 80%+ code coverage
- **Integration Tests** - Test feature flows
  - Multi-component interactions
  - API integration with MSW
  - Redux store integration
- **E2E Tests** - Critical user journeys
  - Authentication flow
  - Core business workflows
  - Cross-browser testing

### Development Tools

- **ESLint** - Code linting
- **Prettier** - Code formatting
- **Husky** - Git hooks
- **TypeScript ESLint** - TypeScript linting rules

---

## Database

**Database**: PostgreSQL 16

**Why PostgreSQL:**

- ✅ Open-source, no licensing costs
- ✅ Cross-platform (Windows, Linux, macOS)
- ✅ Enterprise-grade performance and reliability
- ✅ Advanced features: JSONB, full-text search, CTEs
- ✅ Excellent Docker support (`postgres:16-alpine`)
- ✅ Widely used in modern tech companies
- ✅ Cloud-native (AWS RDS, Azure Database, Google Cloud SQL)

**Development Tools:**

- pgAdmin (open-source GUI)
- DBeaver (cross-platform, free)
- Azure Data Studio with PostgreSQL extension

---

## DevOps & Infrastructure

### Containerization

- **Docker** - Application containerization
  - `Dockerfile` (Backend) - Multi-stage build for .NET API
  - `Dockerfile` (Frontend) - Multi-stage build for React app
- **Docker Compose** - Multi-container orchestration
  - API container
  - Frontend container
  - Database container
  - [Optional: Redis container]
  - [Optional: NGINX reverse proxy]

### CI/CD

- **GitHub Actions** / **Azure DevOps** - Automated pipeline
  - **Build** - Compile backend and frontend
  - **Test** - Run unit and integration tests
  - **Lint** - Code quality checks (ESLint, Prettier, .NET analyzers)
  - **Publish Docker Images** - Push to Docker Hub or GitHub Container Registry
  - **Deploy** - Automated deployment to staging/production

### Deployment Strategy

- **NGINX** - Reverse proxy
  - Route requests to backend API
  - Serve frontend static files
  - SSL/TLS termination
  - Load balancing (if scaled)
- **Environment Configs** - Environment-specific settings
  - Development (`appsettings.Development.json`, `.env.development`)
  - Staging (`appsettings.Staging.json`, `.env.staging`)
  - Production (`appsettings.Production.json`, `.env.production`)
- **Secrets Management**
  - Azure Key Vault / AWS Secrets Manager / HashiCorp Vault
  - Docker secrets for sensitive data
  - Environment variables for non-sensitive configs

### Caching (Optional but Impressive)

- **Redis** - Distributed caching
  - Response caching
  - Session storage
  - Rate limiting

### Cloud Platform (Future)

- **[Optional]**: Azure App Service, AWS ECS, or other cloud hosting

---

## Architecture Decisions

### Backend Structure

```
src/
├── API/                    # ASP.NET Core Web API
├── Application/           # Business logic, CQRS handlers
├── Domain/                # Entities, Value Objects, Interfaces
└── Infrastructure/        # Dapper, SQL, External Services
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

## Documentation & Code Quality

### Documentation

- **README.md** - Comprehensive project documentation
  - Project overview and features
  - Technology stack
  - Setup instructions
  - API documentation links
  - Architecture overview
  - Contributing guidelines
- **Architecture Diagram** - Visual representation
  - C4 model or similar
  - Component relationships
  - Data flow diagrams
  - Deployment architecture

### Git & Code Quality

- **Clean Commit History** - Well-structured Git practices
  - Conventional Commits (feat:, fix:, chore:, docs:)
  - Atomic commits
  - Meaningful commit messages
  - Feature branches and PR workflow
- **Code Quality Standards**
  - Consistent code formatting
  - Code reviews
  - Pre-commit hooks (Husky)
  - No merge commits (squash or rebase)
