-- Add Email column to Tenants table and remove Subdomain
-- Required for email-based tenant registration (Day 4 fix)

-- Add Email column
ALTER TABLE Tenants ADD COLUMN Email VARCHAR(255);

-- Drop old subdomain column and its constraints/indexes
DROP INDEX IF EXISTS idx_tenants_subdomain;
ALTER TABLE Tenants DROP CONSTRAINT IF EXISTS tenants_subdomain_key;
ALTER TABLE Tenants DROP COLUMN IF EXISTS subdomain;

-- Make Email NOT NULL and unique
ALTER TABLE Tenants ALTER COLUMN Email SET NOT NULL;
CREATE UNIQUE INDEX idx_tenants_email ON Tenants(Email);
