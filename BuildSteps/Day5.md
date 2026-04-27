# Day 5: Bookings CRUD & Business Logic

---

## Booking Entity & Enum (Domain Layer)

1. In `Domain/Entities/Booking.cs` creted:
   - `BookingStatus`
   - `Booking`

## Booking DTOs (Application Layer)

2. In `Application/Features/Bookings/DTOs/BookingDto.cs` created:
   - `BookingDto`
   - `CreateBookingRequest`
   - `CreateBookingResponse`
   - `UpdateBookingRequest`
   - `UpdateBookingResponse`
   - `CancelBookingResponse`
   - `ConfirmBookingResponse`
   - `DeleteBookingResponse`

## Booking Commands (Application Layer)

3. In `Application/Features/Bookings/Commands/` created:
   - `CreateBooking`
   - `UpdateBooking`
   - `CancelBooking`
   - `ConfirmBooking`
   - `DeleteBooking`

## Booking Queries (Application Layer)

4. In `Application/Features/Bookings/Queries/` created:
   - `GetBookingById`
   - `GetAllBookings`

## Booking Repository Interface (Application Layer)

5. In `Application/Common/Interfaces/IBookingRepository.cs`:
   - created `IBookingRepository`
   - updated `GetAllBookingsQueryHandler`

## Booking Repository Implementation (Infrastructure Layer)

6. In `Infrastructure/Repositories/BookingRepository.cs` created`BookingRepository`

## Bookings Controller (API Layer)

7. In `API/Controllers/v1` created `BookingsController`

## Database Migration (Infrastructure Layer)

8. In `Infrastructure/Data/Scripts/` created `0006_CreateBookingsTable.sql`

## Dependency Injection Registration (API Layer)

9. In `API/Program.cs` registered `IBookingRepository` → `BookingRepository`

## Bug Fixes & Missing Services

10. Testing revealed missing services and bugs:
    - Created `UserLoginDto` in `LoginCommandHandler.cs`
    - Created `ICurrentUserService` interface in `Application/Common/Interfaces/`
    - Created `CurrentUserService` in `Infrastructure/Services/` (extracts userId from JWT NameIdentifier claim)
    - Added `Microsoft.AspNetCore.Http.Abstractions` package to Infrastructure project
    - Registered `HttpContextAccessor` and `ICurrentUserService` in `Program.cs`
    - Updated `CreateBookingCommandHandler` and `UpdateBookingCommandHandler` to use `ICurrentUserService.UserId`
    - Created `BookingMappingProfile.cs` in `Application/Common/Mappings/` for Booking → BookingDto mapping
    - Added `ManagerOrAbove` policy to `Program.cs` authorization configuration

## Testing Day 5 - Bookings CRUD & Business Logic

11. Tested all booking endpoints successfully:
    - **Create**: POST `/api/v1/bookings` → Created booking with userId from JWT, Status=Pending
    - **List**: GET `/api/v1/bookings?pageNumber=1&pageSize=10` → Returned paginated results with AutoMapper DTOs
    - **Update**: PUT `/api/v1/bookings/{id}` → Updated title, description, notes for Pending booking
    - **Confirm**: POST `/api/v1/bookings/{id}/confirm` (admin) → Changed status Pending → Confirmed ✅
    - **Cancel**: POST `/api/v1/bookings/{id}/cancel` → Changed status to Cancelled ✅
    - **Delete**: DELETE `/api/v1/bookings/{id}` (admin) → Hard delete from database ✅
    - **Multi-tenant isolation**: All queries tenant-filtered via BaseRepository
    - **HasConflictAsync SQL**: Verified time overlap detection `(StartTime < EndTime) AND (EndTime > StartTime)` working correctly
    - All authorization policies enforced (ManagerOrAbove for confirm, AdminOnly for delete) ✅
