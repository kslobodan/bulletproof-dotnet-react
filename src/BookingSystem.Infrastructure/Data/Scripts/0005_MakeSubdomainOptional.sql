-- Make subdomain column optional (Day 4 fix)
-- Email is now the primary identifier for tenants

ALTER TABLE Tenants DROP CONSTRAINT IF EXISTS tenants_subdomain_key;
ALTER TABLE Tenants ALTER COLUMN subdomain DROP NOT NULL;
