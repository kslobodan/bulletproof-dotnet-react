# Day 1: Project Setup & Architecture

---

## Project Setup & Architecture

1. Created src folder and navigated into it: `mkdir src; cd src`
2. Created .NET solution file: `dotnet new sln -n BookingSystem`
3. Created Domain project (class library): `dotnet new classlib -n BookingSystem.Domain`
4. Created Application project (class library): `dotnet new classlib -n BookingSystem.Application`
5. Created Infrastructure project (class library): `dotnet new classlib -n BookingSystem.Infrastructure`
6. Created API project (ASP.NET Core Web API): `dotnet new webapi -n BookingSystem.API`
7. Added all projects to solution:
   - `dotnet sln add BookingSystem.Domain/BookingSystem.Domain.csproj`
   - `dotnet sln add BookingSystem.Application/BookingSystem.Application.csproj`
   - `dotnet sln add BookingSystem.Infrastructure/BookingSystem.Infrastructure.csproj`
   - `dotnet sln add BookingSystem.API/BookingSystem.API.csproj`
8. Configured project references (Clean Architecture dependency flow):
   - `dotnet add BookingSystem.API/BookingSystem.API.csproj reference BookingSystem.Infrastructure/BookingSystem.Infrastructure.csproj`
   - `dotnet add BookingSystem.API/BookingSystem.API.csproj reference BookingSystem.Application/BookingSystem.Application.csproj`
   - `dotnet add BookingSystem.Infrastructure/BookingSystem.Infrastructure.csproj reference BookingSystem.Application/BookingSystem.Application.csproj`
   - `dotnet add BookingSystem.Application/BookingSystem.Application.csproj reference BookingSystem.Domain/BookingSystem.Domain.csproj`

## Docker & PostgreSQL Setup

9. Created `docker-compose.yml` with PostgreSQL 16 service configuration
   - Image: postgres:16-alpine
   - Database: BookingSystemDB
   - Port: 5432
   - Credentials: postgres/postgres (development only)
10. Started PostgreSQL container: `docker-compose up -d`

## NuGet Packages Installation

11. Installed Serilog in API project:
    - `cd ../BookingSystem.API`
    - `dotnet add package Serilog.AspNetCore`
    - `dotnet add package Serilog.Sinks.Console`
    - `dotnet add package Serilog.Sinks.File`
    - `dotnet add package MediatR`
12. Installed Dapper and Npgsql in Infrastructure project:
    - `cd src/BookingSystem.Infrastructure`
    - `dotnet add package Dapper`
    - `dotnet add package Npgsql`
13. Installed MediatR, AutoMapper and FluentValidation in Application project:
    - `cd ../BookingSystem.Application`
    - `dotnet add package MediatR`
    - `dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection`
    - `dotnet add package FluentValidation.DependencyInjectionExtensions`

## Configuration

14. Created `AssemblyReference.cs` in `BookingSystem.Application`
15. Configured Serilog in `Program.cs`
16. Configured MediatR in `Program.cs`
17. Configured FluentValidation (AddValidatorsFromAssembly) in `Program.cs`
18. Configured AutoMapper in `Program.cs`
19. Added PostgreSQL connection string to `appsettings.Development.json`

## ⭐ VS Code C# Auto-Formatting Setup

**Enable auto-format on save for better code quality:**

Press `Ctrl+Shift+P` → Type "Preferences: Open User Settings (JSON)"

Add these settings:

```json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.organizeImports": "explicit"
  },
  "[csharp]": {
    "editor.formatOnSave": true,
    "editor.defaultFormatter": "ms-dotnettools.csharp"
  }
}
```

**Required Extension:** C# Dev Kit by Microsoft (`ms-dotnettools.csharp`)

**Benefits:**

- Auto-formats code on every save
- Organizes imports (removes unused, sorts alphabetically)
- Consistent code style across the project
- Works with EditorConfig for team-wide consistency
