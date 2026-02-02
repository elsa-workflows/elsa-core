# Elsa Workflows 3.6.0 RC3 - TenantId Migration Scripts

This directory contains SQL migration scripts to handle breaking changes in multitenancy for Elsa Workflows 3.6.0 RC3.

## Background

Elsa Workflows 3.6.0 RC3 introduces important changes to how tenant IDs are handled in the database:

- **ADR-0008**: Empty String as Default Tenant ID - standardizes on using empty string (`""`) instead of `null` for the default tenant
- **ADR-0009**: Asterisk Sentinel Value for Tenant-Agnostic Entities - introduces `"*"` as a sentinel value for tenant-agnostic entities

## What These Scripts Do

These migration scripts convert **all existing `NULL` TenantId values to empty string (`""`)** across all Elsa database tables. This represents the default tenant.

### Important Notes

1. **NULL → Empty String (`""`)**: All NULL tenant IDs will be converted to empty strings, representing the default tenant
2. **NOT NULL → Asterisk (`*`)**: The scripts do NOT convert NULL to asterisk. The asterisk is only for explicitly tenant-agnostic entities and must be set intentionally by application code
3. **Safety**: All scripts use transactions and can be rolled back if needed
4. **Idempotent**: The scripts can be run multiple times safely - they only update rows where TenantId is NULL

## Affected Tables

The migration scripts update TenantId in the following tables across 5 database contexts:

### Management Context (2 tables)
- `WorkflowDefinitions`
- `WorkflowInstances`

### Runtime Context (7 tables)
- `Triggers`
- `Bookmarks`
- `BookmarkQueueItems`
- `ActivityExecutionRecords`
- `WorkflowExecutionLogRecords`
- `WorkflowInboxMessages`
- `KeyValuePairs`

### Identity Context (3 tables)
- `Users`
- `Applications`
- `Roles`

### Labels Context (2 tables)
- `Labels`
- `WorkflowDefinitionLabels`

### Alterations Context (2 tables)
- `AlterationPlans`
- `AlterationJobs`

**Total: 16 tables**

## Prerequisites

1. **Backup your database** before running any migration script
2. Ensure you have appropriate permissions to update the database
3. Test the scripts in a non-production environment first
4. Review the ADR documents in `/doc/adr/`:
   - `0008-empty-string-as-default-tenant-id.md`
   - `0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md`

## Usage

### SQL Server

```bash
sqlcmd -S <server> -d <database> -U <username> -P <password> -i migrate-tenantid-sqlserver.sql
```

Or execute within SQL Server Management Studio:
1. Open SQL Server Management Studio
2. Connect to your database
3. Open `migrate-tenantid-sqlserver.sql`
4. Execute the script

### PostgreSQL

```bash
psql -h <host> -U <username> -d <database> -f migrate-tenantid-postgresql.sql
```

Or execute within pgAdmin:
1. Open pgAdmin
2. Connect to your database
3. Open Query Tool
4. Load and execute `migrate-tenantid-postgresql.sql`

### MySQL

```bash
mysql -h <host> -u <username> -p <database> < migrate-tenantid-mysql.sql
```

Or execute within MySQL Workbench:
1. Open MySQL Workbench
2. Connect to your database
3. Open SQL Editor
4. Load and execute `migrate-tenantid-mysql.sql`

## What to Expect

When you run the scripts, you will see output indicating:

1. Which tables were found and migrated
2. How many rows were updated in each table
3. Which tables were skipped (if not present in your database)
4. Success or error messages

### Sample Output

```
Starting TenantId migration for Elsa 3.6.0 RC3...

Migrating Management Context tables...
  Updated 45 rows in WorkflowDefinitions
  Updated 123 rows in WorkflowInstances

Migrating Runtime Context tables...
  Updated 67 rows in Triggers
  Updated 234 rows in Bookmarks
  ...

Migration completed successfully!
Changes committed successfully!
```

## Rollback

All scripts use transactions. If something goes wrong:

- **SQL Server**: The script will automatically rollback on error
- **PostgreSQL**: Press Ctrl+C before the COMMIT or execute `ROLLBACK;`
- **MySQL**: Execute `ROLLBACK;` before the script completes

## Verification

After running the migration, verify the changes:

### SQL Server
```sql
-- Check for any remaining NULL values
SELECT 'WorkflowDefinitions' as TableName, COUNT(*) as NullCount 
FROM WorkflowDefinitions WHERE TenantId IS NULL
UNION ALL
SELECT 'WorkflowInstances', COUNT(*) 
FROM WorkflowInstances WHERE TenantId IS NULL
-- ... add other tables as needed

-- Check for empty string tenant IDs (should have rows after migration)
SELECT 'WorkflowDefinitions' as TableName, COUNT(*) as EmptyStringCount 
FROM WorkflowDefinitions WHERE TenantId = ''
UNION ALL
SELECT 'WorkflowInstances', COUNT(*) 
FROM WorkflowInstances WHERE TenantId = ''
-- ... add other tables as needed
```

### PostgreSQL
```sql
-- Check for any remaining NULL values
SELECT 'WorkflowDefinitions' as TableName, COUNT(*) as NullCount 
FROM "WorkflowDefinitions" WHERE "TenantId" IS NULL
UNION ALL
SELECT 'WorkflowInstances', COUNT(*) 
FROM "WorkflowInstances" WHERE "TenantId" IS NULL;
-- ... add other tables as needed

-- Check for empty string tenant IDs (should have rows after migration)
SELECT 'WorkflowDefinitions' as TableName, COUNT(*) as EmptyStringCount 
FROM "WorkflowDefinitions" WHERE "TenantId" = ''
UNION ALL
SELECT 'WorkflowInstances', COUNT(*) 
FROM "WorkflowInstances" WHERE "TenantId" = '';
-- ... add other tables as needed
```

### MySQL
```sql
-- Check for any remaining NULL values
SELECT 'WorkflowDefinitions' as TableName, COUNT(*) as NullCount 
FROM `WorkflowDefinitions` WHERE `TenantId` IS NULL
UNION ALL
SELECT 'WorkflowInstances', COUNT(*) 
FROM `WorkflowInstances` WHERE `TenantId` IS NULL;
-- ... add other tables as needed

-- Check for empty string tenant IDs (should have rows after migration)
SELECT 'WorkflowDefinitions' as TableName, COUNT(*) as EmptyStringCount 
FROM `WorkflowDefinitions` WHERE `TenantId` = ''
UNION ALL
SELECT 'WorkflowInstances', COUNT(*) 
FROM `WorkflowInstances` WHERE `TenantId` = '';
-- ... add other tables as needed
```

## Post-Migration

After successfully running the migration:

1. ✅ All NULL tenant IDs have been converted to empty strings (`""`)
2. ✅ Your existing workflows and data are now using the default tenant
3. ✅ The application will continue to work as before
4. ⚠️ If you need tenant-agnostic entities, you must explicitly set their TenantId to `"*"` in your application code

## Troubleshooting

### "Table not found" messages
This is normal. Not all Elsa installations use all modules. The scripts skip tables that don't exist.

### Permission denied errors
Ensure your database user has UPDATE permissions on the Elsa tables.

### Transaction timeout
If you have a very large database, you may need to increase transaction timeout settings for your database server.

### Script syntax errors
Ensure you're using the correct script for your database type:
- SQL Server → `migrate-tenantid-sqlserver.sql`
- PostgreSQL → `migrate-tenantid-postgresql.sql`
- MySQL → `migrate-tenantid-mysql.sql`

## Support

For questions or issues:
- GitHub Issues: https://github.com/elsa-workflows/elsa-core/issues
- GitHub Discussions: https://github.com/elsa-workflows/elsa-core/discussions
- Discord: https://discord.gg/hhChk5H472

## Related Documentation

- [ADR-0008: Empty String as Default Tenant ID](/doc/adr/0008-empty-string-as-default-tenant-id.md)
- [ADR-0009: Asterisk Sentinel Value for Tenant-Agnostic Entities](/doc/adr/0009-asterisk-sentinel-value-for-tenant-agnostic-entities.md)
