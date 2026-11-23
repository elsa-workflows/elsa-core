using System.Data;
using System.Globalization;
using FluentMigrator;

namespace Elsa.Persistence.Dapper.Migrations.Management;

/// <summary>
/// Enforces one workflow definition version per tenant, treating both the legacy NULL
/// and the empty tenant ID as the default tenant.
/// </summary>
// Keep the same migration version on 3.10 and 3.11 so upgrades apply the index once.
[Migration(10006, "Elsa:Management:WorkflowDefinitionVersion")]
public sealed class V3_10_WorkflowDefinitionVersion : Migration
{
    private const string TableName = "WorkflowDefinitions";
    private const string UniqueIndex = "UX_WorkflowDef_Tenant_Version";
    private const string SqlServerTenantKey = "DefinitionTenantKey";

    /// <inheritdoc />
    public override void Up()
    {
        if (Schema.Table(TableName).Index(UniqueIndex).Exists())
            return;

        const string quotedTable = "\"WorkflowDefinitions\"";
        const string quotedTenant = "COALESCE(\"TenantId\", '')";
        const string oracleTenant = "\"TenantId\"";
        const string sqlServerTenant = "ISNULL([TenantId], N'')";
        const string mySqlTenant = "COALESCE(`TenantId`, '')";

        IfDatabase("Sqlite", "Postgres", "PostgreSQL", "PostgreSQL92").Execute.WithConnection((connection, transaction) =>
            EnsureUniqueVersions(connection, transaction, quotedTable, quotedTenant, "\"DefinitionId\"", "\"Version\""));
        IfDatabase("SqlServer").Execute.WithConnection((connection, transaction) =>
            EnsureUniqueVersions(connection, transaction, "[WorkflowDefinitions]", sqlServerTenant, "[DefinitionId]", "[Version]"));
        IfDatabase("MySql").Execute.WithConnection((connection, transaction) =>
            EnsureUniqueVersions(connection, transaction, "`WorkflowDefinitions`", mySqlTenant, "`DefinitionId`", "`Version`"));
        IfDatabase("Oracle").Execute.WithConnection((connection, transaction) =>
            EnsureUniqueVersions(connection, transaction, quotedTable, oracleTenant, "\"DefinitionId\"", "\"Version\""));

        // Normalize the indexed key instead of changing existing tenant IDs. This also covers
        // inserts made without an ambient tenant, which still write NULL.
        IfDatabase("Sqlite", "Postgres", "PostgreSQL", "PostgreSQL92").Execute.Sql(
            $"CREATE UNIQUE INDEX \"{UniqueIndex}\" ON {quotedTable} ({quotedTenant}, \"DefinitionId\", \"Version\")");
        IfDatabase("MySql").Execute.Sql(
            $"CREATE UNIQUE INDEX `{UniqueIndex}` ON `WorkflowDefinitions` (({mySqlTenant}), `DefinitionId`, `Version`)");

        // SQL Server indexes a computed column in place of a direct expression.
        if (!Schema.Table(TableName).Column(SqlServerTenantKey).Exists())
            IfDatabase("SqlServer").Execute.Sql(
                $"ALTER TABLE [WorkflowDefinitions] ADD [{SqlServerTenantKey}] AS {sqlServerTenant} PERSISTED");
        IfDatabase("SqlServer").Execute.Sql(
            $"CREATE UNIQUE INDEX [{UniqueIndex}] ON [WorkflowDefinitions] ([{SqlServerTenantKey}], [DefinitionId], [Version])");

        // Oracle stores empty strings as NULL and compares composite keys by the non-null columns.
        IfDatabase("Oracle").Create.Index(UniqueIndex).OnTable(TableName)
            .OnColumn("TenantId").Ascending()
            .OnColumn("DefinitionId").Ascending()
            .OnColumn("Version").Ascending()
            .WithOptions().Unique();
    }

    /// <inheritdoc />
    public override void Down()
    {
        if (Schema.Table(TableName).Index(UniqueIndex).Exists())
            Delete.Index(UniqueIndex).OnTable(TableName);
        if (Schema.Table(TableName).Column(SqlServerTenantKey).Exists())
            IfDatabase("SqlServer").Delete.Column(SqlServerTenantKey).FromTable(TableName);
    }

    private static void EnsureUniqueVersions(
        IDbConnection connection,
        IDbTransaction? transaction,
        string table,
        string normalizedTenant,
        string definitionId,
        string version)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            SELECT {normalizedTenant}, {definitionId}, {version}, COUNT(*)
            FROM {table}
            GROUP BY {normalizedTenant}, {definitionId}, {version}
            HAVING COUNT(*) > 1
            """;
        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return;

        var tenant = reader.IsDBNull(0) ? null : reader.GetString(0);
        var tenantLabel = string.IsNullOrEmpty(tenant) ? "(default)" : $"'{tenant}'";
        var definition = reader.GetString(1);
        var duplicateVersion = Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture);
        var count = Convert.ToInt64(reader.GetValue(3), CultureInfo.InvariantCulture);
        throw new InvalidOperationException(
            $"Cannot create {UniqueIndex} on {TableName}. Tenant {tenantLabel} has {count} rows for workflow definition '{definition}' at version {duplicateVersion}. " +
            "No workflow definition rows were changed. Resolve the duplicate versions and run the migration again.");
    }
}
