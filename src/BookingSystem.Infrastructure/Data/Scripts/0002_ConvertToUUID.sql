-- Drop existing tables to recreate with UUID types
DROP TABLE IF EXISTS UserRoles CASCADE;
DROP TABLE IF EXISTS Users CASCADE;
DROP TABLE IF EXISTS Roles CASCADE;
DROP TABLE IF EXISTS Tenants CASCADE;

-- Tenants table (using UUID)
CREATE TABLE Tenants (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    Name VARCHAR(200) NOT NULL,
    Subdomain VARCHAR(100) NOT NULL UNIQUE,
    Plan VARCHAR(50) NOT NULL DEFAULT 'Free',
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_tenants_subdomain ON Tenants(Subdomain);
CREATE INDEX idx_tenants_isactive ON Tenants(IsActive);

-- Roles table (NOT tenant-scoped - global roles)
CREATE TABLE Roles (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description VARCHAR(500),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Pre-seed roles
INSERT INTO Roles (Name, Description) VALUES
('TenantAdmin', 'Administrator of a tenant with full permissions'),
('Manager', 'Manager with permissions to manage resources and bookings'),
('User', 'Regular user who can create bookings');

-- Users table (tenant-scoped, using UUID)
CREATE TABLE Users (
    Id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    TenantId UUID NOT NULL REFERENCES Tenants(Id) ON DELETE CASCADE,
    Email VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(500) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NULL,
    CONSTRAINT users_tenantid_email_unique UNIQUE (TenantId, Email)
);

CREATE INDEX idx_users_tenantid ON Users(TenantId);
CREATE INDEX idx_users_email ON Users(Email);
CREATE INDEX idx_users_tenantid_email ON Users(TenantId, Email);

-- UserRoles junction table (tenant-scoped)
CREATE TABLE UserRoles (
    UserId UUID NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    RoleId INT NOT NULL REFERENCES Roles(Id) ON DELETE CASCADE,
    TenantId UUID NOT NULL REFERENCES Tenants(Id) ON DELETE CASCADE,
    AssignedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (UserId, RoleId, TenantId)
);

CREATE INDEX idx_userroles_userid ON UserRoles(UserId);
CREATE INDEX idx_userroles_tenantid ON UserRoles(TenantId);
