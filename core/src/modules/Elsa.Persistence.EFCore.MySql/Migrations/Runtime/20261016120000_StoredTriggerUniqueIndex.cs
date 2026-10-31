using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Elsa.Persistence.EFCore.MySql.Migrations.Runtime;

/// <inheritdoc />
/// <remarks>
/// <para>
/// Aligns the <c>Triggers</c> unique index with the MySQL model: four columns including
/// <c>TenantId</c>, each with a 191-character prefix so the key fits InnoDB's 3,072-byte limit.
/// Uniqueness is enforced on those prefixes, not the full 255-character values.
/// </para>
/// <para>
/// Fresh databases get that index from <c>V3_6</c>. Databases that already recorded <c>V3_6</c>
/// may still have the unprefixed four-column name (truncated to 64 characters) or the original
/// three-column name. This repair drops the four-column name only when a column prefix is not 191,
/// creates the prefixed four-column index if it is missing, then drops the three-column index.
/// The three-column and four-column names differ, so the replacement can exist before the old
/// index is removed. A mismatched four-column index must be dropped first because it reuses the
/// same 64-character name.
/// </para>
/// <para>
/// <c>Down()</c> is a no-op: the prefixed four-column index is the current model, and restoring
/// either historical form would either exceed the key-length limit or drop tenant isolation.
/// </para>
/// </remarks>
public partial class StoredTriggerUniqueIndex : Migration
{
    /// <inheritdoc />
    public StoredTriggerUniqueIndex(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);
    }

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        DropFourColumnIndexIfPrefixMismatch(migrationBuilder);
        CreatePrefixedUniqueIndexIfMissing(migrationBuilder);
        DropIndexIfPresent(migrationBuilder, StoredTriggerMySqlUniqueIndex.ThreeColumnName);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }

    private static void DropIndexIfPresent(MigrationBuilder migrationBuilder, string indexName)
    {
        migrationBuilder.Sql($"""
            SET @exist := (SELECT COUNT(*) FROM information_schema.statistics
                WHERE table_schema = DATABASE() AND table_name = 'Triggers'
                AND index_name = '{indexName}');
            SET @sqlstmt := IF(@exist > 0, 'DROP INDEX `{indexName}` ON `Triggers`', 'SELECT 1');
            PREPARE stmt FROM @sqlstmt;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
            """);
    }

    private static void DropFourColumnIndexIfPrefixMismatch(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            SET @exist := (SELECT COUNT(*) FROM information_schema.statistics
                WHERE table_schema = DATABASE() AND table_name = 'Triggers'
                AND index_name = '{StoredTriggerMySqlUniqueIndex.Name}'
                AND COALESCE(SUB_PART, 0) <> {StoredTriggerMySqlUniqueIndex.PrefixLength});
            SET @sqlstmt := IF(@exist > 0, 'DROP INDEX `{StoredTriggerMySqlUniqueIndex.Name}` ON `Triggers`', 'SELECT 1');
            PREPARE stmt FROM @sqlstmt;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
            """);
    }

    private static void CreatePrefixedUniqueIndexIfMissing(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($"""
            SET @exist := (SELECT COUNT(*) FROM information_schema.statistics
                WHERE table_schema = DATABASE() AND table_name = 'Triggers'
                AND index_name = '{StoredTriggerMySqlUniqueIndex.Name}');
            SET @sqlstmt := IF(@exist = 0, 'CREATE UNIQUE INDEX `{StoredTriggerMySqlUniqueIndex.Name}` ON `Triggers` (`WorkflowDefinitionId`({StoredTriggerMySqlUniqueIndex.PrefixLength}), `Hash`({StoredTriggerMySqlUniqueIndex.PrefixLength}), `ActivityId`({StoredTriggerMySqlUniqueIndex.PrefixLength}), `TenantId`({StoredTriggerMySqlUniqueIndex.PrefixLength}))', 'SELECT 1');
            PREPARE stmt FROM @sqlstmt;
            EXECUTE stmt;
            DEALLOCATE PREPARE stmt;
            """);
    }
}
