# Day 5: Bookings CRUD & Business Logic

[← Back to Index](./README.md) | [← Previous: Day 4](./Day4-Resources.md)

---

## Q: "Explain how your HasConflictAsync method prevents double-booking. Walk me through the SQL logic."

**A:** "The `HasConflictAsync` method uses a time overlap detection algorithm to prevent two bookings from occupying the same resource at the same time:

**The SQL Query:**

```sql
SELECT EXISTS (
    SELECT 1 FROM Bookings
    WHERE TenantId = @TenantId
      AND ResourceId = @ResourceId
      AND Status IN (0, 1)  -- Only Pending and Confirmed count as conflicts
      AND Id != COALESCE(@ExcludeBookingId, '00000000-0000-0000-0000-000000000000'::uuid)
      AND StartTime < @EndTime    -- New booking starts before existing ends
      AND EndTime > @StartTime    -- New booking ends after existing starts
)
```

**Time Overlap Logic Explained:**

Two time ranges overlap if they share ANY moment in time. The mathematical formula is:

```
(StartA < EndB) AND (EndA > StartB)
```

**Visual Examples:**

```
Existing: [====10:00----11:00====]
Proposed: [==10:30----11:30==]
Check: 10:30 < 11:00 ✓ AND 11:30 > 10:00 ✓ → CONFLICT

Existing: [====10:00----11:00====]
Proposed:                        [====11:00----12:00====]
Check: 11:00 < 11:00 ✗ → NO CONFLICT (back-to-back allowed)

Existing:           [====10:00----11:00====]
Proposed: [==09:00----09:30==]
Check: 09:00 < 11:00 ✓ BUT 09:30 > 10:00 ✗ → NO CONFLICT
```

**Why only Status IN (0, 1)?**

- **Pending (0)** and **Confirmed (1)** block the time slot
- **Completed (2)**, **Cancelled (3)**, **Rejected (4)** free up the slot for rebooking
- A cancelled booking at 10:00-11:00 doesn't prevent a new booking at the same time

**Why exclude by ID?**
When updating an existing booking's time, we don't want it to conflict with itself:

```csharp
// Update booking from 10:00-11:00 to 10:30-11:30
HasConflictAsync(resourceId, newStart: 10:30, newEnd: 11:30, excludeBookingId: booking.Id)
// Without exclude: would detect conflict with itself (10:00-11:00 overlaps 10:30-11:30)
```

**Database Optimization:**
Composite index on `(TenantId, ResourceId, Status, StartTime, EndTime)` makes this query execute in milliseconds even with thousands of bookings."

---

## Q: "Why use ICurrentUserService instead of just passing userId from the controller? What problem does it solve?"

**A:** "The `ICurrentUserService` centralizes user identity extraction from JWT claims, solving several architectural problems:

**The Problem Without It:**

```csharp
// BAD: Extract userId in every controller action
[HttpPost]
public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (userIdClaim == null) return Unauthorized();

    var command = new CreateBookingCommand
    {
        UserId = Guid.Parse(userIdClaim),  // Duplicated in every action
        ResourceId = request.ResourceId,
        ...
    };

    var result = await _mediator.Send(command);
    return Ok(result);
}
```

**Problems with this approach:**

1. **Code Duplication** - Every controller action repeats the same claims extraction
2. **Testing Difficulty** - Hard to mock User.FindFirst() in unit tests
3. **Tight Coupling** - Controllers directly depend on ASP.NET Core's ClaimsPrincipal
4. **Inconsistent Error Handling** - Each action handles missing claims differently
5. **Violates CQRS** - Commands shouldn't carry authentication data; it's a cross-cutting concern

**The Solution with ICurrentUserService:**

```csharp
// Application Layer (clean, no ASP.NET dependencies)
public interface ICurrentUserService
{
    Guid UserId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
}

// Infrastructure Layer (knows about HTTP context)
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Guid UserId =>
        Guid.Parse(_httpContextAccessor.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());
}

// Command Handler (clean, testable)
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, ...>
{
    private readonly ICurrentUserService _currentUserService;

    public async Task<CreateBookingResponse> Handle(...)
    {
        var booking = new Booking
        {
            UserId = _currentUserService.UserId,  // Simple, clean
            CreatedBy = _currentUserService.UserId,
            ...
        };
    }
}
```

**Benefits:**

1. **Separation of Concerns** - Application layer doesn't know about HTTP or claims
2. **Testability** - Mock `ICurrentUserService` easily in unit tests:
   ```csharp
   var mockUserService = new Mock<ICurrentUserService>();
   mockUserService.Setup(x => x.UserId).Returns(testUserId);
   ```
3. **Consistent Behavior** - All handlers get userId the same way
4. **Future-Proof** - If we switch from JWT to OAuth2, only `CurrentUserService` implementation changes
5. **Clean Architecture** - No infrastructure concerns leak into Application layer

**Why not just add UserId to every Command?**

- Commands should represent user intent (`CreateBookingCommand { ResourceId, StartTime, EndTime }`)
- Authentication context is implicit—the system knows who you are
- Removes temptation to fake userId in requests (security risk)
- Cleaner API contracts (fewer required fields)"

---

## Q: "Walk me through the booking status workflow. Why is UpdateBooking restricted to Pending bookings only?"

**A:** "The booking system implements a 5-state workflow to model real-world business processes:

**Status Enum:**

```csharp
public enum BookingStatus
{
    Pending = 0,      // User created, awaiting approval
    Confirmed = 1,    // Admin/Manager approved
    Completed = 2,    // Booking time passed, service delivered
    Cancelled = 3,    // User or admin cancelled
    Rejected = 4      // Admin rejected the request
}
```

**State Transition Rules:**

```
        ┌─────────┐
        │ Pending │ (User creates booking)
        └────┬────┘
             │
     ┌───────┴────────┐
     │                │
    ✓ Confirm      ✗ Cancel/Reject
     │                │
     ▼                ▼
┌──────────┐    ┌──────────┐
│Confirmed │    │Cancelled │
└────┬─────┘    │ Rejected │
     │          └──────────┘
     │ (time passes)
     ▼
┌──────────┐
│Completed │
└──────────┘
```

**Why restrict updates to Pending only?**

```csharp
public async Task<UpdateBookingResponse> Handle(UpdateBookingCommand request, ...)
{
    var booking = await _bookingRepository.GetByIdAsync(request.Id);

    if (booking.Status != BookingStatus.Pending)
        throw new InvalidOperationException(
            $"Cannot update booking with status {booking.Status}");

    // Only Pending bookings reach here...
}
```

**Business Justification:**

1. **Confirmed Bookings** - Already approved by management
   - Changing time/resource would require re-approval
   - Resources may have been allocated (equipment, staff)
   - Other bookings may depend on this time slot being blocked
   - **Solution:** Cancel confirmed booking, create new one (audit trail)

2. **Completed Bookings** - Historical records
   - Service already delivered
   - May be linked to invoices, reports, statistics
   - Modifying would corrupt historical data
   - **Solution:** Immutable after completion

3. **Cancelled/Rejected Bookings** - Inactive
   - No point updating—they're not happening
   - Better UX: create new booking instead of reviving old one

**Alternative Workflow (if business required it):**

```csharp
// If you needed to modify Confirmed bookings:
if (booking.Status == BookingStatus.Confirmed)
{
    // Revert to Pending, requiring re-approval
    booking.Status = BookingStatus.Pending;

    // Create audit log entry
    await _auditLog.LogAsync(new AuditEntry
    {
        Action = "BookingModified",
        OldValues = originalBooking,
        NewValues = modifiedBooking,
        RequiresReApproval = true
    });
}
```

**Interview Follow-up:**
'What if a manager needs to change a confirmed booking?' → Implement a separate `ModifyConfirmedBooking` command with `[Authorize(Policy = "ManagerOrAbove")]` that logs the change, notifies affected parties, and requires justification."

---

## Q: "How does AutoMapper's BookingDto computed properties (DurationMinutes, IsUpcoming, IsPast) work? Why compute at mapping time?"

**A:** "AutoMapper's mapping profile computes derived properties during object transformation:

**Mapping Configuration:**

```csharp
public class BookingMappingProfile : Profile
{
    public BookingMappingProfile()
    {
        CreateMap<Booking, BookingDto>()
            .ForMember(dest => dest.StatusText,
                opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.DurationMinutes,
                opt => opt.MapFrom(src => (int)(src.EndTime - src.StartTime).TotalMinutes))
            .ForMember(dest => dest.IsUpcoming,
                opt => opt.MapFrom(src => src.StartTime > DateTime.UtcNow))
            .ForMember(dest => dest.IsPast,
                opt => opt.MapFrom(src => src.EndTime < DateTime.UtcNow));
    }
}
```

**Usage in Handler:**

```csharp
public async Task<PagedResult<BookingDto>> Handle(GetAllBookingsQuery request, ...)
{
    var bookings = await _bookingRepository.GetPagedAsync(...);

    // AutoMapper transforms List<Booking> → List<BookingDto>
    var bookingDtos = _mapper.Map<List<BookingDto>>(bookings.Items);

    return new PagedResult<BookingDto>(bookingDtos, bookings.TotalCount, ...);
}
```

**Why Compute at Mapping Time vs Storing in Database?**

**Option 1: Compute in Mapping (Current Approach)**

**Pros:**

- **Always Accurate** - `IsUpcoming` is true/false based on current time
- **No Stale Data** - A booking created yesterday at 2pm for today at 3pm was "upcoming" then, is "past" now
- **No Storage Cost** - Derived from existing `StartTime`/`EndTime`
- **Single Source of Truth** - Only store `StartTime` and `EndTime`, derive everything else

**Cons:**

- **CPU Cost** - Calculates on every read
- **Can't Filter in SQL** - Can't do `WHERE DurationMinutes > 60` efficiently

**Option 2: Store Computed Columns in Database**

**Pros:**

- **Query Performance** - Can filter `WHERE DurationMinutes BETWEEN 30 AND 120`
- **Indexed** - Can create index on computed columns

**Cons:**

- **Stale Data** - `IsUpcoming` stored as `true` yesterday is wrong today
- **Storage Overhead** - Redundant data (duration derivable from start/end)
- **Update Complexity** - Must recalculate on every booking update

**Option 3: PostgreSQL Generated Columns**

```sql
CREATE TABLE Bookings (
    StartTime TIMESTAMP NOT NULL,
    EndTime TIMESTAMP NOT NULL,
    DurationSeconds INT GENERATED ALWAYS AS
        (EXTRACT(EPOCH FROM (EndTime - StartTime))) STORED
);
```

**Pros:**

- Automatically updated by database
- Can index and query efficiently

**Cons:**

- Database-specific syntax (not portable)
- Still can't solve time-dependent fields like `IsUpcoming` (requires trigger/cron)

**Best Practice Decision:**

- **Store:** Immutable data (`StartTime`, `EndTime`, `Status`)
- **Compute in Mapping:** Derived data (`DurationMinutes`, `StatusText`)
- **Compute at Query Time:** Time-dependent booleans (`IsUpcoming`, `IsPast`)

**Performance Note:**
Computing 4 derived properties for 1000 bookings takes ~2ms. Database round-trip takes ~50ms. The computation overhead is negligible compared to I/O. Only optimize if profiling shows it's a bottleneck."

---

## Q: "Explain the ManagerOrAbove authorization policy. How does it differ from role checks in the handler?"

**A:** "The `ManagerOrAbove` policy uses ASP.NET Core's declarative authorization at the controller level, separate from business logic:

**Policy Definition (Program.cs):**

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ManagerOrAbove", policy =>
        policy.RequireRole("TenantAdmin", "Manager"));

    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("TenantAdmin"));
});
```

**Usage in Controller:**

```csharp
[ApiController]
[Authorize]  // All endpoints require authentication
public class BookingsController : ControllerBase
{
    [HttpPost("{id}/confirm")]
    [Authorize(Policy = "ManagerOrAbove")]  // Additional role check
    public async Task<IActionResult> ConfirmBooking(Guid id)
    {
        var command = new ConfirmBookingCommand { Id = id };
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]  // Stricter: only TenantAdmin
    public async Task<IActionResult> DeleteBooking(Guid id)
    {
        var command = new DeleteBookingCommand { Id = id };
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
```

**Request Pipeline Execution:**

```
HTTP POST /api/v1/bookings/123/confirm
↓
1. Authentication Middleware
   → Validates JWT, sets User.Claims
   ↓
2. Authorization Middleware
   → Checks [Authorize(Policy = "ManagerOrAbove")]
   → Reads User roles from JWT claims
   → Evaluates: Is role "TenantAdmin" OR "Manager"?
   → ✗ If NO: Return 403 Forbidden (stops here)
   → ✓ If YES: Continue
   ↓
3. Controller Action
   → await _mediator.Send(command)
   ↓
4. ConfirmBookingCommandHandler
   → Business logic (change Status to Confirmed)
   → No role checks—authorization already done
```

**Why NOT Check Roles in Handler?**

**BAD: Authorization in Business Logic**

```csharp
public class ConfirmBookingCommandHandler : IRequestHandler<...>
{
    private readonly ICurrentUserService _userService;

    public async Task<ConfirmBookingResponse> Handle(...)
    {
        // Mixing authorization with business logic (wrong layer)
        var userRole = _userService.Roles;
        if (!userRole.Contains("Manager") && !userRole.Contains("TenantAdmin"))
            throw new UnauthorizedException("Insufficient permissions");

        // Business logic buried below security checks
        var booking = await _repository.GetByIdAsync(request.Id);
        booking.Status = BookingStatus.Confirmed;
        ...
    }
}
```

**GOOD: Declarative Authorization**

```csharp
// Controller: Declare WHO can access
[Authorize(Policy = "ManagerOrAbove")]

// Handler: Focus on WHAT happens
public class ConfirmBookingCommandHandler : IRequestHandler<...>
{
    public async Task<ConfirmBookingResponse> Handle(...)
    {
        // No authorization code—clean business logic
        var booking = await _repository.GetByIdAsync(request.Id);

        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException("Only Pending bookings can be confirmed");

        booking.Status = BookingStatus.Confirmed;
        await _repository.UpdateAsync(booking);
        return new ConfirmBookingResponse { ... };
    }
}
```

**Benefits:**

1. **Separation of Concerns**
   - Authorization = Cross-cutting concern (WHO can do it)
   - Business logic = Domain concern (WHAT needs to happen)

2. **Centralized Security**
   - All policies defined in one place (`Program.cs`)
   - Easy to audit: "What can Managers do?" → Search for `"ManagerOrAbove"`

3. **Automatic 403 Forbidden**
   - ASP.NET Core returns proper HTTP status code
   - No need to write `return Forbid()` everywhere

4. **Testability**
   - Test authorization separately (integration tests with different roles)
   - Test business logic without mocking authorization

5. **Performance**
   - Authorization checked once at entry point
   - Handler doesn't waste cycles on already-verified checks

**When WOULD You Check Permissions in Handler?**

For **resource-based authorization**:

```csharp
public async Task<UpdateBookingResponse> Handle(UpdateBookingCommand request, ...)
{
    var booking = await _repository.GetByIdAsync(request.Id);

    // Resource-specific check: Can THIS user modify THIS specific booking?
    if (booking.UserId != _currentUserService.UserId &&
        !_currentUserService.IsAdmin)
    {
        throw new UnauthorizedException(
            "You can only modify your own bookings");
    }

    // Business logic...
}
```

This is different—it's checking ownership of a specific resource, not general role capability."

---

## Q: "What happens when two users try to book the same time slot simultaneously? How does your system handle race conditions?"

**A:** "Great question! This is a classic database concurrency problem. Here's how the system handles it:

**The Race Condition Scenario:**

```
Time: 00:00.000
User A: Calls CreateBooking(resourceId=123, start=10:00, end=11:00)
User B: Calls CreateBooking(resourceId=123, start=10:00, end=11:00)

Time: 00:00.001
User A: HasConflictAsync checks → No conflicts found ✓
User B: HasConflictAsync checks → No conflicts found ✓ (User A hasn't inserted yet!)

Time: 00:00.002
User A: INSERT booking → Success
User B: INSERT booking → Success (BOTH got through!)

Result: DOUBLE-BOOKED! 💥
```

**Current Protection (Good Enough for MVP):**

The window for race conditions is extremely small (~1-2ms between conflict check and insert). For a typical booking app with human users:

- Probability: Very low (users rarely click exactly simultaneously)
- Impact: Moderate (conflict resolved by admin if it happens)
- Cost: Zero additional complexity

**Production-Grade Solutions:**

**Option 1: Database Transaction with Serializable Isolation**

```csharp
public async Task<Booking> CreateBookingAsync(CreateBookingCommand request)
{
    using var transaction = connection.BeginTransaction(
        IsolationLevel.Serializable);  // Highest isolation level

    try
    {
        // Check for conflict
        var hasConflict = await HasConflictAsync(...);
        if (hasConflict)
            throw new InvalidOperationException("Slot already booked");

        // Insert booking
        var booking = new Booking { ... };
        await AddAsync(booking);

        transaction.Commit();  // Atomic: check + insert
        return booking;
    }
    catch (SerializationException)
    {
        // Another transaction modified data we read
        transaction.Rollback();
        throw new InvalidOperationException("Booking conflict occurred");
    }
}
```

**Pros:** Guaranteed consistency
**Cons:** Performance penalty (serializable isolation locks rows), potential deadlocks

**Option 2: Optimistic Concurrency with RowVersion**

```csharp
public class Booking
{
    public Guid Id { get; set; }
    [Timestamp]  // EF Core attribute
    public byte[] RowVersion { get; set; }  // Auto-updated on every change
    ...
}

// Update command
UPDATE Bookings
SET Status = @Status, RowVersion = @NewRowVersion
WHERE Id = @Id AND RowVersion = @OldRowVersion;

// If RowVersion changed between read and update:
// → 0 rows updated → throw DbConcurrencyException
```

**Pros:** No locks, high performance
**Cons:** Doesn't prevent initial insert race condition (only helps updates)

**Option 3: Unique Constraint (Best for This Scenario)**

```sql
-- PostgreSQL exclusion constraint using range types
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE Bookings
ADD CONSTRAINT prevent_overlapping_bookings
EXCLUDE USING gist (
    TenantId WITH =,
    ResourceId WITH =,
    tsrange(StartTime, EndTime) WITH &&  -- Range overlap check
)
WHERE (Status IN (0, 1));  -- Only for Pending/Confirmed
```

**How it works:**

- Database enforces no overlapping time ranges at commit time
- Even if two transactions pass `HasConflictAsync`, one will fail on INSERT
- Second transaction gets: `ERROR: conflicting key value violates exclusion constraint`
- Application catches this and returns friendly error

**Implementation:**

```csharp
try
{
    await _repository.AddAsync(booking);
}
catch (PostgresException ex) when (ex.SqlState == "23P01")  // Exclusion violation
{
    throw new InvalidOperationException(
        "This time slot was just booked by another user. Please try a different time.");
}
```

**Pros:**

- Database guarantees no double-booking (impossible to bypass)
- Application layer can be simpler
- No performance impact (constraint checked during commit)

**Cons:**

- Database-specific (PostgreSQL's `gist` index, SQL Server has different syntax)
- Harder to modify logic (e.g., "allow overlaps for VIP users")

**What I'd Do in Production:**

1. **Phase 1 (MVP):** Current implementation—race condition unlikely
2. **Phase 2:** Add database constraint when scaling (defense in depth)
3. **Phase 3:** Optimistic locking if audit requirements demand it

**Interview Answer:**
'The current implementation has a theoretical race condition, but in practice it's negligible for human users. If this were a high-stakes system (airline seats, concert tickets), I'd add a PostgreSQL exclusion constraint as the ultimate safety net. The database is the single source of truth—let it enforce the rule.'"

---

## Q: "Why create separate DTOs (CreateBookingRequest vs Booking entity) instead of using the entity directly in the API?"

**A:** "Separating DTOs from entities follows several software engineering principles:

**The Problem with Using Entities Directly:**

```csharp
// BAD: Exposing domain entity to API
[HttpPost]
public async Task<IActionResult> CreateBooking([FromBody] Booking booking)
{
    // Client sends:
    {
        "id": "00000000-0000-0000-0000-000000000000",
        "tenantId": "attacker-tenant-id",  // SECURITY RISK!
        "userId": "admin-user-id",         // PRIVILEGE ESCALATION!
        "status": 1,                        // Client sets to Confirmed!
        "createdAt": "1900-01-01",         // Invalid data
        "updatedAt": null,
        "createdBy": "attacker-id"
    }

    // Server has no control—accepts whatever client sends
    await _repository.AddAsync(booking);
}
```

**Problems:**

1. **Security**: Client controls sensitive fields (`TenantId`, `UserId`, `Status`)
2. **Over-posting**: Client can modify readonly fields (`CreatedAt`, `Id`)
3. **Breaking Changes**: Adding a property to `Booking` exposes it in API (unintentional contract change)
4. **Validation**: Mix of business rules and API constraints
5. **Database Leakage**: Audit fields (`CreatedBy`, `UpdatedAt`) exposed to clients

**The Solution with DTOs:**

**CreateBookingRequest (API Contract):**

```csharp
public class CreateBookingRequest
{
    [Required]
    public Guid ResourceId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // NO Id, TenantId, UserId, Status, audit fields!
}
```

**Booking (Domain Entity):**

```csharp
public class Booking
{
    public Guid Id { get; set; }              // System-generated
    public Guid TenantId { get; set; }        // From TenantContext
    public Guid UserId { get; set; }          // From ICurrentUserService
    public Guid ResourceId { get; set; }      // From request
    public DateTime StartTime { get; set; }   // From request
    public DateTime EndTime { get; set; }     // From request
    public BookingStatus Status { get; set; } // System-controlled
    public string? Title { get; set; }        // From request
    public string? Description { get; set; }  // From request
    public string? Notes { get; set; }        // Internal use only
    public DateTime CreatedAt { get; set; }   // Auto-set by repository
    public DateTime? UpdatedAt { get; set; }  // Auto-set by repository
    public Guid CreatedBy { get; set; }       // From ICurrentUserService
    public Guid? UpdatedBy { get; set; }      // From ICurrentUserService
}
```

**Handler Orchestrates Mapping:**

```csharp
public async Task<CreateBookingResponse> Handle(CreateBookingCommand request, ...)
{
    var booking = new Booking
    {
        // System-controlled fields
        Id = Guid.NewGuid(),
        TenantId = _tenantContext.TenantId,
        UserId = _currentUserService.UserId,
        Status = BookingStatus.Pending,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = _currentUserService.UserId,

        // User-provided fields (validated by FluentValidation)
        ResourceId = request.ResourceId,
        StartTime = request.StartTime,
        EndTime = request.EndTime,
        Title = request.Title,
        Description = request.Description
    };

    await _repository.AddAsync(booking);
    return new CreateBookingResponse { ... };
}
```

**Benefits:**

1. **API Stability**: Change `Booking` entity without breaking API contract
   - Add internal fields → clients unaffected
   - Rename properties → DTO stays the same (map differently)

2. **Security**: Server controls sensitive fields
   - `TenantId` from middleware, not client
   - `UserId` from JWT, not client
   - `Status` set by business rules, not client

3. **Validation Clarity**:
   - Request validation: Data format, required fields
   - Business validation: Duplicate check, permissions, business rules

4. **Documentation**: Swagger shows only relevant fields

   ```json
   // CreateBookingRequest (Swagger)
   {
     "resourceId": "uuid",
     "startTime": "2026-04-05T10:00:00",
     "endTime": "2026-04-05T11:00:00",
     "title": "Meeting"
   }

   // vs exposing full Booking with 14 confusing properties
   ```

5. **Versioning**: Can create `CreateBookingRequestV2` without touching entity

**DTO Layer Breakdown:**

```
CreateBookingRequest  → Input  (what client sends)
CreateBookingCommand  → Intent (what user wants to do)
Booking              → Domain (business entity)
BookingDto           → Output (what client receives)
```

**When You Might Skip DTOs:**

- **Internal microservices** communicating via gRPC (trusted environment)
- **Admin tools** where security is less critical
- **Prototypes/MVPs** where speed matters more than robustness

**Production Rule:** Always use DTOs for public APIs. The slight mapping overhead (microseconds) is worth the security and flexibility."

---

## Key Takeaways

**Core Concepts:**

1. **Conflict Detection** - Time overlap formula: `(StartA < EndB) AND (EndA > StartB)`
2. **Clean Architecture** - DTOs separate API contracts from domain entities
3. **Security Layers** - Authorization at controller, validation in pipeline, business logic in handler
4. **Service Abstraction** - `ICurrentUserService` isolates infrastructure from application logic
5. **Status Workflows** - Restrict operations based on current state (Pending → Confirmed → Completed)

**Interview-Ready Explanations:**

- Walk through SQL conflict detection with visual examples
- Explain why AutoMapper computes properties vs storing them
- Defend design decisions with trade-offs (MVP vs production-grade solutions)
- Demonstrate understanding of race conditions and database constraints

**Practice These:**

1. Draw the booking status state machine on a whiteboard
2. Explain `ICurrentUserService` benefits without looking at notes
3. Code review: identify the security risks in direct entity binding
4. Implement a simple version of `HasConflictAsync` from memory

[← Back to Index](./README.md)
