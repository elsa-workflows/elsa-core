-- =============================================
-- Elsa Workflows 3.6.0 RC3 - TenantId Migration Script for MySQL
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
--   mysql -h <host> -u <user> -p <database> < migrate-tenantid-mysql.sql
--
-- Or execute within MySQL Workbench or another MySQL client
-- =============================================

SELECT 'Starting TenantId migration for Elsa 3.6.0 RC3...' AS '';
SELECT '' AS '';

-- Enable transaction for rollback capability
START TRANSACTION;

-- Set variables for counting
SET @count = 0;

-- =============================================
-- Management Context
-- =============================================
SELECT 'Migrating Management Context tables...' AS '';

-- WorkflowDefinitions
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'WorkflowDefinitions';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `WorkflowDefinitions` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `WorkflowDefinitions` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in WorkflowDefinitions') AS '';
    ELSE
        SELECT '  No rows to update in WorkflowDefinitions' AS '';
    END IF;
ELSE
    SELECT '  WorkflowDefinitions table not found - skipping' AS '';
END IF;

-- WorkflowInstances
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'WorkflowInstances';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `WorkflowInstances` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `WorkflowInstances` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in WorkflowInstances') AS '';
    ELSE
        SELECT '  No rows to update in WorkflowInstances' AS '';
    END IF;
ELSE
    SELECT '  WorkflowInstances table not found - skipping' AS '';
END IF;

SELECT '' AS '';

-- =============================================
-- Runtime Context
-- =============================================
SELECT 'Migrating Runtime Context tables...' AS '';

-- Triggers (StoredTrigger)
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'Triggers';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `Triggers` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `Triggers` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in Triggers') AS '';
    ELSE
        SELECT '  No rows to update in Triggers' AS '';
    END IF;
ELSE
    SELECT '  Triggers table not found - skipping' AS '';
END IF;

-- Bookmarks (StoredBookmark)
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'Bookmarks';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `Bookmarks` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `Bookmarks` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in Bookmarks') AS '';
    ELSE
        SELECT '  No rows to update in Bookmarks' AS '';
    END IF;
ELSE
    SELECT '  Bookmarks table not found - skipping' AS '';
END IF;

-- BookmarkQueueItems
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'BookmarkQueueItems';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `BookmarkQueueItems` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `BookmarkQueueItems` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in BookmarkQueueItems') AS '';
    ELSE
        SELECT '  No rows to update in BookmarkQueueItems' AS '';
    END IF;
ELSE
    SELECT '  BookmarkQueueItems table not found - skipping' AS '';
END IF;

-- ActivityExecutionRecords
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'ActivityExecutionRecords';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `ActivityExecutionRecords` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `ActivityExecutionRecords` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in ActivityExecutionRecords') AS '';
    ELSE
        SELECT '  No rows to update in ActivityExecutionRecords' AS '';
    END IF;
ELSE
    SELECT '  ActivityExecutionRecords table not found - skipping' AS '';
END IF;

-- WorkflowExecutionLogRecords
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'WorkflowExecutionLogRecords';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `WorkflowExecutionLogRecords` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `WorkflowExecutionLogRecords` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in WorkflowExecutionLogRecords') AS '';
    ELSE
        SELECT '  No rows to update in WorkflowExecutionLogRecords' AS '';
    END IF;
ELSE
    SELECT '  WorkflowExecutionLogRecords table not found - skipping' AS '';
END IF;

-- WorkflowInboxMessages
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'WorkflowInboxMessages';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `WorkflowInboxMessages` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `WorkflowInboxMessages` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in WorkflowInboxMessages') AS '';
    ELSE
        SELECT '  No rows to update in WorkflowInboxMessages' AS '';
    END IF;
ELSE
    SELECT '  WorkflowInboxMessages table not found - skipping' AS '';
END IF;

-- KeyValuePairs (SerializedKeyValuePair)
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'KeyValuePairs';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `KeyValuePairs` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `KeyValuePairs` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in KeyValuePairs') AS '';
    ELSE
        SELECT '  No rows to update in KeyValuePairs' AS '';
    END IF;
ELSE
    SELECT '  KeyValuePairs table not found - skipping' AS '';
END IF;

SELECT '' AS '';

-- =============================================
-- Identity Context
-- =============================================
SELECT 'Migrating Identity Context tables...' AS '';

-- Users
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'Users';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `Users` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `Users` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in Users') AS '';
    ELSE
        SELECT '  No rows to update in Users' AS '';
    END IF;
ELSE
    SELECT '  Users table not found - skipping' AS '';
END IF;

-- Applications
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'Applications';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `Applications` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `Applications` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in Applications') AS '';
    ELSE
        SELECT '  No rows to update in Applications' AS '';
    END IF;
ELSE
    SELECT '  Applications table not found - skipping' AS '';
END IF;

-- Roles
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'Roles';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `Roles` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `Roles` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in Roles') AS '';
    ELSE
        SELECT '  No rows to update in Roles' AS '';
    END IF;
ELSE
    SELECT '  Roles table not found - skipping' AS '';
END IF;

SELECT '' AS '';

-- =============================================
-- Labels Context
-- =============================================
SELECT 'Migrating Labels Context tables...' AS '';

-- Labels
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'Labels';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `Labels` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `Labels` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in Labels') AS '';
    ELSE
        SELECT '  No rows to update in Labels' AS '';
    END IF;
ELSE
    SELECT '  Labels table not found - skipping' AS '';
END IF;

-- WorkflowDefinitionLabels
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'WorkflowDefinitionLabels';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `WorkflowDefinitionLabels` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `WorkflowDefinitionLabels` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in WorkflowDefinitionLabels') AS '';
    ELSE
        SELECT '  No rows to update in WorkflowDefinitionLabels' AS '';
    END IF;
ELSE
    SELECT '  WorkflowDefinitionLabels table not found - skipping' AS '';
END IF;

SELECT '' AS '';

-- =============================================
-- Alterations Context
-- =============================================
SELECT 'Migrating Alterations Context tables...' AS '';

-- AlterationPlans
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'AlterationPlans';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `AlterationPlans` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `AlterationPlans` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in AlterationPlans') AS '';
    ELSE
        SELECT '  No rows to update in AlterationPlans' AS '';
    END IF;
ELSE
    SELECT '  AlterationPlans table not found - skipping' AS '';
END IF;

-- AlterationJobs
SET @table_exists = 0;
SELECT COUNT(*) INTO @table_exists FROM information_schema.tables 
WHERE table_schema = DATABASE() AND table_name = 'AlterationJobs';

IF @table_exists > 0 THEN
    SELECT COUNT(*) INTO @count FROM `AlterationJobs` WHERE `TenantId` IS NULL;
    
    IF @count > 0 THEN
        UPDATE `AlterationJobs` 
        SET `TenantId` = '' 
        WHERE `TenantId` IS NULL;
        SELECT CONCAT('  Updated ', @count, ' rows in AlterationJobs') AS '';
    ELSE
        SELECT '  No rows to update in AlterationJobs' AS '';
    END IF;
ELSE
    SELECT '  AlterationJobs table not found - skipping' AS '';
END IF;

SELECT '' AS '';
SELECT 'Migration completed successfully!' AS '';
SELECT '' AS '';
SELECT 'Review the changes above. If everything looks correct, the transaction will be committed.' AS '';
SELECT 'If you want to undo these changes, execute ROLLBACK; before this completes.' AS '';

-- Commit the transaction
COMMIT;

SELECT 'Changes committed successfully!' AS '';
