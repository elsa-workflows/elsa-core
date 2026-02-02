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
--
-- Note: This script updates tables even if they don't exist (MySQL will skip missing tables with a warning)
-- =============================================

SELECT 'Starting TenantId migration for Elsa 3.6.0 RC3...' AS 'Status';
SELECT '' AS 'Status';

-- Enable transaction for rollback capability
START TRANSACTION;

-- =============================================
-- Management Context
-- =============================================
SELECT 'Migrating Management Context tables...' AS 'Status';

-- WorkflowDefinitions
UPDATE `WorkflowDefinitions` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in WorkflowDefinitions') AS 'Status';

-- WorkflowInstances
UPDATE `WorkflowInstances` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in WorkflowInstances') AS 'Status';

SELECT '' AS 'Status';

-- =============================================
-- Runtime Context
-- =============================================
SELECT 'Migrating Runtime Context tables...' AS 'Status';

-- Triggers (StoredTrigger)
UPDATE `Triggers` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in Triggers') AS 'Status';

-- Bookmarks (StoredBookmark)
UPDATE `Bookmarks` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in Bookmarks') AS 'Status';

-- BookmarkQueueItems
UPDATE `BookmarkQueueItems` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in BookmarkQueueItems') AS 'Status';

-- ActivityExecutionRecords
UPDATE `ActivityExecutionRecords` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in ActivityExecutionRecords') AS 'Status';

-- WorkflowExecutionLogRecords
UPDATE `WorkflowExecutionLogRecords` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in WorkflowExecutionLogRecords') AS 'Status';

-- WorkflowInboxMessages
UPDATE `WorkflowInboxMessages` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in WorkflowInboxMessages') AS 'Status';

-- KeyValuePairs (SerializedKeyValuePair)
UPDATE `KeyValuePairs` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in KeyValuePairs') AS 'Status';

SELECT '' AS 'Status';

-- =============================================
-- Identity Context
-- =============================================
SELECT 'Migrating Identity Context tables...' AS 'Status';

-- Users
UPDATE `Users` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in Users') AS 'Status';

-- Applications
UPDATE `Applications` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in Applications') AS 'Status';

-- Roles
UPDATE `Roles` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in Roles') AS 'Status';

SELECT '' AS 'Status';

-- =============================================
-- Labels Context
-- =============================================
SELECT 'Migrating Labels Context tables...' AS 'Status';

-- Labels
UPDATE `Labels` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in Labels') AS 'Status';

-- WorkflowDefinitionLabels
UPDATE `WorkflowDefinitionLabels` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in WorkflowDefinitionLabels') AS 'Status';

SELECT '' AS 'Status';

-- =============================================
-- Alterations Context
-- =============================================
SELECT 'Migrating Alterations Context tables...' AS 'Status';

-- AlterationPlans
UPDATE `AlterationPlans` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in AlterationPlans') AS 'Status';

-- AlterationJobs
UPDATE `AlterationJobs` SET `TenantId` = '' WHERE `TenantId` IS NULL;
SELECT CONCAT('  Updated ', ROW_COUNT(), ' rows in AlterationJobs') AS 'Status';

SELECT '' AS 'Status';
SELECT 'Migration completed successfully!' AS 'Status';
SELECT '' AS 'Status';

-- Commit the transaction
-- If you need to rollback, press Ctrl+C now or execute: ROLLBACK;
COMMIT;

SELECT 'Changes committed successfully!' AS 'Status';
