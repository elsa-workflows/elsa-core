-- =============================================
-- Elsa Workflows 3.6.0 RC3 - TenantId Migration Script for PostgreSQL
-- =============================================
-- This script migrates NULL TenantId values to empty string ('') for default tenant
-- as per ADR-0008: Empty String as Default Tenant ID
--
-- IMPORTANT: 
-- - Backup your database before running this script
-- - This script converts NULL to '' (empty string) for default tenant
-- - It does NOT convert NULL to '*' (asterisk) - that is only for explicitly tenant-agnostic entities
-- - Review the script and test in a non-production environment first
--
-- Usage:
--   psql -h <host> -U <user> -d <database> -f migrate-tenantid-postgresql.sql
--
-- Or execute within pgAdmin or another PostgreSQL client
-- =============================================

\echo 'Starting TenantId migration for Elsa 3.6.0 RC3...'
\echo ''

-- Enable transaction for rollback capability
BEGIN;

DO $$
DECLARE
    v_count INTEGER;
BEGIN
    -- =============================================
    -- Management Context
    -- =============================================
    RAISE NOTICE 'Migrating Management Context tables...';
    
    -- WorkflowDefinitions
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkflowDefinitions') THEN
        SELECT COUNT(*) INTO v_count FROM "WorkflowDefinitions" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "WorkflowDefinitions" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in WorkflowDefinitions', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in WorkflowDefinitions';
        END IF;
    ELSE
        RAISE NOTICE '  WorkflowDefinitions table not found - skipping';
    END IF;
    
    -- WorkflowInstances
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkflowInstances') THEN
        SELECT COUNT(*) INTO v_count FROM "WorkflowInstances" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "WorkflowInstances" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in WorkflowInstances', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in WorkflowInstances';
        END IF;
    ELSE
        RAISE NOTICE '  WorkflowInstances table not found - skipping';
    END IF;
    
    RAISE NOTICE '';
    
    -- =============================================
    -- Runtime Context
    -- =============================================
    RAISE NOTICE 'Migrating Runtime Context tables...';
    
    -- Triggers (StoredTrigger)
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Triggers') THEN
        SELECT COUNT(*) INTO v_count FROM "Triggers" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "Triggers" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in Triggers', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in Triggers';
        END IF;
    ELSE
        RAISE NOTICE '  Triggers table not found - skipping';
    END IF;
    
    -- Bookmarks (StoredBookmark)
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Bookmarks') THEN
        SELECT COUNT(*) INTO v_count FROM "Bookmarks" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "Bookmarks" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in Bookmarks', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in Bookmarks';
        END IF;
    ELSE
        RAISE NOTICE '  Bookmarks table not found - skipping';
    END IF;
    
    -- BookmarkQueueItems
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'BookmarkQueueItems') THEN
        SELECT COUNT(*) INTO v_count FROM "BookmarkQueueItems" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "BookmarkQueueItems" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in BookmarkQueueItems', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in BookmarkQueueItems';
        END IF;
    ELSE
        RAISE NOTICE '  BookmarkQueueItems table not found - skipping';
    END IF;
    
    -- ActivityExecutionRecords
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'ActivityExecutionRecords') THEN
        SELECT COUNT(*) INTO v_count FROM "ActivityExecutionRecords" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "ActivityExecutionRecords" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in ActivityExecutionRecords', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in ActivityExecutionRecords';
        END IF;
    ELSE
        RAISE NOTICE '  ActivityExecutionRecords table not found - skipping';
    END IF;
    
    -- WorkflowExecutionLogRecords
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkflowExecutionLogRecords') THEN
        SELECT COUNT(*) INTO v_count FROM "WorkflowExecutionLogRecords" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "WorkflowExecutionLogRecords" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in WorkflowExecutionLogRecords', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in WorkflowExecutionLogRecords';
        END IF;
    ELSE
        RAISE NOTICE '  WorkflowExecutionLogRecords table not found - skipping';
    END IF;
    
    -- WorkflowInboxMessages
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkflowInboxMessages') THEN
        SELECT COUNT(*) INTO v_count FROM "WorkflowInboxMessages" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "WorkflowInboxMessages" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in WorkflowInboxMessages', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in WorkflowInboxMessages';
        END IF;
    ELSE
        RAISE NOTICE '  WorkflowInboxMessages table not found - skipping';
    END IF;
    
    -- KeyValuePairs (SerializedKeyValuePair)
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'KeyValuePairs') THEN
        SELECT COUNT(*) INTO v_count FROM "KeyValuePairs" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "KeyValuePairs" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in KeyValuePairs', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in KeyValuePairs';
        END IF;
    ELSE
        RAISE NOTICE '  KeyValuePairs table not found - skipping';
    END IF;
    
    RAISE NOTICE '';
    
    -- =============================================
    -- Identity Context
    -- =============================================
    RAISE NOTICE 'Migrating Identity Context tables...';
    
    -- Users
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Users') THEN
        SELECT COUNT(*) INTO v_count FROM "Users" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "Users" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in Users', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in Users';
        END IF;
    ELSE
        RAISE NOTICE '  Users table not found - skipping';
    END IF;
    
    -- Applications
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Applications') THEN
        SELECT COUNT(*) INTO v_count FROM "Applications" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "Applications" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in Applications', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in Applications';
        END IF;
    ELSE
        RAISE NOTICE '  Applications table not found - skipping';
    END IF;
    
    -- Roles
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Roles') THEN
        SELECT COUNT(*) INTO v_count FROM "Roles" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "Roles" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in Roles', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in Roles';
        END IF;
    ELSE
        RAISE NOTICE '  Roles table not found - skipping';
    END IF;
    
    RAISE NOTICE '';
    
    -- =============================================
    -- Labels Context
    -- =============================================
    RAISE NOTICE 'Migrating Labels Context tables...';
    
    -- Labels
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Labels') THEN
        SELECT COUNT(*) INTO v_count FROM "Labels" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "Labels" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in Labels', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in Labels';
        END IF;
    ELSE
        RAISE NOTICE '  Labels table not found - skipping';
    END IF;
    
    -- WorkflowDefinitionLabels
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'WorkflowDefinitionLabels') THEN
        SELECT COUNT(*) INTO v_count FROM "WorkflowDefinitionLabels" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "WorkflowDefinitionLabels" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in WorkflowDefinitionLabels', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in WorkflowDefinitionLabels';
        END IF;
    ELSE
        RAISE NOTICE '  WorkflowDefinitionLabels table not found - skipping';
    END IF;
    
    RAISE NOTICE '';
    
    -- =============================================
    -- Alterations Context
    -- =============================================
    RAISE NOTICE 'Migrating Alterations Context tables...';
    
    -- AlterationPlans
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AlterationPlans') THEN
        SELECT COUNT(*) INTO v_count FROM "AlterationPlans" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "AlterationPlans" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in AlterationPlans', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in AlterationPlans';
        END IF;
    ELSE
        RAISE NOTICE '  AlterationPlans table not found - skipping';
    END IF;
    
    -- AlterationJobs
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'AlterationJobs') THEN
        SELECT COUNT(*) INTO v_count FROM "AlterationJobs" WHERE "TenantId" IS NULL;
        
        IF v_count > 0 THEN
            UPDATE "AlterationJobs" 
            SET "TenantId" = '' 
            WHERE "TenantId" IS NULL;
            RAISE NOTICE '  Updated % rows in AlterationJobs', v_count;
        ELSE
            RAISE NOTICE '  No rows to update in AlterationJobs';
        END IF;
    ELSE
        RAISE NOTICE '  AlterationJobs table not found - skipping';
    END IF;
    
    RAISE NOTICE '';
    RAISE NOTICE 'Migration completed successfully!';
    RAISE NOTICE '';
    RAISE NOTICE 'Review the changes above. If everything looks correct, the transaction will be committed.';
    RAISE NOTICE 'If you want to undo these changes, you can abort now with Ctrl+C or rollback the transaction.';
    
EXCEPTION
    WHEN OTHERS THEN
        RAISE NOTICE '';
        RAISE NOTICE 'ERROR: Migration failed!';
        RAISE NOTICE 'Error message: %', SQLERRM;
        RAISE;
END $$;

-- Commit the transaction
COMMIT;

\echo 'Changes committed successfully!'
