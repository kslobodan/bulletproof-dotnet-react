# Day 5: Bookings CRUD & Business Logic

---

## Booking Entity & Enum (Domain Layer)

1. In `Domain/Entities` creted `Booking`, in `Domain/Enums` created `BookingStatus`

## Booking DTOs (Application Layer)

2. In `Application/Features/Bookings/DTOs` created:
   - `BookingDto`
   - `BookingStatisticsDto`
   - `CreateBookingRequest`
   - `CreateBookingResponse`
   - `UpdateBookingRequest`
   - `UpdateBookingResponse`
   - `CancelBookingResponse`
   - `ConfirmBookingResponse`
   - `DeleteBookingResponse`

## Booking Repository Interface (Application, Infrastructure, API Layer)

3. - in `Application/Common/Interfaces` created `IBookingRepository`
   - In `Infrastructure/Repositories` created`BookingRepository`
   - in `API/Program.cs` registered `IBookingRepository`

## HTTP Context Setup (Infrastructure, API Layer)

4. - Add `Microsoft.AspNetCore.Http.Abstractions` package to Infrastructure project
   - Register `HttpContextAccessor` in `API/Program.cs`

## Current User Service Interface (Application, Infrastructure, API Layer)

5. - in `Application/Common/Interfaces` created `ICurrentUserService`
   - in `Infrastructure/Services` created `CurrentUserService` (extracts userId from JWT NameIdentifier claim)
   - in `API/Program.cs` registered `ICurrentUserService`

## Booking Commands (Application Layer)

6.  In `Application/Features/Bookings/Commands/` created:
    - `CreateBooking`
    - `UpdateBooking`
    - `CancelBooking`
    - `ConfirmBooking`
    - `DeleteBooking`

## Booking Queries (Application Layer)

7. In `Application/Features/Bookings/Queries/GetAllBookings` created:
   - `GetAllBookingsQuery`
   - `GetAllBookingsQueryHandler`
   - `GetAllBookingsQueryValidator`

   In `Application/Features/Bookings/Queries/GetBookingById` created:
   - `GetBookingByIdQuery`
   - `GetBookingByIdQueryHandler`

## Bookings Controller (API Layer)

8. In `API/Controllers/v1` created `BookingsController`

## Database Migration (Infrastructure Layer)

9. In `Infrastructure/Data/Scripts/` created `0005_CreateBookingsTable.sql`

## Bug Fixes & Missing Services

10. Testing revealed missing services and bugs:

- Created `UserLoginDto` in `LoginCommandHandler.cs`
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
