-- Migration: Create Resources table
-- Description: Creates the Resources table for storing bookable resources (meeting rooms, equipment, etc.)

CREATE TABLE IF NOT EXISTS Resources (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(200) NOT NULL,
    Description VARCHAR(1000),
    ResourceType VARCHAR(100) NOT NULL,
    Capacity INT,
    IsActive BOOLEAN NOT NULL DEFAULT true,
    TenantId UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    UpdatedAt TIMESTAMP,
    
    -- Foreign key to Tenants table
    CONSTRAINT FK_Resources_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    -- Ensure resource names are unique within a tenant
    CONSTRAINT UQ_Resources_Name_TenantId UNIQUE (Name, TenantId)
);

-- Create index on TenantId for faster tenant-scoped queries
CREATE INDEX IF NOT EXISTS IX_Resources_TenantId ON Resources(TenantId);

-- Create index on ResourceType for filtering
CREATE INDEX IF NOT EXISTS IX_Resources_ResourceType ON Resources(ResourceType);

-- Create index on IsActive for filtering active resources
CREATE INDEX IF NOT EXISTS IX_Resources_IsActive ON Resources(IsActive);

-- Create composite index for common query pattern (tenant + active status)
CREATE INDEX IF NOT EXISTS IX_Resources_TenantId_IsActive ON Resources(TenantId, IsActive);
