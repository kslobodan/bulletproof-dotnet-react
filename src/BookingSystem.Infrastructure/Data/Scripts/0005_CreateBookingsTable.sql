-- Migration: Create Bookings table
-- Description: Creates the Bookings table for storing resource reservations with conflict detection support

CREATE TABLE IF NOT EXISTS Bookings (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL,
    ResourceId UUID NOT NULL,
    UserId UUID NOT NULL,
    StartTime TIMESTAMP NOT NULL,
    EndTime TIMESTAMP NOT NULL,
    Status INT NOT NULL DEFAULT 0,  -- 0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled, 4=Rejected
    Title VARCHAR(200),
    Description VARCHAR(1000),
    Notes VARCHAR(1000),
    CreatedAt TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    UpdatedAt TIMESTAMP,
    CreatedBy UUID,
    UpdatedBy UUID,
    
    -- Foreign key to Tenants table (cascade delete: if tenant deleted, delete all bookings)
    CONSTRAINT FK_Bookings_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    -- Foreign key to Resources table (restrict delete: cannot delete resource with bookings)
    CONSTRAINT FK_Bookings_Resources FOREIGN KEY (ResourceId) 
        REFERENCES Resources(Id) ON DELETE RESTRICT,
    
    -- Foreign key to Users table (restrict delete: cannot delete user with bookings)
    CONSTRAINT FK_Bookings_Users FOREIGN KEY (UserId) 
        REFERENCES Users(Id) ON DELETE RESTRICT,
    
    -- Ensure EndTime is after StartTime
    CONSTRAINT CK_Bookings_ValidTimeRange CHECK (EndTime > StartTime)
);

-- Index on TenantId for tenant-scoped queries (most common filter)
CREATE INDEX IF NOT EXISTS IX_Bookings_TenantId ON Bookings(TenantId);

-- Index on ResourceId for resource availability queries
CREATE INDEX IF NOT EXISTS IX_Bookings_ResourceId ON Bookings(ResourceId);

-- Index on UserId for user booking history
CREATE INDEX IF NOT EXISTS IX_Bookings_UserId ON Bookings(UserId);

-- Index on Status for filtering by booking status
CREATE INDEX IF NOT EXISTS IX_Bookings_Status ON Bookings(Status);

-- Index on StartTime for date-based queries and sorting
CREATE INDEX IF NOT EXISTS IX_Bookings_StartTime ON Bookings(StartTime);

-- Composite index for conflict detection queries (critical for performance)
-- Query pattern: WHERE TenantId = ? AND ResourceId = ? AND Status IN (0,1) AND StartTime < ? AND EndTime > ?
CREATE INDEX IF NOT EXISTS IX_Bookings_ConflictDetection 
    ON Bookings(TenantId, ResourceId, Status, StartTime, EndTime);
