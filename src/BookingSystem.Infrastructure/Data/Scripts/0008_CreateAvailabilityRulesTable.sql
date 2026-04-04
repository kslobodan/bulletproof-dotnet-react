-- Migration: Create AvailabilityRules table
-- Description: Creates the AvailabilityRules table for defining when resources are available for booking

CREATE TABLE IF NOT EXISTS AvailabilityRules (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL,
    ResourceId UUID NOT NULL,
    DayOfWeek INT NOT NULL,  -- 0=Sunday, 1=Monday, 2=Tuesday, 3=Wednesday, 4=Thursday, 5=Friday, 6=Saturday
    StartTime TIME NOT NULL,
    EndTime TIME NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    EffectiveFrom TIMESTAMP,
    EffectiveTo TIMESTAMP,
    CreatedAt TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    UpdatedAt TIMESTAMP,
    
    -- Foreign key to Tenants table (cascade delete: if tenant deleted, delete all availability rules)
    CONSTRAINT FK_AvailabilityRules_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    -- Foreign key to Resources table (cascade delete: if resource deleted, delete all its availability rules)
    CONSTRAINT FK_AvailabilityRules_Resources FOREIGN KEY (ResourceId) 
        REFERENCES Resources(Id) ON DELETE CASCADE,
    
    -- Ensure EndTime is after StartTime
    CONSTRAINT CK_AvailabilityRules_ValidTimeRange CHECK (EndTime > StartTime),
    
    -- Ensure DayOfWeek is valid (0-6)
    CONSTRAINT CK_AvailabilityRules_ValidDayOfWeek CHECK (DayOfWeek >= 0 AND DayOfWeek <= 6),
    
    -- Ensure EffectiveTo is after EffectiveFrom when both are set
    CONSTRAINT CK_AvailabilityRules_ValidDateRange CHECK (EffectiveTo IS NULL OR EffectiveFrom IS NULL OR EffectiveTo > EffectiveFrom)
);

-- Index on TenantId for tenant-scoped queries (most common filter)
CREATE INDEX IF NOT EXISTS IX_AvailabilityRules_TenantId ON AvailabilityRules(TenantId);

-- Index on ResourceId for resource availability queries
CREATE INDEX IF NOT EXISTS IX_AvailabilityRules_ResourceId ON AvailabilityRules(ResourceId);

-- Index on DayOfWeek for filtering by day
CREATE INDEX IF NOT EXISTS IX_AvailabilityRules_DayOfWeek ON AvailabilityRules(DayOfWeek);

-- Index on IsActive for filtering active rules
CREATE INDEX IF NOT EXISTS IX_AvailabilityRules_IsActive ON AvailabilityRules(IsActive);

-- Composite index for common query pattern: get all availability rules for a specific resource
-- Query pattern: WHERE TenantId = ? AND ResourceId = ? ORDER BY DayOfWeek, StartTime
CREATE INDEX IF NOT EXISTS IX_AvailabilityRules_ResourceLookup 
    ON AvailabilityRules(TenantId, ResourceId, DayOfWeek, StartTime);

-- Composite index for filtering by tenant, day, and active status
CREATE INDEX IF NOT EXISTS IX_AvailabilityRules_FilterLookup 
    ON AvailabilityRules(TenantId, DayOfWeek, IsActive);
