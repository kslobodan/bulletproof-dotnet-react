-- Add Email column to Tenants table and remove Subdomain
-- Required for email-based tenant registration (Day 4 fix)
-- Made idempotent for fresh database setups (email already exists in 0002, subdomain doesn't exist)
-- NOTE: This migration is essentially a no-op when running from scratch (0002 handles everything)
-- It only applies when upgrading an existing database that has subdomain but no email

-- Add Email column if it doesn't exist (already exists from 0002 in fresh databases)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                   WHERE table_schema='public' AND table_name='tenants' AND column_name='email') THEN
        ALTER TABLE tenants ADD COLUMN Email VARCHAR(255);
    END IF;
END $$;

-- Drop old subdomain column and its constraints/indexes (only applies to upgraded databases)
-- In fresh databases (0002), subdomain never existed, so this is skipped
DO $$
BEGIN
    -- Drop index if it exists
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'idx_tenants_subdomain') THEN
       EXECUTE 'DROP INDEX idx_tenants_subdomain';
    END IF;
    
    -- Drop unique constraint if it exists
    IF EXISTS (SELECT 1 FROM pg_constraint c JOIN pg_class t ON c.conrelid = t.oid 
               WHERE c.conname = 'tenants_subdomain_key' AND t.relname = 'tenants') THEN
        EXECUTE 'ALTER TABLE tenants DROP CONSTRAINT tenants_subdomain_key';
    END IF;
    
    -- Drop column if it exists
    IF EXISTS (SELECT 1 FROM information_schema.columns 
               WHERE table_schema='public' AND table_name='tenants' AND column_name='subdomain') THEN
        EXECUTE 'ALTER TABLE tenants DROP COLUMN subdomain';
    END IF;
END $$;

-- Make Email NOT NULL and unique (already done in 0002)
DO $$
BEGIN
    -- Only set NOT NULL if column is nullable
    IF EXISTS (SELECT 1 FROM information_schema.columns 
               WHERE table_schema='public' AND table_name='tenants' AND column_name='email' AND is_nullable='YES') THEN
        ALTER TABLE tenants ALTER COLUMN email SET NOT NULL;
    END IF;
END $$;

-- Create unique index only if it doesn't exist (already exists from 0002)
CREATE UNIQUE INDEX IF NOT EXISTS idx_tenants_email ON tenants(email);
