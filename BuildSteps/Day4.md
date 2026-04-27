# Day 4: Resources CRUD

---

## Resource Entity (Domain Layer)

1. Created `Resource` entity in `Domain/Entities/Resource.cs`

## Resource DTOs (Application Layer)

2. Created `ResourceDto` in `Application/Features/Resources/DTOs/ResourceDto.cs`
3. Created `CreateResourceRequest` DTO
4. Created `CreateResourceResponse` DTO
5. Created `UpdateResourceRequest` DTO
6. Created `UpdateResourceResponse` DTO
7. Created `DeleteResourceResponse` DTO

## Resource Commands (CQRS)

8. Created `IResourceRepository` interface in `Application/Common/Interfaces/IResourceRepository.cs`
9. Created `CreateResourceCommand` in `Application/Features/Resources/Commands/CreateResource/`
10. Created `CreateResourceCommandHandler`
11. Created `CreateResourceCommandValidator`
12. Created `UpdateResourceCommand` in `Application/Features/Resources/Commands/UpdateResource/`
13. Created `UpdateResourceCommandHandler`
14. Created `UpdateResourceCommandValidator`
15. Created `DeleteResourceCommand` in `Application/Features/Resources/Commands/DeleteResource/`
16. Created `DeleteResourceCommandHandler`
17. Created `DeleteResourceCommandValidator`

## Resource Queries (CQRS)

18. Created `GetResourceByIdQuery` in `Application/Features/Resources/Queries/GetResourceById/`
19. Created `GetResourceByIdQueryHandler`
20. Created `GetResourceByIdQueryValidator`
21. Created `GetAllResourcesQuery` in `Application/Features/Resources/Queries/GetAllResources/`
22. Created `GetAllResourcesQueryHandler`
23. Created `GetAllResourcesQueryValidator`
24. Added `GetPagedAsync` method to `IRepository<T>` interface
25. Implemented `GetPagedAsync` in `BaseRepository<T>`
26. Implemented `GetPagedAsync` in `TenantRepository`

## Resource Repository Implementation

27. Created `ResourceRepository` in `Infrastructure/Repositories/ResourceRepository.cs`
28. Registered `IResourceRepository` in DI container (`Program.cs`):
    - `builder.Services.AddScoped<IResourceRepository, ResourceRepository>();`

## Resources Controller (API Endpoints)

29. Created `ResourcesController` in `API/Controllers/v1/ResourcesController.cs`

## Database Migration for Resources Table

30. Created migration script `0003_CreateResourcesTable.sql`

31. Executed migration: Rebuild and restart API → DbUp applied 0003_CreateResourcesTable.sql
32. Verified table creation: `docker exec bookingsystem-db psql -U postgres -d BookingSystemDB -c "\d resources"`

## Testing Resources CRUD

33. **Register Tenant (Acme Corp)**:
    `Invoke-RestMethod -Uri "http://localhost:5036/api/v1/auth/register-tenant" -Method POST -Body '{"tenantName":"Acme Corp","email":"admin@acme.com","password":"Admin1234","firstName":"Alice","lastName":"Admin","plan":"Pro"}' -ContentType "application/json"`
    Result: Got JWT token + tenantId `4b47f363-8f8d-4dce-bcec-4ee66d2a2eb4`
34. **Create Resource**:
    `POST /api/v1/resources` with headers (Authorization: Bearer {token}, X-Tenant-Id: {guid})
    Result: Conference Room A created, ID `3ebb51d2-36af-4a56-89e5-17db7eec34a5`
35. **List Resources (Paginated)**:
    `GET /api/v1/resources?pageNumber=1&pageSize=10`
    Result: PagedResult with 1 item, totalCount=1, totalPages=1
36. **Get Resource by ID**:
    `GET /api/v1/resources/3ebb51d2-36af-4a56-89e5-17db7eec34a5`
    Result: Single ResourceDto returned
37. **Update Resource**:
    `PUT /api/v1/resources/3ebb51d2-36af-4a56-89e5-17db7eec34a5`
    Result: Name changed to "Conference Room A (Updated)", capacity 10→12, updatedAt set
38. **Delete Resource**:
    `DELETE /api/v1/resources/3ebb51d2-36af-4a56-89e5-17db7eec34a5`
    Result: 200 OK "Resource deleted successfully"
39. **Verify Deletion**:
    `GET /api/v1/resources`
    Result: Empty list, totalCount=0

## Multi-Tenant Isolation Testing

40. Created resource for Acme Corp: "Acme Meeting Room 1" (ID: `47fd25cb-ad01-4e64-ba9a-4a4e28582b1c`)
41. Registered second tenant: TechCo (`email:admin@techco.com`, tenantId: `fa6b63f7-55ae-4660-b1de-13a7d0258902`)
42. Created resource for TechCo: "TechCo Lab Room" (Laboratory)
43. Registered third tenant: GlobalCorp (`email:admin@globalcorp.com`, tenantId: `e13ea0c3-b658-424b-91da-d286df05703e`)
44. Created resource for GlobalCorp: "GlobalCorp Boardroom" (ID: `b1846791-47f2-4a27-860f-40977d1feb18`)
45. **Verified database isolation**: `docker exec bookingsystem-db psql -U postgres -d BookingSystemDB -c "SELECT id, name, resourcetype, tenantid FROM resources"`
    Result: 3 resources, each with different tenantId
