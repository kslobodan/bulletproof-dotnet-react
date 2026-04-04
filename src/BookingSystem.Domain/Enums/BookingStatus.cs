namespace BookingSystem.Domain.Enums;

public enum BookingStatus
{
    Pending = 0,      // Initial state after creation
    Confirmed = 1,    // Approved by admin/manager
    Completed = 2,    // Booking time has passed
    Cancelled = 3,    // Cancelled by user or admin
    Rejected = 4      // Denied by admin/manager
}
