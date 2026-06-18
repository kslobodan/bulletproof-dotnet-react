-- Migration 0009: Add Soft Delete Support
-- Adds IsDeleted and DeletedAt columns to Resources, Bookings, and AvailabilityRules tables

-- Add soft delete columns to Resources table
ALTER TABLE Resources
ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN DeletedAt TIMESTAMP NULL;

-- Add index for soft delete queries on Resources
CREATE INDEX IX_Resources_IsDeleted ON Resources(TenantId, IsDeleted);

-- Add soft delete columns to Bookings table
ALTER TABLE Bookings
ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN DeletedAt TIMESTAMP NULL;

-- Add index for soft delete queries on Bookings
CREATE INDEX IX_Bookings_IsDeleted ON Bookings(TenantId, IsDeleted);

-- Add soft delete columns to AvailabilityRules table
ALTER TABLE AvailabilityRules
ADD COLUMN IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
ADD COLUMN DeletedAt TIMESTAMP NULL;

-- Add index for soft delete queries on AvailabilityRules
CREATE INDEX IX_AvailabilityRules_IsDeleted ON AvailabilityRules(TenantId, IsDeleted);
