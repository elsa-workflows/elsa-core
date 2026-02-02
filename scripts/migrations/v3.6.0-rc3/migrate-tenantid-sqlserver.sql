-- =============================================
-- Elsa Workflows 3.6.0 RC3 - TenantId Migration Script for SQL Server
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
--   sqlcmd -S <server> -d <database> -i migrate-tenantid-sqlserver.sql
--
-- Or execute within SQL Server Management Studio
-- =============================================

PRINT 'Starting TenantId migration for Elsa 3.6.0 RC3...';
PRINT '';

-- Enable transaction for rollback capability
BEGIN TRANSACTION;

BEGIN TRY
    -- =============================================
    -- Management Context
    -- =============================================
    PRINT 'Migrating Management Context tables...';
    
    -- WorkflowDefinitions
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WorkflowDefinitions')
    BEGIN
        DECLARE @workflowDefinitionsCount INT;
        SELECT @workflowDefinitionsCount = COUNT(*) FROM [WorkflowDefinitions] WHERE [TenantId] IS NULL;
        
        IF @workflowDefinitionsCount > 0
        BEGIN
            UPDATE [WorkflowDefinitions] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@workflowDefinitionsCount AS VARCHAR) + ' rows in WorkflowDefinitions';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in WorkflowDefinitions';
        END
    END
    ELSE
    BEGIN
        PRINT '  WorkflowDefinitions table not found - skipping';
    END
    
    -- WorkflowInstances
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WorkflowInstances')
    BEGIN
        DECLARE @workflowInstancesCount INT;
        SELECT @workflowInstancesCount = COUNT(*) FROM [WorkflowInstances] WHERE [TenantId] IS NULL;
        
        IF @workflowInstancesCount > 0
        BEGIN
            UPDATE [WorkflowInstances] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@workflowInstancesCount AS VARCHAR) + ' rows in WorkflowInstances';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in WorkflowInstances';
        END
    END
    ELSE
    BEGIN
        PRINT '  WorkflowInstances table not found - skipping';
    END
    
    PRINT '';
    
    -- =============================================
    -- Runtime Context
    -- =============================================
    PRINT 'Migrating Runtime Context tables...';
    
    -- Triggers (StoredTrigger)
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Triggers')
    BEGIN
        DECLARE @triggersCount INT;
        SELECT @triggersCount = COUNT(*) FROM [Triggers] WHERE [TenantId] IS NULL;
        
        IF @triggersCount > 0
        BEGIN
            UPDATE [Triggers] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@triggersCount AS VARCHAR) + ' rows in Triggers';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in Triggers';
        END
    END
    ELSE
    BEGIN
        PRINT '  Triggers table not found - skipping';
    END
    
    -- Bookmarks (StoredBookmark)
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Bookmarks')
    BEGIN
        DECLARE @bookmarksCount INT;
        SELECT @bookmarksCount = COUNT(*) FROM [Bookmarks] WHERE [TenantId] IS NULL;
        
        IF @bookmarksCount > 0
        BEGIN
            UPDATE [Bookmarks] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@bookmarksCount AS VARCHAR) + ' rows in Bookmarks';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in Bookmarks';
        END
    END
    ELSE
    BEGIN
        PRINT '  Bookmarks table not found - skipping';
    END
    
    -- BookmarkQueueItems
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'BookmarkQueueItems')
    BEGIN
        DECLARE @bookmarkQueueItemsCount INT;
        SELECT @bookmarkQueueItemsCount = COUNT(*) FROM [BookmarkQueueItems] WHERE [TenantId] IS NULL;
        
        IF @bookmarkQueueItemsCount > 0
        BEGIN
            UPDATE [BookmarkQueueItems] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@bookmarkQueueItemsCount AS VARCHAR) + ' rows in BookmarkQueueItems';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in BookmarkQueueItems';
        END
    END
    ELSE
    BEGIN
        PRINT '  BookmarkQueueItems table not found - skipping';
    END
    
    -- ActivityExecutionRecords
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ActivityExecutionRecords')
    BEGIN
        DECLARE @activityExecutionRecordsCount INT;
        SELECT @activityExecutionRecordsCount = COUNT(*) FROM [ActivityExecutionRecords] WHERE [TenantId] IS NULL;
        
        IF @activityExecutionRecordsCount > 0
        BEGIN
            UPDATE [ActivityExecutionRecords] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@activityExecutionRecordsCount AS VARCHAR) + ' rows in ActivityExecutionRecords';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in ActivityExecutionRecords';
        END
    END
    ELSE
    BEGIN
        PRINT '  ActivityExecutionRecords table not found - skipping';
    END
    
    -- WorkflowExecutionLogRecords
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WorkflowExecutionLogRecords')
    BEGIN
        DECLARE @workflowExecutionLogRecordsCount INT;
        SELECT @workflowExecutionLogRecordsCount = COUNT(*) FROM [WorkflowExecutionLogRecords] WHERE [TenantId] IS NULL;
        
        IF @workflowExecutionLogRecordsCount > 0
        BEGIN
            UPDATE [WorkflowExecutionLogRecords] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@workflowExecutionLogRecordsCount AS VARCHAR) + ' rows in WorkflowExecutionLogRecords';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in WorkflowExecutionLogRecords';
        END
    END
    ELSE
    BEGIN
        PRINT '  WorkflowExecutionLogRecords table not found - skipping';
    END
    
    -- WorkflowInboxMessages
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WorkflowInboxMessages')
    BEGIN
        DECLARE @workflowInboxMessagesCount INT;
        SELECT @workflowInboxMessagesCount = COUNT(*) FROM [WorkflowInboxMessages] WHERE [TenantId] IS NULL;
        
        IF @workflowInboxMessagesCount > 0
        BEGIN
            UPDATE [WorkflowInboxMessages] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@workflowInboxMessagesCount AS VARCHAR) + ' rows in WorkflowInboxMessages';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in WorkflowInboxMessages';
        END
    END
    ELSE
    BEGIN
        PRINT '  WorkflowInboxMessages table not found - skipping';
    END
    
    -- KeyValuePairs (SerializedKeyValuePair)
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KeyValuePairs')
    BEGIN
        DECLARE @keyValuePairsCount INT;
        SELECT @keyValuePairsCount = COUNT(*) FROM [KeyValuePairs] WHERE [TenantId] IS NULL;
        
        IF @keyValuePairsCount > 0
        BEGIN
            UPDATE [KeyValuePairs] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@keyValuePairsCount AS VARCHAR) + ' rows in KeyValuePairs';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in KeyValuePairs';
        END
    END
    ELSE
    BEGIN
        PRINT '  KeyValuePairs table not found - skipping';
    END
    
    PRINT '';
    
    -- =============================================
    -- Identity Context
    -- =============================================
    PRINT 'Migrating Identity Context tables...';
    
    -- Users
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Users')
    BEGIN
        DECLARE @usersCount INT;
        SELECT @usersCount = COUNT(*) FROM [Users] WHERE [TenantId] IS NULL;
        
        IF @usersCount > 0
        BEGIN
            UPDATE [Users] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@usersCount AS VARCHAR) + ' rows in Users';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in Users';
        END
    END
    ELSE
    BEGIN
        PRINT '  Users table not found - skipping';
    END
    
    -- Applications
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Applications')
    BEGIN
        DECLARE @applicationsCount INT;
        SELECT @applicationsCount = COUNT(*) FROM [Applications] WHERE [TenantId] IS NULL;
        
        IF @applicationsCount > 0
        BEGIN
            UPDATE [Applications] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@applicationsCount AS VARCHAR) + ' rows in Applications';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in Applications';
        END
    END
    ELSE
    BEGIN
        PRINT '  Applications table not found - skipping';
    END
    
    -- Roles
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Roles')
    BEGIN
        DECLARE @rolesCount INT;
        SELECT @rolesCount = COUNT(*) FROM [Roles] WHERE [TenantId] IS NULL;
        
        IF @rolesCount > 0
        BEGIN
            UPDATE [Roles] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@rolesCount AS VARCHAR) + ' rows in Roles';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in Roles';
        END
    END
    ELSE
    BEGIN
        PRINT '  Roles table not found - skipping';
    END
    
    PRINT '';
    
    -- =============================================
    -- Labels Context
    -- =============================================
    PRINT 'Migrating Labels Context tables...';
    
    -- Labels
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Labels')
    BEGIN
        DECLARE @labelsCount INT;
        SELECT @labelsCount = COUNT(*) FROM [Labels] WHERE [TenantId] IS NULL;
        
        IF @labelsCount > 0
        BEGIN
            UPDATE [Labels] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@labelsCount AS VARCHAR) + ' rows in Labels';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in Labels';
        END
    END
    ELSE
    BEGIN
        PRINT '  Labels table not found - skipping';
    END
    
    -- WorkflowDefinitionLabels
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'WorkflowDefinitionLabels')
    BEGIN
        DECLARE @workflowDefinitionLabelsCount INT;
        SELECT @workflowDefinitionLabelsCount = COUNT(*) FROM [WorkflowDefinitionLabels] WHERE [TenantId] IS NULL;
        
        IF @workflowDefinitionLabelsCount > 0
        BEGIN
            UPDATE [WorkflowDefinitionLabels] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@workflowDefinitionLabelsCount AS VARCHAR) + ' rows in WorkflowDefinitionLabels';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in WorkflowDefinitionLabels';
        END
    END
    ELSE
    BEGIN
        PRINT '  WorkflowDefinitionLabels table not found - skipping';
    END
    
    PRINT '';
    
    -- =============================================
    -- Alterations Context
    -- =============================================
    PRINT 'Migrating Alterations Context tables...';
    
    -- AlterationPlans
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AlterationPlans')
    BEGIN
        DECLARE @alterationPlansCount INT;
        SELECT @alterationPlansCount = COUNT(*) FROM [AlterationPlans] WHERE [TenantId] IS NULL;
        
        IF @alterationPlansCount > 0
        BEGIN
            UPDATE [AlterationPlans] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@alterationPlansCount AS VARCHAR) + ' rows in AlterationPlans';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in AlterationPlans';
        END
    END
    ELSE
    BEGIN
        PRINT '  AlterationPlans table not found - skipping';
    END
    
    -- AlterationJobs
    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AlterationJobs')
    BEGIN
        DECLARE @alterationJobsCount INT;
        SELECT @alterationJobsCount = COUNT(*) FROM [AlterationJobs] WHERE [TenantId] IS NULL;
        
        IF @alterationJobsCount > 0
        BEGIN
            UPDATE [AlterationJobs] 
            SET [TenantId] = '' 
            WHERE [TenantId] IS NULL;
            PRINT '  Updated ' + CAST(@alterationJobsCount AS VARCHAR) + ' rows in AlterationJobs';
        END
        ELSE
        BEGIN
            PRINT '  No rows to update in AlterationJobs';
        END
    END
    ELSE
    BEGIN
        PRINT '  AlterationJobs table not found - skipping';
    END
    
    PRINT '';
    PRINT 'Migration completed successfully!';
    PRINT '';
    PRINT 'IMPORTANT: Review the changes above. If everything looks correct, type COMMIT to save.';
    PRINT 'If you want to undo these changes, type ROLLBACK.';
    
    -- Commit the transaction
    COMMIT TRANSACTION;
    PRINT 'Changes committed successfully!';
    
END TRY
BEGIN CATCH
    -- Rollback on error
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END
    
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    
    PRINT '';
    PRINT 'ERROR: Migration failed and was rolled back!';
    PRINT 'Error message: ' + @ErrorMessage;
    
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
