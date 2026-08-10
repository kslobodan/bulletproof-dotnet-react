-- Migration: Create RefreshTokens table
-- Purpose: Store refresh tokens for secure token rotation authentication
-- Date: 2026-04-05

CREATE TABLE IF NOT EXISTS RefreshTokens (
    Id UUID PRIMARY KEY,
    Token VARCHAR(500) NOT NULL UNIQUE,
    UserId UUID NOT NULL,
    TenantId UUID NOT NULL,
    CreatedAt TIMESTAMP NOT NULL,
    ExpiresAt TIMESTAMP NOT NULL,
    IsRevoked BOOLEAN NOT NULL DEFAULT FALSE,
    RevokedAt TIMESTAMP NULL,
    ReplacedByToken VARCHAR(500) NULL,
    
    -- Foreign keys
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) 
        REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RefreshTokens_Tenants FOREIGN KEY (TenantId) 
        REFERENCES Tenants(Id) ON DELETE CASCADE
);

-- Index on Token for fast lookup during refresh
CREATE INDEX IF NOT EXISTS IX_RefreshTokens_Token ON RefreshTokens(Token);

-- Index on UserId for user-specific queries
CREATE INDEX IF NOT EXISTS IX_RefreshTokens_UserId ON RefreshTokens(UserId);

-- Index on TenantId for tenant isolation
CREATE INDEX IF NOT EXISTS IX_RefreshTokens_TenantId ON RefreshTokens(TenantId);

-- Composite index for cleanup queries (expired/revoked tokens)
CREATE INDEX IF NOT EXISTS IX_RefreshTokens_IsRevoked_ExpiresAt 
    ON RefreshTokens(IsRevoked, ExpiresAt);

-- Index on CreatedAt for cleanup (delete old tokens)
CREATE INDEX IF NOT EXISTS IX_RefreshTokens_CreatedAt ON RefreshTokens(CreatedAt);
