-- Make subdomain column optional (Day 4 fix)
-- Email is now the primary identifier for tenants  
-- Made idempotent for fresh databases where subdomain never existed (0002)

DO $$
BEGIN
    -- Drop unique constraint if it exists
    IF EXISTS (SELECT 1 FROM pg_constraint c JOIN pg_class t ON c.conrelid = t.oid 
               WHERE c.conname = 'tenants_subdomain_key' AND t.relname = 'tenants') THEN
        EXECUTE 'ALTER TABLE tenants DROP CONSTRAINT tenants_subdomain_key';
    END IF;
    
    -- Make subdomain nullable if column exists
    IF EXISTS (SELECT 1 FROM information_schema.columns 
               WHERE table_schema='public' AND table_name='tenants' AND column_name='subdomain') THEN
        EXECUTE 'ALTER TABLE tenants ALTER COLUMN subdomain DROP NOT NULL';
    END IF;
END $$;
