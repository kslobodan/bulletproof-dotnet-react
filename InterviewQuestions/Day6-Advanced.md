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

## Q: "Explain your RefreshToken mechanism. Why not just make JWT expiration longer?"

**A:** "RefreshTokens solve the security vs UX tradeoff between short-lived access tokens and long-lived user sessions through token rotation:

**The Core Problem:**

```
❌ Long-lived JWT (7 days):
- If stolen, attacker has 7 days of access
- Cannot revoke (JWTs are stateless)
- No logout capability
- Security risk: compromised tokens = compromised accounts

❌ Short-lived JWT (60 min) + re-login:
- Secure (limited damage window)
- Terrible UX (login every hour)
- Users abandon app
```

**✅ The Solution: RefreshToken Pattern**

```
Access Token (JWT): 60 minutes, stateless
Refresh Token: 7 days, stateful (in database)

Client stores both tokens:
- Access token used for API requests
- Refresh token used to get new access tokens
```

**Token Rotation Flow:**

```csharp
// Initial Login
POST /auth/login { email, password }
→ Returns: { accessToken: "...", refreshToken: "ABC123..." }

// After 60 minutes, access token expires
GET /api/resources → 401 Unauthorized

// Client refreshes tokens
POST /auth/refresh { refreshToken: "ABC123" }
→ Server validates "ABC123" in database:
  ✓ Exists
  ✓ Not revoked
  ✓ Not expired
→ Server generates new access token + new refresh token
→ Server marks "ABC123" as revoked, replaced by "XYZ789"
→ Returns: { accessToken: "...", refreshToken: "XYZ789" }

// Client retries original request
GET /api/resources with new access token → 200 OK

// If someone tries to reuse old refresh token
POST /auth/refresh { refreshToken: "ABC123" }
→ Server checks database: REVOKED
→ Returns: 401 Unauthorized ❌
```

**Database Schema:**

```sql
CREATE TABLE RefreshTokens (
    Id UUID PRIMARY KEY,
    Token VARCHAR(500) NOT NULL UNIQUE,  -- Base64 random token
    UserId UUID NOT NULL,
    TenantId UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    ExpiresAt TIMESTAMP NOT NULL,        -- 7 days from creation
    IsRevoked BOOLEAN DEFAULT FALSE,
    RevokedAt TIMESTAMP NULL,
    ReplacedByToken VARCHAR(500) NULL,   -- Token rotation tracking
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

**Implementation Details:**

```csharp
// 1. Generate cryptographically secure random token
public string GenerateRefreshToken()
{
    var randomBytes = new byte[32];  // 256 bits
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(randomBytes);
    return Convert.ToBase64String(randomBytes);  // "1j54JTLufIsTapexRv9h9JROawjZuuinBH/A6QnvrNU="
}

// 2. Store refresh token on login
var refreshToken = new RefreshToken
{
    Id = Guid.NewGuid(),
    Token = _jwtTokenService.GenerateRefreshToken(),
    UserId = user.Id,
    TenantId = tenant.Id,
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddDays(7),  // Configurable
    IsRevoked = false
};
await _refreshTokenRepository.AddAsync(refreshToken);

// 3. Token rotation on refresh
var oldToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);
if (oldToken == null || oldToken.IsRevoked || oldToken.IsExpired)
    throw new UnauthorizedAccessException();

var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
await _refreshTokenRepository.RevokeAsync(oldToken.Id, newRefreshToken);
```

**Security Benefits:**

| Feature              | JWT Only (7-day) | RefreshToken Pattern |
| -------------------- | ---------------- | -------------------- |
| **Revocation**       | ❌ Impossible    | ✅ Instant           |
| **Logout**           | ❌ Can't enforce | ✅ Revoke token      |
| **Token Theft**      | ❌ 7 days damage | ✅ 60 min damage     |
| **Replay Attacks**   | ❌ Vulnerable    | ✅ One-time use      |
| **Audit Trail**      | ❌ No tracking   | ✅ Full history      |
| **Session Control**  | ❌ None          | ✅ Can limit devices |
| **Suspicious Login** | ❌ Can't detect  | ✅ Can revoke all    |

**Token Rotation Security:**

```
Login → RT1 (active)
Refresh → RT1 revoked, RT2 active
Refresh → RT2 revoked, RT3 active
Refresh → RT3 revoked, RT4 active

Attacker steals RT2 (already revoked):
POST /refresh { refreshToken: RT2 }
→ Server: "Token revoked on 2026-04-05 at 14:35, replaced by RT3"
→ 401 Unauthorized ✅
```

**Real-World Scenarios:**

```csharp
// Scenario 1: Logout
POST /auth/logout
→ Revoke current refresh token
→ User must re-authenticate

// Scenario 2: Security breach detected
var userTokens = await _repository.GetAllByUserIdAsync(compromisedUserId);
foreach (var token in userTokens)
    await _repository.RevokeAsync(token.Id);
// All sessions terminated, user must login again

// Scenario 3: Cleanup
await _repository.DeleteExpiredAsync();
// Deletes revoked/expired tokens older than 30 days
// Prevents database bloat while keeping audit trail
```

**Why This is Production-Grade:**

- **OAuth 2.0 Standard**: Industry-standard pattern used by Google, Facebook, GitHub
- **Defense in Depth**: Multiple layers (HTTPS, short-lived JWT, token rotation, database validation)
- **Compliance Ready**: Session tracking for GDPR, audit requirements
- **User Experience**: Seamless (no repeated logins) but secure"

---

## Q: "How does your Rate Limiting middleware prevent DoS attacks? Walk me through the configuration."

**A:** "I use AspNetCoreRateLimit to implement IP-based rate limiting with per-endpoint rules and whitelisting:

**The Configuration (appsettings.json):**

```json
{
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": false,
    "HttpStatusCode": 429,
    "GeneralRules": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 60
      },
      {
        "Endpoint": "*",
        "Period": "1h",
        "Limit": 1000
      }
    ],
    "EndpointWhitelist": ["get:/swagger/*", "get:/health"],
    "IpRateLimitPolicies": {
      "IpRules": [
        {
          "Ip": "127.0.0.1",
          "Rules": [{ "Endpoint": "*", "Period": "1m", "Limit": 1000 }]
        }
      ]
    }
  }
}
```

**How It Works:**

```
1. Request arrives → Rate Limit Middleware (BEFORE authentication)
2. Extract client IP → "203.0.113.42"
3. Check request count in memory cache
4. IP "203.0.113.42" → Window "14:30:00-14:31:00" → Count: 45
5. Count < Limit (60) → Allow request ✅
6. Increment counter → Save to cache
7. Add response headers:
   X-Rate-Limit-Limit: 60
   X-Rate-Limit-Remaining: 14
   X-Rate-Limit-Reset: 900 (seconds until window resets)
8. Continue to next middleware

If count > limit:
→ Return 429 Too Many Requests ❌
→ Add Retry-After header
→ Request rejected before reaching application logic
```

**Configuration Explained:**

```csharp
// EnableEndpointRateLimiting: true
// Different endpoints can have different limits
POST /auth/login → can have stricter limit (10/min)
GET /resources → can have relaxed limit (100/min)

// StackBlockedRequests: false
// Rejected requests DON'T count toward limit
User makes 100 requests → 60 allowed, 40 rejected
→ Next minute: Fresh 60 requests allowed (not impacted by previous rejections)
// If true, user would be "penalty boxed" for repeated violations

// HttpStatusCode: 429
// Standard HTTP status for rate limiting
// Clients can handle gracefully (retry with backoff)

// GeneralRules
"Period": "1m", "Limit": 60  → 60 requests per minute
"Period": "1h", "Limit": 1000 → 1000 requests per hour
// Both rules enforced simultaneously (sliding windows)

// EndpointWhitelist
"get:/swagger/*" → Documentation unrestricted
"get:/health" → Health checks unrestricted (monitoring tools)

// IpRateLimitPolicies
"Ip": "127.0.0.1", "Limit": 1000
// Localhost gets higher limit for development/testing
```

**Middleware Pipeline Placement:**

```csharp
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();  // 1. Error handling
app.UseIpRateLimiting();                                // 2. Rate limiting ← EARLY!
app.UseMiddleware<TenantResolutionMiddleware>();        // 3. Tenant resolution
app.UseAuthentication();                                // 4. Authentication
app.UseAuthorization();                                 // 5. Authorization
app.MapControllers();                                   // 6. Controllers
```

**Why Rate Limiting is Second:**

```
DoS Attack: 10,000 requests/sec to /auth/login

Without rate limiting:
→ All 10,000 requests hit authentication
→ 10,000 database queries
→ Server overloaded
→ Legitimate users can't access

With rate limiting (2nd in pipeline):
→ Only 60 requests/min pass through
→ 9,940 requests rejected immediately (no DB hit)
→ Server remains responsive
→ Legitimate users unaffected ✅
```

**Attack Scenarios Protected:**

```csharp
// 1. Brute Force Login
POST /auth/login (60 attempts/min)
→ Attacker limited to 60 password guesses per minute
→ Far too slow to crack passwords
→ Can lower to 10/min for login endpoint specifically

// 2. API Scraping
GET /resources (1000 requests/hour)
→ Attacker can't extract entire database
→ Scraping becomes economically unfeasible

// 3. Distributed DoS
100 IPs × 60 requests/min = 6000 req/min total
→ Each IP individually limited
→ Server handles load easily
→ Can implement IP ban for repeat offenders

// 4. Resource Exhaustion
Complex search: GET /bookings?filters=...
→ Limited to 60/min per IP
→ Can't exhaust database connections
→ Can't cause out-of-memory errors
```

**Response Headers (for clients):**

```http
HTTP/1.1 200 OK
X-Rate-Limit-Limit: 60
X-Rate-Limit-Remaining: 45
X-Rate-Limit-Reset: 1775392280

HTTP/1.1 429 Too Many Requests
Retry-After: 15
X-Rate-Limit-Limit: 60
X-Rate-Limit-Remaining: 0
X-Rate-Limit-Reset: 1775392280
Content-Type: application/json

{
  "error": "Rate limit exceeded. Try again in 15 seconds."
}
```

**Production Enhancements:**

```csharp
// 1. Distributed Caching (Redis)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis:6379";
});
// Allows multiple API servers to share rate limit counters

// 2. Per-Endpoint Rules
{
  "Endpoint": "post:/api/*/auth/login",
  "Period": "1m",
  "Limit": 5  // Stricter for sensitive endpoints
}

// 3. Client ID Header (for authenticated users)
"ClientIdHeader": "X-ClientId"
// Rate limit by user ID instead of IP (better for mobile apps behind NAT)

// 4. Whitelist Trusted IPs
"IpWhitelist": ["192.168.1.0/24", "10.0.0.100"]
// Internal network, monitoring tools, CDN origins
```

**Why This Approach:**

- **Zero Code Changes**: Middleware handles everything automatically
- **Flexible**: Easy to adjust limits based on monitoring
- **Standard**: X-Rate-Limit headers follow RFC 6585
- **Production-Ready**: Used by Twitter API, GitHub API, Stripe API
- **Multi-Layered**: Combines with authentication, WAF, CDN for comprehensive protection"

---

## Q: "Explain your soft delete implementation. Why track both IsDeleted and DeletedAt?"

**A:** "Soft delete marks records as deleted without physically removing them, enabling recovery and maintaining data integrity:

**Schema Changes:**

```sql
-- Migration 0009: Add soft delete support
ALTER TABLE Resources ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE Resources ADD COLUMN DeletedAt TIMESTAMP NULL;

ALTER TABLE Bookings ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE Bookings ADD COLUMN DeletedAt TIMESTAMP NULL;

ALTER TABLE AvailabilityRules ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT false;
ALTER TABLE AvailabilityRules ADD COLUMN DeletedAt TIMESTAMP NULL;
```

**Why Both Columns?**

```csharp
// IsDeleted: Fast boolean check
SELECT * FROM Resources WHERE IsDeleted = false;
// Index: CREATE INDEX ix_resources_isdeleted ON Resources(IsDeleted);
// Use case: All active queries (99% of queries)

// DeletedAt: Audit trail and recovery
SELECT * FROM Resources WHERE DeletedAt BETWEEN @Start AND @End;
// Use case: "Show me all resources deleted last week"
// Use case: "Who deleted this resource and when?"

// Combined use:
SELECT * FROM Resources
WHERE IsDeleted = false  // Fast index scan
   OR (IsDeleted = true AND DeletedAt > @CutoffDate);  // Show recently deleted
```

**Updated IRepository Interface:**

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);  // Returns only non-deleted
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize);  // Non-deleted only
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);  // Hard delete (admin only)
    Task SoftDeleteAsync(Guid id);  // Soft delete (normal users) ← NEW
}
```

**BaseRepository Implementation:**

```csharp
public async Task SoftDeleteAsync(Guid id)
{
    var sql = $@"
        UPDATE {TableName}
        SET IsDeleted = true,
            DeletedAt = @DeletedAt,
            UpdatedAt = @UpdatedAt
        WHERE Id = @Id AND TenantId = @TenantId AND IsDeleted = false";

    using var connection = _connectionFactory.CreateConnection();
    var rowsAffected = await connection.ExecuteAsync(sql, new
    {
        Id = id,
        TenantId = TenantId,
        DeletedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    });

    if (rowsAffected == 0)
    {
        throw new NotFoundException($"{typeof(T).Name} not found or already deleted");
    }
}

// Updated GetAll to exclude soft-deleted
protected virtual string BuildGetPagedQuery()
{
    return $@"
        SELECT * FROM {TableName}
        WHERE TenantId = @TenantId
          AND IsDeleted = false  ← Filter soft-deleted
        ORDER BY CreatedAt DESC
        LIMIT @PageSize OFFSET @Offset";
}
```

**Command Handler Update:**

```csharp
// DeleteResourceCommandHandler
public async Task<DeleteResourceResponse> Handle(
    DeleteResourceCommand request,
    CancellationToken cancellationToken)
{
    // Regular users: soft delete
    if (!_currentUserService.IsAdmin)
    {
        await _resourceRepository.SoftDeleteAsync(request.Id);
        return new DeleteResourceResponse
        {
            Message = "Resource deleted successfully (can be recovered)"
        };
    }

    // Admins: hard delete
    await _resourceRepository.DeleteAsync(request.Id);
    return new DeleteResourceResponse
    {
        Message = "Resource permanently deleted"
    };
}
```

**Benefits of Soft Delete:**

```csharp
// 1. Recovery
// User: "I accidentally deleted Meeting Room A!"
var deletedResource = await connection.QuerySingleAsync<Resource>(@"
    SELECT * FROM Resources
    WHERE Id = @Id AND TenantId = @TenantId AND IsDeleted = true
", new { Id, TenantId });

// Restore it
await connection.ExecuteAsync(@"
    UPDATE Resources
    SET IsDeleted = false, DeletedAt = NULL, UpdatedAt = @Now
    WHERE Id = @Id
", new { Id, Now = DateTime.UtcNow });

// 2. Referential Integrity
// Booking references Resource
// If resource is soft-deleted, bookings remain intact
SELECT b.*, r.Name as ResourceName
FROM Bookings b
INNER JOIN Resources r ON b.ResourceId = r.Id  -- Still works!
WHERE b.TenantId = @TenantId;

// 3. Audit Compliance
// "Show me all deleted resources in Q1 2026"
SELECT *
FROM Resources
WHERE IsDeleted = true
  AND DeletedAt BETWEEN '2026-01-01' AND '2026-03-31'
ORDER BY DeletedAt DESC;

// 4. Data Analysis
// "How many resources were deleted each month?"
SELECT
    DATE_TRUNC('month', DeletedAt) as Month,
    COUNT(*) as DeletedCount
FROM Resources
WHERE IsDeleted = true
GROUP BY DATE_TRUNC('month', DeletedAt);
```

**Hard Delete (Permanent):**

```csharp
// Still available for admins - complete data removal
public async Task DeleteAsync(Guid id)
{
    var sql = $@"
        DELETE FROM {TableName}
        WHERE Id = @Id AND TenantId = @TenantId";

    using var connection = _connectionFactory.CreateConnection();
    var rowsAffected = await connection.ExecuteAsync(sql, new
    {
        Id = id,
        TenantId = TenantId
    });

    if (rowsAffected == 0)
    {
        throw new NotFoundException($"{typeof(T).Name} not found");
    }
}
```

**Database Cleanup:**

```csharp
// Periodic cleanup job (runs monthly)
public async Task PurgeOldSoftDeletedAsync(int daysOld = 90)
{
    var sql = @"
        DELETE FROM Resources
        WHERE IsDeleted = true
          AND DeletedAt < @CutoffDate";

    var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
    var deletedCount = await connection.ExecuteAsync(sql, new { CutoffDate = cutoffDate });

    _logger.LogInformation($"Purged {deletedCount} soft-deleted resources older than {daysOld} days");
}
```

**Trade-offs:**

| Aspect          | Soft Delete          | Hard Delete          |
| --------------- | -------------------- | -------------------- |
| **Recovery**    | ✅ Easy              | ❌ Impossible        |
| **Disk Space**  | ❌ Grows over time   | ✅ Freed immediately |
| **Query Speed** | ❌ Slightly slower\* | ✅ Faster            |
| **Compliance**  | ✅ Audit trail       | ❌ No record         |
| **Complexity**  | ❌ More code         | ✅ Simple            |

\*With proper index on IsDeleted, performance impact is negligible"

---

## Q: "How did you implement the booking statistics endpoint? What SQL aggregation techniques did you use?"

**A:** "The statistics endpoint uses PostgreSQL aggregate functions and GROUP BY to calculate metrics efficiently at the database level:

**The DTO:**

```csharp
public class BookingStatisticsDto
{
    public int TotalBookings { get; set; }
    public int PendingCount { get; set; }
    public int ConfirmedCount { get; set; }
    public int CancelledCount { get; set; }
    public int CompletedCount { get; set; }
    public Dictionary<string, int> BookingsByResource { get; set; } = new();
    public Dictionary<string, int> BookingsByMonth { get; set; } = new();
}
```

**The SQL Query:**

```csharp
public async Task<BookingStatisticsDto> GetStatisticsAsync(
    DateTime? startDate = null,
    DateTime? endDate = null)
{
    using var connection = _connectionFactory.CreateConnection();

    var parameters = new DynamicParameters();
    parameters.Add("TenantId", TenantId);

    var whereClause = "WHERE TenantId = @TenantId AND IsDeleted = false";

    if (startDate.HasValue)
    {
        whereClause += " AND StartTime >= @StartDate";
        parameters.Add("StartDate", startDate.Value);
    }

    if (endDate.HasValue)
    {
        whereClause += " AND EndTime <= @EndDate";
        parameters.Add("EndDate", endDate.Value);
    }

    // 1. Count by status (single query, multiple aggregates)
    var statusSql = $@"
        SELECT
            COUNT(*)::int AS TotalBookings,
            COUNT(*) FILTER (WHERE Status = 'Pending')::int AS PendingCount,
            COUNT(*) FILTER (WHERE Status = 'Confirmed')::int AS ConfirmedCount,
            COUNT(*) FILTER (WHERE Status = 'Cancelled')::int AS CancelledCount,
            COUNT(*) FILTER (WHERE Status = 'Completed')::int AS CompletedCount
        FROM Bookings
        {whereClause}";

    var statusResult = await connection.QuerySingleAsync<StatusCountsDto>(
        statusSql,
        parameters);

    // 2. Bookings by resource (GROUP BY)
    var resourceSql = $@"
        SELECT
            r.Name AS ResourceName,
            COUNT(b.Id)::int AS BookingCount
        FROM Bookings b
        INNER JOIN Resources r ON b.ResourceId = r.Id
        {whereClause}
        GROUP BY r.Name
        ORDER BY BookingCount DESC
        LIMIT 10";

    var resourceStats = await connection.QueryAsync<ResourceStatDto>(
        resourceSql,
        parameters);

    // 3. Bookings by month (DATE_TRUNC for time grouping)
    var monthSql = $@"
        SELECT
            TO_CHAR(DATE_TRUNC('month', StartTime), 'YYYY-MM') AS Month,
            COUNT(*)::int AS BookingCount
        FROM Bookings
        {whereClause}
        GROUP BY DATE_TRUNC('month', StartTime)
        ORDER BY Month DESC";

    var monthStats = await connection.QueryAsync<MonthStatDto>(
        monthSql,
        parameters);

    return new BookingStatisticsDto
    {
        TotalBookings = statusResult.TotalBookings,
        PendingCount = statusResult.PendingCount,
        ConfirmedCount = statusResult.ConfirmedCount,
        CancelledCount = statusResult.CancelledCount,
        CompletedCount = statusResult.CompletedCount,
        BookingsByResource = resourceStats.ToDictionary(x => x.ResourceName, x => x.BookingCount),
        BookingsByMonth = monthStats.ToDictionary(x => x.Month, x => x.BookingCount)
    };
}
```

**SQL Techniques Explained:**

```sql
-- 1. FILTER clause (aggregate with condition)
COUNT(*) FILTER (WHERE Status = 'Pending')
-- Equivalent to:
SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END)
-- But more readable and performant

-- 2. GROUP BY for aggregation
SELECT
    r.Name,
    COUNT(b.Id) AS BookingCount
FROM Bookings b
INNER JOIN Resources r ON b.ResourceId = r.Id
GROUP BY r.Name;

Result:
| Name              | BookingCount |
|-------------------|--------------|
| Meeting Room A    | 45           |
| Conference Hall B | 32           |
| Lab Room 101      | 18           |

-- 3. DATE_TRUNC for time-based grouping
DATE_TRUNC('month', StartTime)  -- Truncates to first day of month
'2026-04-15 10:30' → '2026-04-01 00:00'
'2026-03-22 14:15' → '2026-03-01 00:00'

SELECT
    DATE_TRUNC('month', StartTime) AS Month,
    COUNT(*) AS BookingCount
FROM Bookings
GROUP BY DATE_TRUNC('month', StartTime);

Result:
| Month               | BookingCount |
|---------------------|--------------|
| 2026-04-01 00:00:00 | 127          |
| 2026-03-01 00:00:00 | 98           |
| 2026-02-01 00:00:00 | 85           |

-- 4. TO_CHAR for formatting
TO_CHAR(DATE_TRUNC('month', StartTime), 'YYYY-MM')
'2026-04-01 00:00:00' → '2026-04'

-- 5. ::int type casting
COUNT(*)::int
-- Postgres returns COUNT as bigint (64-bit)
-- Cast to int (32-bit) for C# compatibility
```

**Why Database-Level Aggregation?**

```csharp
// ❌ BAD: Aggregate in application code
var allBookings = await _repository.GetAllAsync();  // Fetch 100,000 bookings
var pendingCount = allBookings.Count(b => b.Status == BookingStatus.Pending);
var confirmedCount = allBookings.Count(b => b.Status == BookingStatus.Confirmed);
// Problems:
// - Transfers 100,000 rows over network
// - Uses memory for all 100,000 objects
// - Slow LINQ aggregation in .NET

// ✅ GOOD: Aggregate in database
var sql = "SELECT COUNT(*) FILTER (WHERE Status = 'Pending') FROM Bookings";
var pendingCount = await connection.ExecuteScalarAsync<int>(sql);
// Benefits:
// - Only 1 integer transferred over network
// - Zero .NET memory allocation
// - Database index-optimized aggregation
```

**Performance Comparison:**

```
Dataset: 100,000 bookings

Application Aggregation:
- Network transfer: 50 MB
- Memory: 500 MB
- Time: 2.5 seconds

Database Aggregation:
- Network transfer: 0.5 KB
- Memory: 1 MB
- Time: 15 milliseconds

167x faster! ✅
```

**Query Endpoint:**

```csharp
[HttpGet("statistics")]
[Authorize(Policy = "ManagerOrAbove")]
public async Task<IActionResult> GetStatistics(
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null)
{
    var query = new GetBookingStatisticsQuery
    {
        StartDate = startDate,
        EndTime = endDate
    };

    var result = await _mediator.Send(query);
    return Ok(result);
}
```

**Example Request/Response:**

```http
GET /api/v1/bookings/statistics?startDate=2026-01-01&endDate=2026-12-31

Response:
{
  "totalBookings": 345,
  "pendingCount": 42,
  "confirmedCount": 187,
  "cancelledCount": 38,
  "completedCount": 78,
  "bookingsByResource": {
    "Meeting Room A": 95,
    "Conference Hall B": 72,
    "Lab Room 101": 58,
    "Training Room": 45,
    "Board Room": 75
  },
  "bookingsByMonth": {
    "2026-12": 32,
    "2026-11": 28,
    "2026-10": 35,
    "2026-09": 29,
    "2026-08": 31
  }
}
```

**Why This Design:**

- **Efficient**: Aggregates 100k+ records in milliseconds
- **Scalable**: Performance doesn't degrade with data growth (uses indexes)
- **Flexible**: Easy to add new metrics (AVG duration, peak hours, etc.)
- **Standard SQL**: Works with any SQL database (not PostgreSQL-specific except FILTER)"

---

## 🎯 Key Takeaways

1. **MediatR Pipeline Behaviors** provide cross-cutting concerns (audit logging, validation, caching) without duplicating code in handlers
2. **TIME vs TIMESTAMP** - Use TIME for recurring daily schedules, TIMESTAMP for specific moments
3. **Dynamic WHERE clauses** with DynamicParameters prevent SQL injection while allowing flexible filtering
4. **Composite indexes** dramatically improve query performance when designed for specific access patterns
5. **CHECK constraints** enforce data integrity at the database level, providing defense-in-depth
6. **Clean Architecture** keeps business logic independent of infrastructure concerns
7. **RefreshToken pattern** combines security and UX through token rotation and stateful validation
8. **Rate limiting** protects APIs from abuse and DoS attacks with minimal performance overhead
9. **Soft delete** enables data recovery and audit trails while maintaining referential integrity
10. **Database-level aggregation** is orders of magnitude faster than application-level aggregation

---

[← Back to Index](./README.md) | [← Previous: Day 5](./Day5-Bookings.md)
