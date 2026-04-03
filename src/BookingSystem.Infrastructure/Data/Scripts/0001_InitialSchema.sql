-- Create Tenants table
CREATE TABLE IF NOT EXISTS Tenants (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(200) NOT NULL,
    Subdomain VARCHAR(100) NOT NULL UNIQUE,
    Plan VARCHAR(50) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create Roles table
CREATE TABLE IF NOT EXISTS Roles (
    Id SERIAL PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description VARCHAR(500),
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Insert default roles
INSERT INTO Roles (Name, Description) VALUES
('TenantAdmin', 'Administrator of a tenant with full permissions'),
('Manager', 'Manager with permissions to manage resources and bookings'),
('User', 'Regular user who can create bookings')
ON CONFLICT (Name) DO NOTHING;

-- Create Users table
CREATE TABLE IF NOT EXISTS Users (
    Id SERIAL PRIMARY KEY,
    TenantId INT NOT NULL REFERENCES Tenants(Id) ON DELETE CASCADE,
    Email VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(500) NOT NULL,
    FirstName VARCHAR(100) NOT NULL,
    LastName VARCHAR(100) NOT NULL,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(TenantId, Email)
);

-- Create UserRoles junction table
CREATE TABLE IF NOT EXISTS UserRoles (
    UserId INT NOT NULL REFERENCES Users(Id) ON DELETE CASCADE,
    RoleId INT NOT NULL REFERENCES Roles(Id) ON DELETE CASCADE,
    AssignedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (UserId, RoleId)
);

-- Create indexes for performance
CREATE INDEX IF NOT EXISTS idx_users_tenantid ON Users(TenantId);
CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
CREATE INDEX IF NOT EXISTS idx_userroles_userid ON UserRoles(UserId);
CREATE INDEX IF NOT EXISTS idx_userroles_roleid ON UserRoles(RoleId);
