-- Migration: Create AuditLogs table
-- Description: Creates the AuditLogs table for tracking all CUD operations for compliance and security

CREATE TABLE IF NOT EXISTS AuditLogs (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL,
    EntityName VARCHAR(100) NOT NULL,
    EntityId UUID NOT NULL,
    Action VARCHAR(50) NOT NULL,  -- Create, Update, Delete, Cancel, Confirm
    OldValues TEXT,  -- JSON snapshot before change (null for Create)
    NewValues TEXT,  -- JSON snapshot after change (null for Delete)
    UserId UUID NOT NULL,
    Timestamp TIMESTAMP NOT NULL DEFAULT (NOW() AT TIME ZONE 'UTC'),
    IpAddress VARCHAR(50),
    Reason VARCHAR(500),
    
    -- Foreign key to Tenants table (cascade delete: if tenant deleted, delete audit logs)
    CONSTRAINT FK_AuditLogs_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE,
    
    -- Foreign key to Users table (restrict delete: cannot delete user with audit history)
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) 
        REFERENCES Users(Id) ON DELETE RESTRICT
);

-- Index on TenantId for tenant-scoped queries (most common filter)
CREATE INDEX IF NOT EXISTS IX_AuditLogs_TenantId ON AuditLogs(TenantId);

-- Index on EntityName and EntityId for entity history lookups
CREATE INDEX IF NOT EXISTS IX_AuditLogs_EntityName_EntityId ON AuditLogs(EntityName, EntityId);

-- Index on UserId for user activity queries
CREATE INDEX IF NOT EXISTS IX_AuditLogs_UserId ON AuditLogs(UserId);

-- Index on Timestamp for chronological ordering (DESC for recent first)
CREATE INDEX IF NOT EXISTS IX_AuditLogs_Timestamp ON AuditLogs(Timestamp DESC);

-- Index on Action for filtering by operation type
CREATE INDEX IF NOT EXISTS IX_AuditLogs_Action ON AuditLogs(Action);

-- Composite index for admin audit trail queries (tenant + entity + time)
CREATE INDEX IF NOT EXISTS IX_AuditLogs_Tenant_Entity_Time ON AuditLogs(TenantId, EntityName, Timestamp DESC);
