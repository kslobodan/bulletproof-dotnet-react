# Installation Guide - Required Technologies & Libraries

This document lists all technologies, libraries, and packages required for the Bulletproof .NET React project.

---

## Prerequisites

### Required Software

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 20+ LTS** - [Download](https://nodejs.org/)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop)
- **Git** - [Download](https://git-scm.com/)
- **PostgreSQL 16** (optional for local dev, Docker recommended)

### Recommended IDE

- **Visual Studio Code** - Lightweight, cross-platform

### VS Code Extensions (Required)

Install these extensions from VS Code marketplace:

**Essential for .NET Development:**

- **C# Dev Kit** (ms-dotnettools.csdevkit) - Official Microsoft C# extension
  - Includes C# editing, debugging, testing
  - IntelliSense and code navigation
  - OR use **C#** (ms-dotnettools.csharp) if Dev Kit is not available
- **NuGet Package Manager** (jmrog.vscode-nuget-package-manager) - Manage NuGet packages

**Helpful for Development:**

- **ESLint** (dbaeumer.vscode-eslint) - JavaScript/TypeScript linting
- **Prettier** (esbenp.prettier-vscode) - Code formatting
- **Tailwind CSS IntelliSense** (bradlc.vscode-tailwindcss) - Tailwind autocomplete
- **Docker** (ms-azuretools.vscode-docker) - Docker container management
- **GitLens** (eamodio.gitlens) - Enhanced Git capabilities
- **REST Client** (humao.rest-client) - Test API endpoints (alternative to Postman)
- **Thunder Client** (rangav.vscode-thunder-client) - API testing in VS Code
- **Error Lens** (usernamehw.errorlens) - Inline error highlighting

**Optional but Useful:**

- **Auto Rename Tag** (formulahendry.auto-rename-tag)
- **Path Intellisense** (christian-kohler.path-intellisense)
- **Material Icon Theme** (pkief.material-icon-theme)

### Other Recommended Tools

- **pgAdmin** or **DBeaver** - PostgreSQL GUI tools
- **Postman** or **Insomnia** or **Thunder Client** - API testing
- **Docker Compose** (included with Docker Desktop)

---

## Backend (.NET 8 API)

### Project Structure

```
src/
├── API/                    # ASP.NET Core Web API
├── Application/           # Business logic, CQRS
├── Domain/                # Entities, interfaces
└── Infrastructure/        # Data access, external services
tests/
├── UnitTests/
├── IntegrationTests/
└── ArchitectureTests/
```

### NuGet Packages

#### API Project

```bash
# Core Framework
Swashbuckle.AspNetCore                      # Swagger/OpenAPI (provides complete OpenAPI functionality)
Swashbuckle.AspNetCore.Filters              # Swagger auth support (optional)

# Authentication
Microsoft.AspNetCore.Authentication.JwtBearer
Microsoft.AspNetCore.Identity.EntityFrameworkCore  # User management (optional with Dapper)
System.IdentityModel.Tokens.Jwt

# API Features
Asp.Versioning.Http                          # API versioning (v8.1.1)
Asp.Versioning.Mvc.ApiExplorer               # API Explorer for versioned APIs
AspNetCoreRateLimit                          # Rate limiting

# Logging
Serilog.AspNetCore
Serilog.Sinks.Console
Serilog.Sinks.File
Serilog.Enrichers.Environment
```

#### Application Project

```bash
# CQRS & Mediator
MediatR
MediatR.Extensions.Microsoft.DependencyInjection

# Validation
FluentValidation
FluentValidation.DependencyInjectionExtensions

# Mapping
AutoMapper
AutoMapper.Extensions.Microsoft.DependencyInjection
```

#### Domain Project

```bash
# No external dependencies (pure domain logic)
```

#### Infrastructure Project

```bash
# Database - PostgreSQL
Npgsql                                       # PostgreSQL provider for .NET
Dapper                                       # Micro-ORM

# Database Migrations
DbUp                                         # OR
FluentMigrator
FluentMigrator.Runner

# Password Hashing
BCrypt.Net-Next

# Resilience
Polly                                        # Optional: Retry policies, circuit breaker

# Caching (Optional)
StackExchange.Redis                          # If using Redis
Microsoft.Extensions.Caching.StackExchangeRedis
```

#### Unit Tests Project

```bash
# Testing Framework
xUnit
xUnit.runner.visualstudio

# Mocking
Moq

# Assertions
FluentAssertions

# Test SDK
Microsoft.NET.Test.Sdk
```

#### Integration Tests Project

```bash
# Testing Framework
xUnit
xUnit.runner.visualstudio
Microsoft.NET.Test.Sdk

# Integration Testing
Microsoft.AspNetCore.Mvc.Testing             # WebApplicationFactory
FluentAssertions

# Test Containers
Testcontainers                               # Docker containers for tests
Testcontainers.PostgreSql
```

#### Architecture Tests Project

```bash
# Architecture Testing
NetArchTest.Rules                            # Enforce architecture rules

# Testing Framework
xUnit
FluentAssertions
Microsoft.NET.Test.Sdk
```

### Installation Commands (Backend)

```bash
# Navigate to solution directory
cd src

# Create solution
dotnet new sln -n BulletproofTaskManager

# Create projects
dotnet new webapi -n API
dotnet new classlib -n Application
dotnet new classlib -n Domain
dotnet new classlib -n Infrastructure
dotnet new xunit -n UnitTests
dotnet new xunit -n IntegrationTests
dotnet new xunit -n ArchitectureTests

# Add projects to solution
dotnet sln add API/API.csproj
dotnet sln add Application/Application.csproj
dotnet sln add Domain/Domain.csproj
dotnet sln add Infrastructure/Infrastructure.csproj
dotnet sln add ../tests/UnitTests/UnitTests.csproj
dotnet sln add ../tests/IntegrationTests/IntegrationTests.csproj
dotnet sln add ../tests/ArchitectureTests/ArchitectureTests.csproj

# Add project references
dotnet add API/API.csproj reference Application/Application.csproj
dotnet add API/API.csproj reference Infrastructure/Infrastructure.csproj
dotnet add Application/Application.csproj reference Domain/Domain.csproj
dotnet add Infrastructure/Infrastructure.csproj reference Application/Application.csproj

# Install packages (API)
cd API
dotnet add package Swashbuckle.AspNetCore
dotnet add package Swashbuckle.AspNetCore.Filters
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package Microsoft.AspNetCore.Mvc.Versioning
dotnet add package Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer
dotnet add package AspNetCoreRateLimit
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File

# Install packages (Application)
cd ../Application
dotnet add package MediatR
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
dotnet add package AutoMapper
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection

# Install packages (Infrastructure)
cd ../Infrastructure
dotnet add package Npgsql
dotnet add package Dapper
dotnet add package DbUp
dotnet add package BCrypt.Net-Next

# Install packages (Tests)
cd ../../tests/UnitTests
dotnet add package Moq
dotnet add package FluentAssertions

cd ../IntegrationTests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package FluentAssertions
dotnet add package Testcontainers
dotnet add package Testcontainers.PostgreSql

cd ../ArchitectureTests
dotnet add package NetArchTest.Rules
dotnet add package FluentAssertions
```

---

## Frontend (React + TypeScript)

### Project Structure

```
src/
├── assets/               # Images, fonts
├── components/           # Reusable components
│   ├── common/
│   └── ui/              # shadcn/ui components
├── features/            # Feature modules
│   ├── auth/
│   ├── projects/
│   └── tasks/
├── hooks/               # Custom hooks
├── lib/                 # Utilities
├── routes/              # Route definitions
├── services/            # API services
├── store/               # Redux store
└── types/               # TypeScript types
```

### NPM Packages

#### Core Dependencies

```json
{
  "dependencies": {
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.22.0",

    "typescript": "^5.3.0",

    "@reduxjs/toolkit": "^2.2.0",
    "react-redux": "^9.1.0",

    "@tanstack/react-query": "^5.24.0",

    "axios": "^1.6.0",

    "react-hook-form": "^7.50.0",
    "zod": "^3.22.0",
    "@hookform/resolvers": "^3.3.0",

    "tailwindcss": "^3.4.0",
    "autoprefixer": "^10.4.0",
    "postcss": "^8.4.0",
    "clsx": "^2.1.0",
    "tailwind-merge": "^2.2.0",
    "class-variance-authority": "^0.7.0",

    "@radix-ui/react-alert-dialog": "^1.0.5",
    "@radix-ui/react-avatar": "^1.0.4",
    "@radix-ui/react-checkbox": "^1.0.4",
    "@radix-ui/react-dialog": "^1.0.5",
    "@radix-ui/react-dropdown-menu": "^2.0.6",
    "@radix-ui/react-label": "^2.0.2",
    "@radix-ui/react-select": "^2.0.0",
    "@radix-ui/react-separator": "^1.0.3",
    "@radix-ui/react-slot": "^1.0.2",
    "@radix-ui/react-toast": "^1.1.5",

    "lucide-react": "^0.344.0",

    "date-fns": "^3.3.0"
  }
}
```

#### Dev Dependencies

```json
{
  "devDependencies": {
    "@types/react": "^18.2.0",
    "@types/react-dom": "^18.2.0",
    "@types/node": "^20.11.0",

    "@vitejs/plugin-react": "^4.2.0",
    "vite": "^5.1.0",

    "eslint": "^8.56.0",
    "eslint-plugin-react-hooks": "^4.6.0",
    "eslint-plugin-react-refresh": "^0.4.5",
    "@typescript-eslint/eslint-plugin": "^6.21.0",
    "@typescript-eslint/parser": "^6.21.0",

    "prettier": "^3.2.0",
    "prettier-plugin-tailwindcss": "^0.5.0",

    "vitest": "^1.3.0",
    "@testing-library/react": "^14.2.0",
    "@testing-library/jest-dom": "^6.4.0",
    "@testing-library/user-event": "^14.5.0",
    "jsdom": "^24.0.0",

    "msw": "^2.1.0",

    "@playwright/test": "^1.42.0",

    "husky": "^9.0.0",
    "lint-staged": "^15.2.0"
  }
}
```

### Installation Commands (Frontend)

```bash
# Create Vite project with React + TypeScript
npm create vite@latest frontend -- --template react-ts
cd frontend

# Install core dependencies
npm install react-router-dom
npm install @reduxjs/toolkit react-redux
npm install @tanstack/react-query
npm install axios
npm install react-hook-form zod @hookform/resolvers
npm install date-fns

# Install TailwindCSS
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p

# Install shadcn/ui utilities
npm install tailwind-merge clsx class-variance-authority

# Install shadcn/ui CLI and components
npx shadcn-ui@latest init
npx shadcn-ui@latest add button
npx shadcn-ui@latest add input
npx shadcn-ui@latest add label
npx shadcn-ui@latest add card
npx shadcn-ui@latest add dialog
npx shadcn-ui@latest add dropdown-menu
npx shadcn-ui@latest add select
npx shadcn-ui@latest add toast
npx shadcn-ui@latest add avatar
npx shadcn-ui@latest add separator
npx shadcn-ui@latest add alert-dialog
npx shadcn-ui@latest add checkbox

# Install Radix UI components (if not installed by shadcn)
# These are usually installed automatically by shadcn CLI

# Install icons
npm install lucide-react

# Install ESLint & Prettier
npm install -D prettier prettier-plugin-tailwindcss
npm install -D eslint @typescript-eslint/eslint-plugin @typescript-eslint/parser
npm install -D eslint-plugin-react-hooks eslint-plugin-react-refresh

# Install testing libraries
npm install -D vitest jsdom
npm install -D @testing-library/react @testing-library/jest-dom @testing-library/user-event
npm install -D msw

# Install E2E testing
npm install -D @playwright/test
npx playwright install

# Install Git hooks
npm install -D husky lint-staged
npx husky init
```

---

## Docker Setup

### Required Images

```bash
# Pull required Docker images
docker pull postgres:16-alpine
docker pull nginx:alpine
```

### Docker Files

- `Dockerfile` (backend) - Multi-stage build for .NET API
- `Dockerfile` (frontend) - Multi-stage build with NGINX
- `docker-compose.yml` - Orchestration file
- `.dockerignore` - Files to exclude from Docker build

---

## Development Environment Setup

### 1. Clone Repository

```bash
git clone <repository-url>
cd bulletproof-dotnet-react
```

### 2. Backend Setup

```bash
cd src
# Follow backend installation commands above
dotnet restore
dotnet build
```

### 3. Frontend Setup

```bash
cd frontend
# Follow frontend installation commands above
npm install
```

### 4. PostgreSQL Setup (Docker)

```bash
# Start PostgreSQL container
docker run --name bulletproof-postgres \
  -e POSTGRES_USER=admin \
  -e POSTGRES_PASSWORD=admin123 \
  -e POSTGRES_DB=taskmanager \
  -p 5432:5432 \
  -d postgres:16-alpine
```

### 5. Run Application

```bash
# Terminal 1: Backend
cd src/API
dotnet run

# Terminal 2: Frontend
cd frontend
npm run dev

# Terminal 3: PostgreSQL (if needed)
docker start bulletproof-postgres
```

### 6. Verify Installation

- Backend API: `https://localhost:7000/swagger`
- Frontend: `http://localhost:5173`
- PostgreSQL: `localhost:5432`

---

## Environment Variables

### Backend (.NET)

Create `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=taskmanager;Username=admin;Password=admin123"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-characters-long",
    "Issuer": "BulletproofAPI",
    "Audience": "BulletproofClient",
    "ExpirationMinutes": 60
  }
}
```

### Frontend (React)

Create `.env.development`:

```env
VITE_API_BASE_URL=https://localhost:7000/api/v1
```

---

## Verification Checklist

- [ ] .NET 8 SDK installed (`dotnet --version`)
- [ ] Node.js 20+ installed (`node --version`)
- [ ] Docker Desktop running (`docker --version`)
- [ ] Git installed (`git --version`)
- [ ] Backend solution builds successfully (`dotnet build`)
- [ ] Frontend builds successfully (`npm run build`)
- [ ] PostgreSQL container running (`docker ps`)
- [ ] Can access Swagger UI
- [ ] Can access frontend dev server
- [ ] Tests run successfully (`dotnet test`, `npm test`)

---

## Troubleshooting

### Common Issues

**Port conflicts:**

- Backend: Change port in `launchSettings.json`
- Frontend: Change port in `vite.config.ts`
- PostgreSQL: Change port in docker run command

**PostgreSQL connection issues:**

- Check container is running: `docker ps`
- Check connection string in `appsettings.Development.json`
- Test connection with pgAdmin/DBeaver

**CORS errors:**

- Configure CORS in backend `Program.cs`
- Ensure frontend URL is whitelisted

**NuGet package restore issues:**

- Clear cache: `dotnet nuget locals all --clear`
- Restore: `dotnet restore`

**NPM installation issues:**

- Clear cache: `npm cache clean --force`
- Delete `node_modules` and `package-lock.json`
- Reinstall: `npm install`

---

## Next Steps

After installation, proceed to [PROJECT_ROADMAP.md](PROJECT_ROADMAP.md) to begin Day 1 development.
