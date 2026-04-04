# Bulletproof .NET React - Project Roadmap

## 🎯 Project Goal

Create a **Multi-tenant Booking System** that demonstrates senior full-stack developer skills and can be rebuilt from memory in 1.5-2 days after practice.

**Timeline**: 2 weeks of focused development  
**Domain**: Multi-tenant Resource Booking (Meeting rooms, Equipment, Appointments)  
**End Goal**: Pass senior fullstack developer interviews with enterprise-level architecture

---

## 📊 Project Scope

### Core Entities

1. **Tenants** - Organizations owning the booking system (subdomain, plan)
2. **Users** - Tenant-scoped authentication, roles (TenantAdmin, Manager, User)
3. **Roles** - Permission levels
4. **UserRoles** - Many-to-many relationship
5. **Resources** - Bookable items (meeting rooms, equipment, doctors, etc.)
6. **Bookings** - Reservations with time slots and status
7. **AvailabilityRules** - Resource availability schedules
8. **AuditLogs** - Track all changes for compliance
9. **RefreshTokens** - Secure token management

### Must-Have Features

- ✅ Multi-tenant data isolation (all queries filtered by TenantId)
- ✅ User registration and JWT authentication (tenant-scoped)
- ⏳ CRUD operations for Resources and Bookings (Resources complete, Bookings in progress)
- ⏳ Booking conflict prevention (interface defined, implementation pending)
- ⏳ Status workflow (commands created: Pending → Confirmed → Completed/Cancelled)
- [ ] Availability rules (working hours per resource)
- ✅ Pagination and filtering (PagedResult<T> pattern)
- ✅ Tenant-level and role-based authorization
- [ ] Audit logging for all changes
- ✅ Global error handling
- ✅ API versioning (v1)
- ✅ Swagger with JWT auth
- [ ] Rate limiting

### Testing

- [ ] Backend unit tests (business logic)
- [ ] Backend integration tests (API endpoints)
- [ ] Frontend component tests
- [ ] E2E tests (critical paths)

### DevOps

- ✅ Dockerfiles (backend, frontend)
- ✅ docker-compose with PostgreSQL and NGINX
- ✅ GitHub Actions CI/CD
- ✅ README with architecture diagram

---

## 📅 Two-Week Development Plan

### **Week 1: Backend Foundation + Core Features**

#### **Day 1: Project Setup & Architecture**

- [x] Initialize .NET solution with Clean Architecture structure
  - `API` project (ASP.NET Core Web API)
  - `Application` project (business logic, CQRS)
  - `Domain` project (entities, interfaces)
  - `Infrastructure` project (Dapper, PostgreSQL)
  - Test projects (Unit, Integration, Architecture)
- [x] Set up PostgreSQL with Docker
- [x] Configure Dapper and Npgsql
- [x] Set up MediatR for CQRS
- [x] Configure Serilog for logging
- [x] Add FluentValidation
- [x] Set up AutoMapper

**Learning Focus**: Clean Architecture layers, project dependencies

---

#### **Day 2: Core Infrastructure & Multi-tenancy** ✅ COMPLETE

- [x] Implement global error handling middleware
- [x] Set up API versioning (v1)
- [x] Configure Swagger/OpenAPI
- [x] Implement `PagedResult<T>` pattern
- [x] Set up database migrations (DbUp)
- [x] Create database schema (Tenants, Users, Roles, UserRoles tables with UUID)
- [x] Implement base repository pattern with Dapper
- [x] Create TenantContext service for tenant resolution
- [x] Implement multi-tenant query filter middleware

**Learning Focus**: Middleware pipeline, multi-tenancy patterns, Dapper queries

---

#### **Day 3: Authentication & Authorization** ✅ COMPLETE

- [x] Implement Tenant and User entities with tenant isolation
- [x] Create authentication commands (RegisterTenant, RegisterUser, Login)
- [x] JWT token generation with TenantId claim
- [x] Password hashing (BCrypt)
- [x] Configure JWT authentication in pipeline
- [x] Implement tenant-level + role-based authorization
- [x] Create authorization policies (RequireTenantAdmin, RequireManager)
- [x] Add JWT auth to Swagger

**Learning Focus**: JWT flow, multi-tenant auth, claims-based authorization

---

#### **Day 4: Resources CRUD** ✅ COMPLETE

- [x] Implement Resource entity (tenant-scoped)
- [x] Create Resource commands (Create, Update, Delete)
- [x] Create Resource queries (GetById, GetAll with pagination, filtering by type)
- [x] Implement Resource validation rules
- [x] Add Dapper repositories for Resources with tenant filtering
- [x] Create Resource DTOs
- [x] Implement Resource API endpoints
- [x] Add authorization (TenantAdmin/Manager can manage resources)
- [x] Test multi-tenant isolation with 3 tenants

**Learning Focus**: CQRS pattern, tenant-scoped queries

---

#### **Day 5: Bookings CRUD & Business Logic** ✅ COMPLETE

- [x] Implement Booking entity with status enum (tenant-scoped)
- [x] Create Booking DTOs (8 files: BookingDto, Create/Update/Cancel/Confirm/Delete Request/Response)
- [x] Create Booking commands (Create, Update, Cancel, Confirm, Delete) with FluentValidation
- [x] Create Booking queries (GetById, GetAll with filtering by resource/user/status/dates)
- [x] Create IBookingRepository interface with HasConflictAsync method
- [x] Implement BookingRepository with Dapper (conflict detection SQL, CRUD operations)
- [x] Create BookingsController with REST endpoints
- [x] Create database migration (0006_CreateBookingsTable.sql)
- [x] Register IBookingRepository in DI container
- [x] Test booking operations and conflict detection
- [x] Implemented ICurrentUserService for JWT userId extraction
- [x] Created BookingMappingProfile for AutoMapper
- [x] Added ManagerOrAbove authorization policy

**Learning Focus**: Complex business logic, time-based validation, conflict resolution

---

#### **Day 6: Advanced Features & Audit Logging** 🔄 IN PROGRESS

**Audit Logging** ✅ COMPLETE:

- [x] Create AuditLog entity and repository ✅
- [x] Implement audit logging behavior (MediatR pipeline to track all CUD operations) ✅
- [x] Create database migration for AuditLogs table ✅
- [x] Register audit logging services in DI ✅
- [x] Test audit logging (verify entries created for Create/Update/Delete/Cancel/Confirm) ✅

**AvailabilityRules** 🔄 IN PROGRESS (44% - Step 4 of 9 complete):

- [x] Step 1: Create AvailabilityRule entity (13 properties) ✅
- [x] Step 2: Create DTOs (6 classes with computed fields) ✅
- [x] Step 3: Create CQRS Commands (9 files: Create/Update/Delete Command/Handler/Validator + IAvailabilityRuleRepository interface) ✅
- [x] Step 4: Create CQRS Queries (6 files: GetById/GetAll Query/Handler/Validator + updated IAvailabilityRuleRepository with GetPagedAsync) ✅
- [ ] Step 5: Implement AvailabilityRuleRepository with Dapper
- [ ] Step 6: Create AvailabilityRulesController with REST endpoints
- [ ] Step 7: Create database migration (0008_CreateAvailabilityRulesTable.sql)
- [ ] Step 8: Register repository in DI container
- [ ] Step 9: Test AvailabilityRules CRUD operations

**Other Advanced Features** (after AvailabilityRules):

- [ ] Create admin endpoints to view audit logs (optional GetAuditLogs query)
- [ ] Implement soft delete for entities (IsDeleted flag, update queries)
- [ ] Add filtering and sorting to booking queries (date range, status, resourceId)
- [ ] Create statistics endpoints (booking counts by status, resource utilization %, popular time slots)
- [ ] Add rate limiting middleware (AspNetCoreRateLimit for DoS protection)
- [ ] Implement RefreshToken mechanism (token rotation, expiration handling)

**Learning Focus**: Audit patterns, MediatR pipeline behaviors, enterprise security, advanced queries

**Current Status**: ✅ **Audit logging complete!** 🔄 **AvailabilityRules in progress** - Commands done, now need Queries.

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

#### **Day 9: Authentication Flow & Tenant Setup**

- [ ] Create auth Redux slice with async thunks
- [ ] Implement token storage with tenant context
- [ ] Add axios interceptors (token injection, tenant header, refresh logic)
- [ ] Create protected route wrapper
- [ ] Implement tenant registration form (first user becomes TenantAdmin)
- [ ] Implement login form with React Hook Form + Zod
- [ ] Implement user registration form (within tenant)
- [ ] Add form validation and error display
- [ ] Create navigation with user menu (show tenant name)
- [ ] Add logout functionality

**Learning Focus**: Multi-tenant frontend architecture, React Hook Form, auth flow

---

#### **Day 10: Resources Feature**

- [ ] Create resources Redux slice with async thunks
- [ ] Implement resources list page with pagination
- [ ] Create resource card components (show type, capacity, availability)
- [ ] Implement create resource form (modal/page)
- [ ] Implement edit resource functionality
- [ ] Implement delete resource (with confirmation)
- [ ] Add loading states and error handling
- [ ] Implement search/filter for resources (by type, availability)

**Learning Focus**: Redux Toolkit patterns, CRUD operations, tenant-scoped data

---

#### **Day 11: Bookings Feature**

- [ ] Create bookings Redux slice with async thunks
- [ ] Implement bookings calendar view (day/week/month)
- [ ] Create booking card components
- [ ] Implement create booking form with date/time pickers
- [ ] Implement conflict detection UI feedback
- [ ] Implement edit/cancel booking functionality
- [ ] Add booking filtering (date range, resource, status, user)
- [ ] Implement booking status management (Confirm, Cancel)
- [ ] Add booking details view
- [ ] Show resource availability in real-time

**Learning Focus**: Calendar UI, complex date/time handling, conflict visualization

---

#### **Day 12: Frontend Testing & Polish**

- [ ] Set up Vitest and React Testing Library
- [ ] Write component tests (forms, cards, lists)
- [ ] Write hook tests (custom hooks)
- [ ] Test Redux slices and thunks with MSW
- [ ] Set up Playwright or Cypress
- [ ] Write E2E test for authentication flow
- [ ] Write E2E test for creating resource and booking
- [ ] Write E2E test for booking conflict prevention
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
