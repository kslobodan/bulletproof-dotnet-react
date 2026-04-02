# Bulletproof .NET React - Project Roadmap

## 🎯 Project Goal

Create a **Task Management System** that demonstrates senior full-stack developer skills and can be rebuilt from memory in 1-2 days after practice.

**Timeline**: 2 weeks of focused development  
**Domain**: Task/Project Management (like simplified Jira/Trello)  
**End Goal**: Pass senior fullstack developer interviews

---

## 📊 Project Scope

### Core Entities

1. **Users** - Authentication, roles (Admin, User)
2. **Projects** - Containers for tasks
3. **Tasks** - Work items with status, priority, assignment
4. **Comments** (optional if time permits)

### Must-Have Features

- ✅ User registration and JWT authentication
- ✅ CRUD operations for Projects and Tasks
- ✅ Task assignment to users
- ✅ Status workflow (Todo → In Progress → Done)
- ✅ Priority levels (Low, Medium, High)
- ✅ Pagination and filtering
- ✅ Role-based authorization
- ✅ Global error handling
- ✅ API versioning (v1)
- ✅ Swagger with JWT auth
- ✅ Rate limiting

### Testing

- ✅ Backend unit tests (business logic)
- ✅ Backend integration tests (API endpoints)
- ✅ Frontend component tests
- ✅ E2E tests (critical paths)

### DevOps

- ✅ Dockerfiles (backend, frontend)
- ✅ docker-compose with PostgreSQL and NGINX
- ✅ GitHub Actions CI/CD
- ✅ README with architecture diagram

---

## 📅 Two-Week Development Plan

### **Week 1: Backend Foundation + Core Features**

#### **Day 1: Project Setup & Architecture**

- [ ] Initialize .NET solution with Clean Architecture structure
  - `API` project (ASP.NET Core Web API)
  - `Application` project (business logic, CQRS)
  - `Domain` project (entities, interfaces)
  - `Infrastructure` project (Dapper, PostgreSQL)
  - Test projects (Unit, Integration, Architecture)
- [ ] Set up PostgreSQL with Docker
- [ ] Configure Dapper and Npgsql
- [ ] Set up MediatR for CQRS
- [ ] Configure Serilog for logging
- [ ] Add FluentValidation
- [ ] Set up AutoMapper

**Learning Focus**: Clean Architecture layers, project dependencies

---

#### **Day 2: Core Infrastructure**

- [ ] Implement global error handling middleware
- [ ] Set up API versioning (v1)
- [ ] Configure Swagger/OpenAPI
- [ ] Implement `PagedResult<T>` pattern
- [ ] Set up database migrations (DbUp or FluentMigrator)
- [ ] Create database schema (Users, Projects, Tasks tables)
- [ ] Implement base repository pattern with Dapper

**Learning Focus**: Middleware pipeline, Dapper queries, pagination

---

#### **Day 3: Authentication & Authorization**

- [ ] Implement User entity and value objects
- [ ] Create authentication commands (Register, Login)
- [ ] JWT token generation and validation
- [ ] Password hashing (BCrypt)
- [ ] Configure JWT authentication in pipeline
- [ ] Implement role-based authorization
- [ ] Add JWT auth to Swagger

**Learning Focus**: JWT flow, authentication middleware, security

---

#### **Day 4: Projects CRUD**

- [ ] Implement Project entity
- [ ] Create Project commands (Create, Update, Delete)
- [ ] Create Project queries (GetById, GetAll with pagination)
- [ ] Implement Project validation rules
- [ ] Add Dapper repositories for Projects
- [ ] Create Project DTOs
- [ ] Implement Project API endpoints
- [ ] Add authorization (only owner can modify)

**Learning Focus**: CQRS pattern, command/query separation

---

#### **Day 5: Tasks CRUD**

- [ ] Implement Task entity with status enum
- [ ] Create Task commands (Create, Update, Delete, ChangeStatus)
- [ ] Create Task queries (GetById, GetByProject, GetAll with filtering)
- [ ] Implement Task validation rules
- [ ] Add Dapper repositories for Tasks
- [ ] Create Task DTOs
- [ ] Implement Task API endpoints
- [ ] Task assignment logic

**Learning Focus**: Complex queries with Dapper, business logic

---

#### **Day 6: Advanced Backend Features**

- [ ] Implement rate limiting middleware
- [ ] Add filtering and sorting to task queries
- [ ] Implement soft delete for entities
- [ ] Add audit fields (CreatedAt, UpdatedAt, CreatedBy)
- [ ] Create statistics endpoints (task counts, project summaries)
- [ ] Add input sanitization
- [ ] Implement domain events (optional)

**Learning Focus**: Production patterns, security hardening

---

#### **Day 7: Backend Testing**

- [ ] Set up xUnit test projects
- [ ] Write unit tests for domain logic (entities, value objects)
- [ ] Write unit tests for CQRS handlers
- [ ] Write unit tests for validators
- [ ] Set up integration tests with WebApplicationFactory
- [ ] Configure Testcontainers for PostgreSQL
- [ ] Write integration tests for auth endpoints
- [ ] Write integration tests for CRUD operations
- [ ] Add architecture tests (layer dependencies)
- [ ] Aim for 80%+ code coverage

**Learning Focus**: Testing strategies, test isolation, mocking

---

### **Week 2: Frontend + DevOps + Polish**

#### **Day 8: Frontend Setup**

- [ ] Initialize Vite + React + TypeScript project
- [ ] Install and configure TailwindCSS
- [ ] Set up shadcn/ui (install CLI, add components)
- [ ] Configure Redux Toolkit store
- [ ] Set up React Router v6
- [ ] Create folder structure (features, components, lib, services)
- [ ] Set up Axios with interceptors
- [ ] Create authentication context/slice
- [ ] Implement login/register pages

**Learning Focus**: Modern React setup, Redux Toolkit structure

---

#### **Day 9: Authentication Flow**

- [ ] Create auth Redux slice with async thunks
- [ ] Implement token storage (localStorage/sessionStorage)
- [ ] Add axios interceptors (token injection, refresh logic)
- [ ] Create protected route wrapper
- [ ] Implement login form with React Hook Form + Zod
- [ ] Implement register form
- [ ] Add form validation and error display
- [ ] Create navigation with user menu
- [ ] Add logout functionality

**Learning Focus**: React Hook Form, Zod validation, auth flow

---

#### **Day 10: Projects Feature**

- [ ] Create projects Redux slice with async thunks
- [ ] Implement projects list page with pagination
- [ ] Create project card components
- [ ] Implement create project form (modal/page)
- [ ] Implement edit project functionality
- [ ] Implement delete project (with confirmation)
- [ ] Add loading states and error handling
- [ ] Implement search/filter for projects

**Learning Focus**: Redux Toolkit Query patterns, CRUD operations

---

#### **Day 11: Tasks Feature**

- [ ] Create tasks Redux slice with async thunks
- [ ] Implement task board view (columns by status)
- [ ] Create task card components
- [ ] Implement create task form
- [ ] Implement edit task (inline or modal)
- [ ] Implement drag-and-drop status change (optional)
- [ ] Add task filtering (priority, assignee, status)
- [ ] Implement task assignment dropdown
- [ ] Add task details view

**Learning Focus**: Complex UI state, real-time updates pattern

---

#### **Day 12: Frontend Testing & Polish**

- [ ] Set up Vitest and React Testing Library
- [ ] Write component tests (forms, cards, lists)
- [ ] Write hook tests (custom hooks)
- [ ] Test Redux slices and thunks with MSW
- [ ] Set up Playwright or Cypress
- [ ] Write E2E test for authentication flow
- [ ] Write E2E test for creating project and task
- [ ] Add loading skeletons
- [ ] Improve error messages and toast notifications
- [ ] Responsive design check (mobile, tablet, desktop)

**Learning Focus**: Testing React apps, E2E testing

---

#### **Day 13: Docker & DevOps**

- [ ] Create Dockerfile for backend (multi-stage)
- [ ] Create Dockerfile for frontend (multi-stage with nginx)
- [ ] Create docker-compose.yml
  - PostgreSQL service
  - Backend API service
  - Frontend service
  - NGINX reverse proxy
- [ ] Configure NGINX reverse proxy rules
- [ ] Set up environment variables
- [ ] Test full stack with Docker Compose
- [ ] Create .dockerignore files

**Learning Focus**: Docker containerization, multi-container orchestration

---

#### **Day 14: CI/CD & Documentation**

- [ ] Set up GitHub Actions workflow
  - Build backend
  - Run backend tests
  - Build frontend
  - Run frontend tests and linting
  - Build Docker images
  - Push to Docker Hub/GitHub Container Registry (optional)
- [ ] Create comprehensive README.md
  - Project overview
  - Features list
  - Technology stack
  - Architecture overview
  - Setup instructions (local, Docker)
  - API documentation link
  - Screenshots (optional)
- [ ] Create architecture diagram (C4, flowchart, or simple diagram)
- [ ] Write CONTRIBUTING.md (optional)
- [ ] Add LICENSE file (MIT)
- [ ] Final code review and cleanup
- [ ] Clean commit history (squash if needed)
- [ ] Tag v1.0.0 release

**Learning Focus**: CI/CD pipelines, documentation best practices

---

## 🎓 Post-Development Practice Plan

### **Week 3: First Solo Rebuild**

- Attempt to rebuild the entire project from scratch
- Use TECH_STACK.md and PROJECT_ROADMAP.md as guides
- Don't look at code unless completely stuck
- Document what you struggle with

### **Week 4: Second Solo Rebuild**

- Rebuild again, this time faster
- Focus on areas where you struggled last time
- Try to complete in 3-4 days

### **Week 5+: Speed Practice**

- Rebuild multiple times until you can do it in 1-2 days
- This is when it becomes muscle memory
- You'll be able to explain every decision in interviews

---

## 🎯 Key Interview Talking Points

### **Architecture Decisions**

- "Why Clean Architecture?" → Separation of concerns, testability, maintainability
- "Why CQRS?" → Separates read and write models, scalability, clarity
- "Why Dapper over EF?" → Performance, control, learning SQL, no magic

### **Technical Choices**

- "Why PostgreSQL?" → Open-source, modern, cross-platform, JSON support
- "Why Redux Toolkit?" → Predictable state, DevTools, industry standard
- "Why shadcn/ui?" → Copy-paste components, full customization, accessibility

### **Testing Strategy**

- Unit tests for business logic (80%+ coverage)
- Integration tests for API endpoints
- E2E tests for critical user journeys
- Architecture tests to enforce boundaries

### **Production Readiness**

- Global error handling with consistent responses
- Rate limiting to prevent abuse
- API versioning for backward compatibility
- Pagination for large datasets
- JWT authentication with role-based authorization
- Docker for consistent deployments
- CI/CD for automated quality gates

---

## 📈 Success Metrics

### **After 2 Weeks**

- ✅ Fully functional Task Management System
- ✅ All core features implemented
- ✅ Tests passing with 80%+ coverage
- ✅ Docker Compose working locally
- ✅ CI/CD pipeline green
- ✅ Professional README and documentation

### **After 4-6 Weeks of Practice**

- ✅ Can rebuild project in 1-2 days
- ✅ Can explain every piece of code
- ✅ Can defend architectural decisions
- ✅ Can customize for different domains
- ✅ Confident in senior-level interviews

---

## � Additional Resources

For personal development notes, daily progress tracking, and checkpoint questions, see `PERSONAL_NOTES.md` (not included in the repository).

**Ready to start Day 1?** Let me know when you want to begin! 🚀
