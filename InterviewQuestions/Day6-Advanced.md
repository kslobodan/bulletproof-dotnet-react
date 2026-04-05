# Day 6: Advanced Features & Audit Logging

[← Back to Index](./README.md) | [← Previous: Day 5](./Day5-Bookings.md)

---

## Q: "Explain how your audit logging works. How do you intercept all CUD operations automatically?"

**A:** "I implemented audit logging using MediatR's `IPipelineBehavior<TRequest, TResponse>`, which intercepts all commands before and after execution. Here's how it works:

**MediatR Pipeline Architecture:**

```
Client → MediatR → [Pipeline Behaviors] → Command Handler → Database
                         ↓
                   AuditLoggingBehavior
                   (Before & After)
```

**The AuditLoggingBehavior Implementation:**

```csharp
public class AuditLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ITenantContext _tenantContext;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. Check if this command should be audited
        if (!IsAuditableCommand(typeof(TRequest).Name))
            return await next(); // Skip logging for queries

        // 2. Parse command name to extract entity and action
        var (entityName, action) = ParseCommandName(typeof(TRequest).Name);

        // 3. Serialize "before" state (for updates)
        var oldValues = action == "Update"
            ? SerializeOldValues(request)
            : null;

        // 4. Execute the actual command handler
        var response = await next();

        // 5. Extract entity ID from response
        var entityId = ExtractEntityId(response);

        // 6. Serialize "after" state
        var newValues = SerializeNewValues(response);

        // 7. Save audit log asynchronously (fire-and-forget)
        _ = Task.Run(async () =>
        {
            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                UserId = _currentUserService.UserId,
                Timestamp = DateTime.UtcNow
            };
            await _auditLogRepository.AddAsync(auditLog);
        }, cancellationToken);

        return response;
    }
}
```

**Key Design Decisions:**

1. **Command Name Parsing**: `CreateBookingCommand` → Entity: "Booking", Action: "Create"
2. **Selective Auditing**: Only logs Create/Update/Delete/Cancel/Confirm operations, not queries
3. **Fire-and-Forget**: Audit logging happens asynchronously to not slow down the main request
4. **Reflection for Flexibility**: Uses reflection to extract entity IDs from different response types
5. **JSON Serialization**: Stores old/new values as JSON for flexibility

**Registration in DI:**

```csharp
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Application.AssemblyReference).Assembly);
    cfg.AddOpenBehavior(typeof(AuditLoggingBehavior<,>)); // Open generic registration
});
```

**Why This Approach?**

- **Zero Code Duplication**: Don't need to add audit logging code to every handler
- **Consistent**: All commands are audited the same way
- **Maintainable**: Add new auditable commands without changing audit logic
- **Testable**: Can test audit logic independently from business logic
- **Compliance Ready**: Automatically tracks who did what, when, and what changed"

---

## Q: "Why use TIME type instead of TIMESTAMP for AvailabilityRules StartTime and EndTime? What's the difference?"

**A:** "The `TIME` type is perfect for recurring daily schedules, while `TIMESTAMP` is for specific moments in time. Here's the key difference:

**TIME Type (What We Used):**

```sql
CREATE TABLE AvailabilityRules (
    DayOfWeek INT NOT NULL,      -- 0=Sunday, 1=Monday, ..., 6=Saturday
    StartTime TIME NOT NULL,     -- e.g., '09:00:00' (every Monday at 9 AM)
    EndTime TIME NOT NULL        -- e.g., '17:00:00' (every Monday until 5 PM)
);

-- Insert: "Meeting room available Monday 9 AM - 5 PM"
INSERT INTO AvailabilityRules (DayOfWeek, StartTime, EndTime)
VALUES (1, '09:00:00', '17:00:00');
```

**TIMESTAMP Type (What We Avoided):**

```sql
-- BAD: Would force us to specify exact dates
StartTime TIMESTAMP,      -- '2026-04-07 09:00:00' (specific Monday)
EndTime TIMESTAMP         -- '2026-04-07 17:00:00'
-- Problem: Only works for ONE specific Monday!
```

**Why TIME is Correct Here:**

| Aspect         | TIME                           | TIMESTAMP                          |
| -------------- | ------------------------------ | ---------------------------------- |
| **Represents** | Time of day (09:00)            | Specific moment (2026-04-07 09:00) |
| **Recurrence** | Repeats every week             | One-time event                     |
| **Storage**    | 8 bytes                        | 8 bytes                            |
| **Range**      | 00:00:00 - 23:59:59            | Full date + time                   |
| **Use Case**   | Daily schedules, working hours | Appointments, deadlines            |

**Real-World Example:**

```csharp
// AvailabilityRule: "Meeting Room A available Monday-Friday 9 AM - 5 PM"
var rules = new[]
{
    new AvailabilityRule
    {
        DayOfWeek = DayOfWeek.Monday,
        StartTime = TimeSpan.FromHours(9),   // 09:00:00
        EndTime = TimeSpan.FromHours(17)     // 17:00:00
    },
    // ... repeat for Tuesday through Friday
};

// Booking: "John books Meeting Room A on 2026-04-07 at 10:00 AM"
var booking = new Booking
{
    StartTime = new DateTime(2026, 4, 7, 10, 0, 0),  // Specific moment
    EndTime = new DateTime(2026, 4, 7, 11, 0, 0)
};

// Check: Is the room available on Monday at 10 AM?
var dayOfWeek = booking.StartTime.DayOfWeek;  // Monday
var timeOfDay = booking.StartTime.TimeOfDay;  // 10:00:00

var rule = rules.FirstOrDefault(r =>
    r.DayOfWeek == dayOfWeek &&
    timeOfDay >= r.StartTime &&
    timeOfDay < r.EndTime &&
    r.IsActive);

bool isAvailable = rule != null;  // True - 10:00 is between 09:00 and 17:00
```

**C# TimeSpan vs DateTime:**

```csharp
// TIME in PostgreSQL → TimeSpan in C#
public TimeSpan StartTime { get; set; }  // 09:00:00 (duration from midnight)

// TIMESTAMP in PostgreSQL → DateTime in C#
public DateTime CreatedAt { get; set; }  // 2026-04-07 09:00:00 (absolute moment)
```

**Database Check Constraint:**

```sql
-- Ensures end time is after start time (e.g., 17:00 > 09:00)
CONSTRAINT CK_AvailabilityRules_ValidTimeRange CHECK (EndTime > StartTime)
```

---

## Q: "Walk me through your dynamic WHERE clause building in GetPagedAsync. How do you prevent SQL injection with optional filters?"

**A:** "I use Dapper's `DynamicParameters` to build dynamic WHERE clauses safely, preventing SQL injection while allowing flexible filtering:

**The Repository Method:**

```csharp
public async Task<PagedResult<AvailabilityRule>> GetPagedAsync(
    int pageNumber,
    int pageSize,
    Guid? resourceId = null,      // Optional filter
    DayOfWeek? dayOfWeek = null,  // Optional filter
    bool? isActive = null)        // Optional filter
{
    using var connection = _connectionFactory.CreateConnection();

    // 1. Build dynamic WHERE conditions
    var whereConditions = new List<string> { "TenantId = @TenantId" };
    var parameters = new DynamicParameters();
    parameters.Add("TenantId", TenantId);

    // 2. Add optional filters only if provided
    if (resourceId.HasValue)
    {
        whereConditions.Add("ResourceId = @ResourceId");
        parameters.Add("ResourceId", resourceId.Value);
    }

    if (dayOfWeek.HasValue)
    {
        whereConditions.Add("DayOfWeek = @DayOfWeek");
        parameters.Add("DayOfWeek", (int)dayOfWeek.Value); // Enum to int
    }

    if (isActive.HasValue)
    {
        whereConditions.Add("IsActive = @IsActive");
        parameters.Add("IsActive", isActive.Value);
    }

    // 3. Join conditions with AND
    var whereClause = string.Join(" AND ", whereConditions);
    // Result: "TenantId = @TenantId AND ResourceId = @ResourceId AND IsActive = @IsActive"

    // 4. Build COUNT query
    var countSql = $@"
        SELECT COUNT(*)::int FROM AvailabilityRules
        WHERE {whereClause}";

    var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

    // 5. Build data query with pagination
    var offset = (pageNumber - 1) * pageSize;
    parameters.Add("PageSize", pageSize);
    parameters.Add("Offset", offset);

    var dataSql = $@"
        SELECT * FROM AvailabilityRules
        WHERE {whereClause}
        ORDER BY DayOfWeek, StartTime
        LIMIT @PageSize OFFSET @Offset";

    var items = await connection.QueryAsync<AvailabilityRule>(dataSql, parameters);

    return new PagedResult<AvailabilityRule>(
        items.ToList(),
        totalCount,
        pageNumber,
        pageSize
    );
}
```

**SQL Injection Prevention:**

```csharp
// ❌ VULNERABLE: String concatenation
var sql = $"WHERE ResourceId = '{resourceId}'";  // Can be injected!

// ✅ SAFE: Parameterized query
parameters.Add("ResourceId", resourceId);
var sql = "WHERE ResourceId = @ResourceId";  // Parameter placeholder
```

**Dynamic Query Examples:**

```csharp
// Request 1: All rules
GetPagedAsync(1, 10)
// SQL: WHERE TenantId = @TenantId

// Request 2: Only active rules
GetPagedAsync(1, 10, isActive: true)
// SQL: WHERE TenantId = @TenantId AND IsActive = @IsActive

// Request 3: Monday rules for specific resource
GetPagedAsync(1, 10, resourceId: someId, dayOfWeek: DayOfWeek.Monday)
// SQL: WHERE TenantId = @TenantId AND ResourceId = @ResourceId AND DayOfWeek = @DayOfWeek
```

**Why This Approach?**

1. **Flexible**: Supports any combination of filters
2. **Safe**: All parameters use @placeholders, preventing SQL injection
3. **Efficient**: Only includes filters that are actually used
4. **Type-Safe**: DynamicParameters handles type conversions (enum to int)
5. **Maintainable**: Easy to add new filters without changing the core logic"

---

## Q: "Why did you create 6 indexes for AvailabilityRules? Explain the composite indexes."

**A:** "Every index was designed for a specific query pattern based on how the application uses availability rules:

**Index Strategy:**

```sql
-- 1. PRIMARY KEY (automatic)
CREATE UNIQUE INDEX availabilityrules_pkey ON AvailabilityRules(Id);
-- Query: SELECT * FROM AvailabilityRules WHERE Id = ?
-- Usage: GetById operations

-- 2. Tenant filtering (most common)
CREATE INDEX ix_availabilityrules_tenantid ON AvailabilityRules(TenantId);
-- Query: SELECT * FROM AvailabilityRules WHERE TenantId = ?
-- Usage: All tenant-scoped queries

-- 3. Resource lookup
CREATE INDEX ix_availabilityrules_resourceid ON AvailabilityRules(ResourceId);
-- Query: SELECT * FROM AvailabilityRules WHERE ResourceId = ?
-- Usage: GetByResourceIdAsync - "Show me all rules for Meeting Room A"

-- 4. Day of week filtering
CREATE INDEX ix_availabilityrules_dayofweek ON AvailabilityRules(DayOfWeek);
-- Query: SELECT * FROM AvailabilityRules WHERE DayOfWeek = ?
-- Usage: "Show me all Monday rules across all resources"

-- 5. Active status filtering
CREATE INDEX ix_availabilityrules_isactive ON AvailabilityRules(IsActive);
-- Query: SELECT * FROM AvailabilityRules WHERE IsActive = true
-- Usage: "Show me only active rules"

-- 6. COMPOSITE: Resource lookup with ordering
CREATE INDEX ix_availabilityrules_resourcelookup
    ON AvailabilityRules(TenantId, ResourceId, DayOfWeek, StartTime);
-- Query: SELECT * FROM AvailabilityRules
--        WHERE TenantId = ? AND ResourceId = ?
--        ORDER BY DayOfWeek, StartTime
-- Usage: GetByResourceIdAsync with proper ordering (most common pattern)

-- 7. COMPOSITE: Multi-filter queries
CREATE INDEX ix_availabilityrules_filterlookup
    ON AvailabilityRules(TenantId, DayOfWeek, IsActive);
-- Query: SELECT * FROM AvailabilityRules
--        WHERE TenantId = ? AND DayOfWeek = ? AND IsActive = ?
-- Usage: GetPagedAsync with multiple filters
```

**How Composite Indexes Work:**

```
Index: (TenantId, ResourceId, DayOfWeek, StartTime)

PostgreSQL can use this index for:
✅ WHERE TenantId = ?
✅ WHERE TenantId = ? AND ResourceId = ?
✅ WHERE TenantId = ? AND ResourceId = ? AND DayOfWeek = ?
✅ WHERE TenantId = ? AND ResourceId = ? AND DayOfWeek = ? AND StartTime = ?
✅ WHERE TenantId = ? AND ResourceId = ? ORDER BY DayOfWeek, StartTime
❌ WHERE ResourceId = ?  (doesn't start with TenantId - can't use this index)
❌ WHERE DayOfWeek = ?   (doesn't start with TenantId - can't use this index)
```

**Index Column Order Matters:**

```sql
-- GOOD: Matches our query pattern
INDEX (TenantId, ResourceId, DayOfWeek, StartTime)
Query: WHERE TenantId = ? AND ResourceId = ? ORDER BY DayOfWeek, StartTime
-- ✅ Uses index for WHERE and ORDER BY

-- BAD: Wrong order
INDEX (StartTime, DayOfWeek, ResourceId, TenantId)
Query: WHERE TenantId = ? AND ResourceId = ? ORDER BY DayOfWeek, StartTime
-- ❌ Cannot use index efficiently
```

**Real-World Performance:**

```csharp
// Without composite index - needs 2 operations:
// 1. Full table scan filtering WHERE TenantId AND ResourceId
// 2. Sort in memory by DayOfWeek, StartTime
Time: ~100ms with 10,000 rules

// With composite index - single index scan:
// 1. Index seeks directly to TenantId + ResourceId
// 2. Reads in sorted order (no additional sort needed)
Time: ~2ms with 10,000 rules (50x faster!)
```

**Trade-offs:**

- **Pros**: Dramatically faster queries (milliseconds vs seconds)
- **Cons**:
  - Indexes take disk space (~200 bytes per row)
  - INSERT/UPDATE slightly slower (must update 7 indexes)
  - For AvailabilityRules, reads >>> writes, so worth it"

---

## Q: "How does your AvailabilityRule entity prevent invalid data at the database level?"

**A:** "I use PostgreSQL CHECK constraints to enforce business rules directly in the database, providing defense-in-depth beyond application validation:

**The Three Check Constraints:**

```sql
CREATE TABLE AvailabilityRules (
    -- ... columns ...

    -- 1. Ensure end time is after start time
    CONSTRAINT CK_AvailabilityRules_ValidTimeRange
        CHECK (EndTime > StartTime),

    -- 2. Ensure day of week is valid (0-6)
    CONSTRAINT CK_AvailabilityRules_ValidDayOfWeek
        CHECK (DayOfWeek >= 0 AND DayOfWeek <= 6),

    -- 3. Ensure effective date range is valid
    CONSTRAINT CK_AvailabilityRules_ValidDateRange
        CHECK (EffectiveTo IS NULL OR EffectiveFrom IS NULL OR EffectiveTo > EffectiveFrom)
);
```

**Constraint 1: Valid Time Range**

```sql
-- ✅ ALLOWED: 9 AM to 5 PM
INSERT INTO AvailabilityRules (StartTime, EndTime)
VALUES ('09:00:00', '17:00:00');

-- ❌ REJECTED: End time before start time
INSERT INTO AvailabilityRules (StartTime, EndTime)
VALUES ('17:00:00', '09:00:00');
-- ERROR: new row violates check constraint "ck_availabilityrules_validtimerange"

-- ❌ REJECTED: Same time (no duration)
INSERT INTO AvailabilityRules (StartTime, EndTime)
VALUES ('09:00:00', '09:00:00');
-- ERROR: EndTime must be > StartTime (not >=)
```

**Constraint 2: Valid Day of Week**

```csharp
// DayOfWeek enum in C#
public enum DayOfWeek
{
    Sunday = 0,
    Monday = 1,
    Tuesday = 2,
    Wednesday = 3,
    Thursday = 4,
    Friday = 5,
    Saturday = 6
}

// ✅ ALLOWED: Valid days
INSERT VALUES (0);  -- Sunday
INSERT VALUES (6);  -- Saturday

// ❌ REJECTED: Invalid day
INSERT VALUES (7);  -- Not a valid day
-- ERROR: new row violates check constraint "ck_availabilityrules_validdayofweek"

INSERT VALUES (-1); -- Negative day
-- ERROR: same constraint violation
```

**Constraint 3: Valid Date Range (Nullable Logic)**

```sql
-- ✅ ALLOWED: No date restrictions (permanent rule)
INSERT VALUES (EffectiveFrom: NULL, EffectiveTo: NULL);

-- ✅ ALLOWED: Only start date (open-ended)
INSERT VALUES (EffectiveFrom: '2026-01-01', EffectiveTo: NULL);

-- ✅ ALLOWED: Only end date (temporary until date)
INSERT VALUES (EffectiveFrom: NULL, EffectiveTo: '2026-12-31');

-- ✅ ALLOWED: Both dates with end after start
INSERT VALUES (EffectiveFrom: '2026-01-01', EffectiveTo: '2026-12-31');

-- ❌ REJECTED: End date before start date
INSERT VALUES (EffectiveFrom: '2026-12-31', EffectiveTo: '2026-01-01');
-- ERROR: new row violates check constraint "ck_availabilityrules_validdaterange"
```

**Why Check Constraints Matter:**

```csharp
// Scenario: Developer bypasses validation and inserts directly via SQL
-- Without constraints:
INSERT INTO AvailabilityRules (StartTime, EndTime) VALUES ('17:00', '09:00');
-- ✅ Inserted successfully (but data is invalid!)

-- With constraints:
INSERT INTO AvailabilityRules (StartTime, EndTime) VALUES ('17:00', '09:00');
-- ❌ ERROR: Check constraint violation (data integrity protected!)
```

**Defense in Depth:**

```
Layer 1: FluentValidation in Application layer
         ↓ (validates command input)
Layer 2: Business logic in Command Handlers
         ↓ (validates business rules)
Layer 3: Database CHECK constraints
         ↓ (final defense - cannot bypass)
Database: Data integrity guaranteed ✅
```

**Benefits:**

1. **Cannot be bypassed**: Even direct database access respects constraints
2. **Self-documenting**: Constraints show business rules in schema
3. **Performance**: Database checks constraints at millisecond speed
4. **Consistency**: Same rules apply regardless of application layer
5. **Migration safety**: Old versions of app can't insert invalid data"

---

## Q: "Wal me through how AvailabilityRules fit into your architecture. How do the layers interact?"

**A:** "AvailabilityRules follow Clean Architecture and CQRS pattern. Here's the complete flow:

**Architecture Layers:**

```
┌─────────────────────────────────────────────┐
│         API Layer (BookingSystem.API)        │
│  - AvailabilityRulesController              │
│  - Handles HTTP requests/responses           │
│  - Authorization (ManagerOrAbove)            │
└──────────────────┬──────────────────────────┘
                   │ (MediatR commands/queries)
┌──────────────────▼──────────────────────────┐
│    Application Layer (Application)           │
│  - Commands: Create/Update/Delete            │
│  - Queries: GetById/GetAll                   │
│  - Validators: FluentValidation              │
│  - DTOs: Request/Response objects            │
│  - Interfaces: IAvailabilityRuleRepository   │
└──────────────────┬──────────────────────────┘
                   │ (Domain entities)
┌──────────────────▼──────────────────────────┐
│       Domain Layer (Domain)                  │
│  - AvailabilityRule entity                   │
│  - Business rules and invariants             │
└──────────────────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────┐
│  Infrastructure Layer (Infrastructure)       │
│  - AvailabilityRuleRepository (Dapper)       │
│  - Database migrations                       │
│  - PostgreSQL connection                     │
└─────────────────────────────────────────────┘
```

**Create Availability Rule Flow:**

```
1. Client → POST /api/v1/availabilityrules
   Body: { resourceId, dayOfWeek: 1, startTime: "09:00", endTime: "17:00" }

2. AvailabilityRulesController.Create()
   - [Authorize(Policy = "ManagerOrAbove")] ← Authorization check
   - Maps request to CreateAvailabilityRuleCommand
   - await _mediator.Send(command)

3. MediatR Pipeline
   - AuditLoggingBehavior intercepts
   - CreateAvailabilityRuleCommandValidator runs
     ✓ ResourceId not empty
     ✓ DayOfWeek is valid enum (0-6)
     ✓ StartTime < EndTime
     ✓ Valid time range (00:00-23:59)

4. CreateAvailabilityRuleCommandHandler.Handle()
   var rule = new AvailabilityRule
   {
       Id = Guid.NewGuid(),                          // Auto-generated
       TenantId = _tenantContext.TenantId,          // From JWT claims
       ResourceId = request.ResourceId,
       DayOfWeek = request.DayOfWeek,
       StartTime = request.StartTime,
       EndTime = request.EndTime,
       IsActive = request.IsActive,
       EffectiveFrom = request.EffectiveFrom,
       EffectiveTo = request.EffectiveTo,
       CreatedAt = DateTime.UtcNow                  // Auto-timestamp
   };

   await _availabilityRuleRepository.AddAsync(rule);

5. AvailabilityRuleRepository.AddAsync()
   - Convert TimeSpan to SQL TIME
   - Convert DayOfWeek enum to int

   INSERT INTO AvailabilityRules (
       Id, TenantId, ResourceId, DayOfWeek,
       StartTime, EndTime, IsActive, EffectiveFrom, EffectiveTo, CreatedAt
   )
   VALUES (
       @Id, @TenantId, @ResourceId, @DayOfWeek,
       @StartTime, @EndTime, @IsActive, @EffectiveFrom, @EffectiveTo, @CreatedAt
   )

6. PostgreSQL
   - CHECK constraint: EndTime > StartTime ✓
   - CHECK constraint: DayOfWeek 0-6 ✓
   - FK constraint: ResourceId exists in Resources ✓
   - FK constraint: TenantId exists in Tenants ✓
   - Row inserted successfully

7. CreateAvailabilityRuleCommandHandler (continued)
   - Convert entity to DTO with computed fields:
     * DayOfWeekText = "Monday"
     * StartTimeText = "09:00"
     * EndTimeText = "17:00"
     * DurationMinutes = 480

   return new CreateAvailabilityRuleResponse
   {
       AvailabilityRule = dto,
       Message = "Availability rule created successfully"
   };

8. AuditLoggingBehavior (after handler)
   - Creates audit log entry:
     * EntityName = "AvailabilityRule"
     * Action = "Create"
     * EntityId = rule.Id
     * NewValues = JSON serialization of response
     * UserId = from JWT
   - Saves asynchronously (fire-and-forget)

9. Controller
   return CreatedAtAction(
       nameof(GetById),
       new { id = result.AvailabilityRule.Id },
       result
   );

10. Client ← 201 Created
    Location: /api/v1/availabilityrules/{id}
    Body: { availabilityRule: { id, dayOfWeekText: "Monday", ... }, message: "..." }
```

**Query Flow (GetAll with filters):**

```
1. Client → GET /api/v1/availabilityrules?resourceId={id}&dayOfWeek=1&isActive=true&pageNumber=1&pageSize=10

2. AvailabilityRulesController.GetAll()
   var query = new GetAllAvailabilityRulesQuery
   {
       PageNumber = 1,
       PageSize = 10,
       ResourceId = resourceId,
       DayOfWeek = DayOfWeek.Monday,
       IsActive = true
   };

3. GetAllAvailabilityRulesQueryValidator
   ✓ PageNumber > 0
   ✓ PageSize 1-100
   ✓ DayOfWeek is valid enum

4. GetAllAvailabilityRulesQueryHandler
   var pagedResult = await _repository.GetPagedAsync(
       query.PageNumber,
       query.PageSize,
       query.ResourceId,
       query.DayOfWeek,
       query.IsActive
   );

5. AvailabilityRuleRepository.GetPagedAsync()
   - Builds dynamic WHERE clause:
     "TenantId = @TenantId AND ResourceId = @ResourceId AND DayOfWeek = @DayOfWeek AND IsActive = @IsActive"

   - Uses composite index: ix_availabilityrules_filterlookup (TenantId, DayOfWeek, IsActive)

   - Counts total: SELECT COUNT(*)::int WHERE ...
   - Fetches page: SELECT * WHERE ... ORDER BY DayOfWeek, StartTime LIMIT 10 OFFSET 0

6. Handler converts entities to DTOs
   dtos = rules.Select(r => new AvailabilityRuleDto
   {
       DayOfWeekText = r.DayOfWeek.ToString(),  // "Monday"
       StartTimeText = r.StartTime.ToString(@"hh\:mm"),  // "09:00"
       DurationMinutes = (r.EndTime - r.StartTime).TotalMinutes  // 480
   });

7. Client ← 200 OK
   {
       items: [...],
       totalCount: 25,
       pageNumber: 1,
       pageSize: 10,
       totalPages: 3,
       hasPreviousPage: false,
       hasNextPage: true
   }
```

**Dependency Direction (Clean Architecture):**

```
API        →  depends on  →  Application
Application →  depends on  →  Domain
Infrastructure → depends on → Application (interfaces only)

Domain has ZERO dependencies (pure business logic)
```

"

---

## 📝 Code Exercises

### Exercise 1: Implement Weekend Validation

**Task**: Modify `CreateAvailabilityRuleCommandValidator` to reject availability rules for weekends (Saturday and Sunday) for resources of type "Office".

```csharp
// Add this validation rule:
// "Office resources cannot have availability rules on weekends"

// Hint: You'll need to inject IResourceRepository to check resource type
```

<details>
<summary>Solution</summary>

```csharp
public class CreateAvailabilityRuleCommandValidator : AbstractValidator<CreateAvailabilityRuleCommand>
{
    private readonly IResourceRepository _resourceRepository;

    public CreateAvailabilityRuleCommandValidator(IResourceRepository resourceRepository)
    {
        _resourceRepository = resourceRepository;

        RuleFor(x => x.ResourceId)
            .NotEmpty().WithMessage("ResourceId is required");

        RuleFor(x => x.DayOfWeek)
            .IsInEnum().WithMessage("Invalid day of week")
            .MustAsync(async (command, dayOfWeek, cancellationToken) =>
            {
                // Check if resource is Office type
                var resource = await _resourceRepository.GetByIdAsync(command.ResourceId, cancellationToken);
                if (resource == null) return true; // Let other validation handle missing resource

                // If Office type, reject weekends
                if (resource.ResourceType == "Office")
                {
                    return dayOfWeek != DayOfWeek.Saturday && dayOfWeek != DayOfWeek.Sunday;
                }

                return true; // Non-office resources can have weekend rules
            })
            .WithMessage("Office resources cannot have availability rules on weekends");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("StartTime is required")
            .Must(BeValidTimeSpan).WithMessage("StartTime must be between 00:00 and 23:59");

        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("EndTime is required")
            .Must(BeValidTimeSpan).WithMessage("EndTime must be between 00:00 and 23:59")
            .GreaterThan(x => x.StartTime).WithMessage("EndTime must be after StartTime");

        RuleFor(x => x.EffectiveTo)
            .GreaterThan(x => x.EffectiveFrom)
            .When(x => x.EffectiveFrom.HasValue && x.EffectiveTo.HasValue)
            .WithMessage("EffectiveTo must be after EffectiveFrom");
    }

    private bool BeValidTimeSpan(TimeSpan time)
    {
        return time >= TimeSpan.Zero && time < TimeSpan.FromDays(1);
    }
}
```

</details>

---

### Exercise 2: Query Active Rules for Today

**Task**: Write a method `GetActiveRulesForToday()` that returns all active availability rules for the current day of the week.

```csharp
// Should return all rules where:
// - DayOfWeek matches DateTime.Today.DayOfWeek
// - IsActive = true
// - EffectiveFrom <= Today (or NULL)
// - EffectiveTo >= Today (or NULL)

public async Task<IEnumerable<AvailabilityRuleDto>> GetActiveRulesForToday()
{
    // Your code here
}
```

<details>
<summary>Solution</summary>

```csharp
public class GetActiveRulesForTodayQueryHandler : IRequestHandler<GetActiveRulesForTodayQuery, IEnumerable<AvailabilityRuleDto>>
{
    private readonly IAvailabilityRuleRepository _repository;
    private readonly ITenantContext _tenantContext;

    public GetActiveRulesForTodayQueryHandler(
        IAvailabilityRuleRepository repository,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<AvailabilityRuleDto>> Handle(
        GetActiveRulesForTodayQuery request,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var todayDayOfWeek = today.DayOfWeek;

        // Repository method with additional date filtering
        var rules = await _repository.GetActiveRulesForDateAsync(today, todayDayOfWeek);

        return rules.Select(rule => new AvailabilityRuleDto
        {
            Id = rule.Id,
            TenantId = rule.TenantId,
            ResourceId = rule.ResourceId,
            DayOfWeek = rule.DayOfWeek,
            DayOfWeekText = rule.DayOfWeek.ToString(),
            StartTime = rule.StartTime,
            StartTimeText = rule.StartTime.ToString(@"hh\:mm"),
            EndTime = rule.EndTime,
            EndTimeText = rule.EndTime.ToString(@"hh\:mm"),
            DurationMinutes = (int)(rule.EndTime - rule.StartTime).TotalMinutes,
            IsActive = rule.IsActive,
            EffectiveFrom = rule.EffectiveFrom,
            EffectiveTo = rule.EffectiveTo,
            CreatedAt = rule.CreatedAt,
            UpdatedAt = rule.UpdatedAt
        });
    }
}

// Repository extension method
public async Task<IEnumerable<AvailabilityRule>> GetActiveRulesForDateAsync(
    DateTime date,
    DayOfWeek dayOfWeek)
{
    using var connection = _connectionFactory.CreateConnection();

    var sql = @"
        SELECT * FROM AvailabilityRules
        WHERE TenantId = @TenantId
          AND DayOfWeek = @DayOfWeek
          AND IsActive = true
          AND (EffectiveFrom IS NULL OR EffectiveFrom <= @Date)
          AND (EffectiveTo IS NULL OR EffectiveTo >= @Date)
        ORDER BY StartTime";

    return await connection.QueryAsync<AvailabilityRule>(sql, new
    {
        TenantId = TenantId,
        DayOfWeek = (int)dayOfWeek,
        Date = date
    });
}
```

</details>

---

### Exercise 3: Prevent Overlapping Availability Rules

**Task**: Add validation to prevent creating overlapping availability rules for the same resource and day.

Example:

- Existing rule: Monday 9 AM - 5 PM
- New rule: Monday 3 PM - 7 PM (SHOULD BE REJECTED - overlaps 3-5 PM)

```csharp
// Add this to CreateAvailabilityRuleCommandValidator
RuleFor(x => x)
    .MustAsync(async (command, cancellationToken) =>
    {
        // Check for overlapping rules
        // Your code here
    })
    .WithMessage("Availability rule overlaps with an existing rule for this resource and day");
```

<details>
<summary>Solution</summary>

```csharp
public class CreateAvailabilityRuleCommandValidator : AbstractValidator<CreateAvailabilityRuleCommand>
{
    private readonly IAvailabilityRuleRepository _repository;

    public CreateAvailabilityRuleCommandValidator(IAvailabilityRuleRepository repository)
    {
        _repository = repository;

        // ... other rules ...

        // Check for overlapping rules
        RuleFor(x => x)
            .MustAsync(async (command, cancellationToken) =>
            {
                // Get all rules for this resource and day
                var existingRules = await _repository.GetByResourceIdAndDayAsync(
                    command.ResourceId,
                    command.DayOfWeek,
                    cancellationToken);

                // Check for time overlap
                // Overlap formula: (StartA < EndB) AND (EndA > StartB)
                foreach (var rule in existingRules)
                {
                    // Skip inactive rules
                    if (!rule.IsActive) continue;

                    // Check if time ranges overlap
                    bool overlaps = command.StartTime < rule.EndTime &&
                                  command.EndTime > rule.StartTime;

                    if (overlaps)
                    {
                        // Check if date ranges overlap
                        bool dateRangeOverlaps = true;

                        if (command.EffectiveFrom.HasValue && rule.EffectiveTo.HasValue)
                        {
                            if (command.EffectiveFrom > rule.EffectiveTo)
                                dateRangeOverlaps = false; // New starts after existing ends
                        }

                        if (command.EffectiveTo.HasValue && rule.EffectiveFrom.HasValue)
                        {
                            if (command.EffectiveTo < rule.EffectiveFrom)
                                dateRangeOverlaps = false; // New ends before existing starts
                        }

                        if (dateRangeOverlaps)
                            return false; // Overlap detected!
                    }
                }

                return true; // No overlaps
            })
            .WithMessage("Availability rule overlaps with an existing rule for this resource and day");
    }
}

// Add repository method
public async Task<IEnumerable<AvailabilityRule>> GetByResourceIdAndDayAsync(
    Guid resourceId,
    DayOfWeek dayOfWeek,
    CancellationToken cancellationToken = default)
{
    using var connection = _connectionFactory.CreateConnection();

    var sql = @"
        SELECT * FROM AvailabilityRules
        WHERE TenantId = @TenantId
          AND ResourceId = @ResourceId
          AND DayOfWeek = @DayOfWeek";

    return await connection.QueryAsync<AvailabilityRule>(sql, new
    {
        TenantId = TenantId,
        ResourceId = resourceId,
        DayOfWeek = (int)dayOfWeek
    });
}
```

</details>

---

## 🎯 Key Takeaways

1. **MediatR Pipeline Behaviors** provide cross-cutting concerns (audit logging, validation, caching) without duplicating code in handlers
2. **TIME vs TIMESTAMP** - Use TIME for recurring daily schedules, TIMESTAMP for specific moments
3. **Dynamic WHERE clauses** with DynamicParameters prevent SQL injection while allowing flexible filtering
4. **Composite indexes** dramatically improve query performance when designed for specific access patterns
5. **CHECK constraints** enforce data integrity at the database level, providing defense-in-depth
6. **Clean Architecture** keeps business logic independent of infrastructure concerns

---

[← Back to Index](./README.md) | [← Previous: Day 5](./Day5-Bookings.md)
