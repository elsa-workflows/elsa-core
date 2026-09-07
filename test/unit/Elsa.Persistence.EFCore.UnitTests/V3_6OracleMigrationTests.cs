using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// Regression tests for the Oracle V3_6 migrations. Oracle refuses to change a column's datatype to or from a LOB type
/// in place: <c>ALTER TABLE ... MODIFY</c> fails with ORA-22858 when the target type is a LOB and with ORA-22859 when
/// the source type is. Both V3_6 migrations used to be generated as such an in-place alteration - NCLOB to JSON for
/// <c>WorkflowDefinitions.StringData</c>, NVARCHAR2(450) to NCLOB for <c>ActivityNodeId</c> - so neither could ever
/// apply. They must instead add a new column, copy the values across, drop the original and rename the new one.
/// </summary>
/// <remarks>
/// The scripts are generated offline through <see cref="IMigrator.GenerateScript"/>; no Oracle connection is opened.
/// </remarks>
public class V3_6OracleMigrationTests
{
    private const string ManagementV3_4 = "20250222190910_V3_4";
    private const string ManagementV3_6 = "20251116182825_V3_6";
    private const string RuntimeV3_5 = "20250530105102_V3_5";
    private const string RuntimeV3_6 = "20251204150355_V3_6";

    [Theory]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Default)]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Idempotent)]
    [InlineData("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public void ManagementUp_ConvertsStringDataToJsonWithoutInPlaceAlter(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateManagementScript(ManagementV3_4, ManagementV3_6, schema, options);

        AssertColumnConverted(script, schema, "WorkflowDefinitions", "StringData", "JSON", "JSON(TO_CLOB(\"StringData\"))", notNull: false);
    }

    [Theory]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Default)]
    [InlineData("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public void ManagementDown_ConvertsStringDataBackToNclobWithoutInPlaceAlter(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateManagementScript(ManagementV3_6, ManagementV3_4, schema, options);

        AssertColumnConverted(script, schema, "WorkflowDefinitions", "StringData", "NCLOB", "TO_NCLOB(JSON_SERIALIZE(\"StringData\" RETURNING CLOB))", notNull: false);
    }

    [Theory]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Default)]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Idempotent)]
    [InlineData("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public void RuntimeUp_ConvertsActivityNodeIdToNclobWithoutInPlaceAlter(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateRuntimeScript(RuntimeV3_5, RuntimeV3_6, schema, options);

        foreach (var table in new[] { "WorkflowExecutionLogRecords", "ActivityExecutionRecords" })
            AssertColumnConverted(script, schema, table, "ActivityNodeId", "NCLOB", "TO_NCLOB(\"ActivityNodeId\")", notNull: true);
    }

    [Theory]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Default)]
    [InlineData("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public void RuntimeDown_ConvertsActivityNodeIdBackToNvarchar2WithoutInPlaceAlter(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateRuntimeScript(RuntimeV3_6, RuntimeV3_5, schema, options);

        foreach (var table in new[] { "WorkflowExecutionLogRecords", "ActivityExecutionRecords" })
            AssertColumnConverted(script, schema, table, "ActivityNodeId", "NVARCHAR2(450)", "DBMS_LOB.SUBSTR(\"ActivityNodeId\", 450, 1)", notNull: true);
    }

    // Oracle commits DDL implicitly, so the OriginalSource column the migration adds right before the alteration that
    // used to fail stays behind. Re-running has to skip it instead of failing with ORA-01430.
    [Theory]
    [InlineData("Elsa")]
    [InlineData("custom_schema")]
    public void ManagementUp_AddsOriginalSourceOnlyWhenItIsMissing(string schema)
    {
        var script = GenerateManagementScript(ManagementV3_4, ManagementV3_6, schema, MigrationsSqlGenerationOptions.Default);

        AssertColumnLookup(script, schema, "WorkflowDefinitions", "COLUMN_NAME = 'OriginalSource'");
        AssertStatementAt(script, $"ALTER TABLE {Qualify(schema, "WorkflowDefinitions")} ADD (\"OriginalSource\" NCLOB)");
    }

    [Theory]
    [InlineData("Elsa")]
    [InlineData("custom_schema")]
    public void ManagementDown_DropsOriginalSourceOnlyWhenItIsPresent(string schema)
    {
        var script = GenerateManagementScript(ManagementV3_6, ManagementV3_4, schema, MigrationsSqlGenerationOptions.Default);

        AssertColumnLookup(script, schema, "WorkflowDefinitions", "COLUMN_NAME = 'OriginalSource'");
        AssertStatementAt(script, $"ALTER TABLE {Qualify(schema, "WorkflowDefinitions")} DROP COLUMN \"OriginalSource\"");
    }

    // The unique trigger index is created before the alteration that used to fail, so a re-run would find it already
    // there. A left-behind index is guarded against by an ALL_INDEXES lookup rather than by tolerating ORA-00955,
    // because swallowing that error would also mask a different object already holding the name. The dropped
    // indexes are guarded by tolerating ORA-01418 instead, since that error is specific to a missing index.
    [Theory]
    [InlineData("Elsa")]
    [InlineData("custom_schema")]
    public void RuntimeUp_ToleratesIndexesLeftBehindByAPartiallyAppliedRun(string schema)
    {
        var script = GenerateRuntimeScript(RuntimeV3_5, RuntimeV3_6, schema, MigrationsSqlGenerationOptions.Default);

        AssertIndexCreationGuardedByExistenceCheck(script, schema, "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId", $"CREATE UNIQUE INDEX \"{schema}\".\"IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId\"");
        AssertToleratesOracleError(script, $"DROP INDEX \"{schema}\".\"IX_WorkflowExecutionLogRecord_ActivityNodeId\"", -1418);
        AssertToleratesOracleError(script, $"DROP INDEX \"{schema}\".\"IX_ActivityExecutionRecord_ActivityNodeId\"", -1418);
    }

    [Theory]
    [InlineData("Elsa")]
    [InlineData("custom_schema")]
    public void RuntimeDown_ToleratesIndexesLeftBehindByAPartiallyAppliedRun(string schema)
    {
        var script = GenerateRuntimeScript(RuntimeV3_6, RuntimeV3_5, schema, MigrationsSqlGenerationOptions.Default);

        AssertToleratesOracleError(script, $"DROP INDEX \"{schema}\".\"IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId\"", -1418);
        AssertIndexCreationGuardedByExistenceCheck(script, schema, "IX_WorkflowExecutionLogRecord_ActivityNodeId", $"CREATE INDEX \"{schema}\".\"IX_WorkflowExecutionLogRecord_ActivityNodeId\"");
        AssertIndexCreationGuardedByExistenceCheck(script, schema, "IX_ActivityExecutionRecord_ActivityNodeId", $"CREATE INDEX \"{schema}\".\"IX_ActivityExecutionRecord_ActivityNodeId\"");
    }

    /// <summary>
    /// Pins the direction that would silently look fine: a re-run against an already-converted column must not copy
    /// out of and then drop that column, so the conversion is skipped when the datatype is already the target one and
    /// refused outright when it is neither the source nor the target type.
    /// </summary>
    [Fact]
    public void ConversionIsSkippedWhenAlreadyApplied()
    {
        var script = GenerateManagementScript(ManagementV3_4, ManagementV3_6, "Elsa", MigrationsSqlGenerationOptions.Default);

        Assert.Contains("IF l_temp_type IS NULL AND l_source_type = 'JSON' THEN", script, StringComparison.Ordinal);
        Assert.Contains("l_already_converted := TRUE;", script, StringComparison.Ordinal);
        Assert.Contains("IF l_source_type IS NOT NULL AND l_source_type != 'NCLOB' THEN", script, StringComparison.Ordinal);
        Assert.Contains("RAISE_APPLICATION_ERROR(-20002,", script, StringComparison.Ordinal);
    }

    private static void AssertColumnConverted(string script, string schema, string table, string column, string toColumnDefinition, string expectedCopyExpression, bool notNull)
    {
        var qualifiedTable = Qualify(schema, table);
        var tempColumn = $"{column}_New";

        // This is the statement Oracle rejects with ORA-22858 / ORA-22859 and the reason the migration could not apply.
        Assert.DoesNotContain($"MODIFY \"{column}\"", script, StringComparison.Ordinal);

        var add = AssertStatementAt(script, $"ALTER TABLE {qualifiedTable} ADD (\"{tempColumn}\" {toColumnDefinition})");
        var copy = AssertStatementAt(script, $"UPDATE {qualifiedTable} SET \"{tempColumn}\" = {expectedCopyExpression} WHERE \"{column}\" IS NOT NULL");
        var drop = AssertStatementAt(script, $"ALTER TABLE {qualifiedTable} DROP COLUMN \"{column}\"");
        var rename = AssertStatementAt(script, $"ALTER TABLE {qualifiedTable} RENAME COLUMN \"{tempColumn}\" TO \"{column}\"");

        Assert.True(add < copy && copy < drop && drop < rename, $"Expected the add/copy/drop/rename sequence for \"{column}\" on {qualifiedTable} in that order, but found them at {add}, {copy}, {drop} and {rename}:\n{script}");

        // Both columns are looked up through the block's local function, which takes the column name as a parameter,
        // so the guard that makes the block re-runnable reads COLUMN_NAME = p_column rather than a literal.
        AssertColumnLookup(script, schema, table, "COLUMN_NAME = p_column");

        var setNotNull = $"ALTER TABLE {qualifiedTable} MODIFY (\"{column}\" NOT NULL)";

        if (!notNull)
        {
            Assert.DoesNotContain(setNotNull, script, StringComparison.Ordinal);
            return;
        }

        // The converted column starts out nullable because the copy needs it to; the constraint therefore has to land
        // after the rename, and only when the column is not already constrained.
        var constrain = AssertStatementAt(script, setNotNull);
        Assert.True(rename < constrain, $"Expected \"{column}\" on {qualifiedTable} to be constrained to NOT NULL after the rename, but the constraint appears at {constrain} and the rename at {rename}:\n{script}");
        Assert.Contains("IF l_nullable = 'Y' THEN", script, StringComparison.Ordinal);
    }

    private static void AssertColumnLookup(string script, string schema, string table, string columnPredicate)
    {
        Assert.Contains($"FROM ALL_TAB_COLUMNS WHERE OWNER = '{schema}' AND TABLE_NAME = '{table}' AND {columnPredicate}", script, StringComparison.Ordinal);
    }

    private static void AssertToleratesOracleError(string script, string statementPrefix, int sqlCode)
    {
        var statement = AssertStatementAt(script, statementPrefix);
        var guard = script.IndexOf($"IF SQLCODE != {sqlCode} THEN RAISE; END IF;", statement, StringComparison.Ordinal);

        Assert.True(guard >= 0, $"Expected '{statementPrefix}' to be followed by a handler that swallows ORA{sqlCode}:\n{script}");
    }

    // Index creation is guarded by an ALL_INDEXES lookup rather than by tolerating ORA-00955, because swallowing
    // that error would also mask a different object already holding the name.
    private static void AssertIndexCreationGuardedByExistenceCheck(string script, string schema, string indexName, string createStatementPrefix)
    {
        Assert.Contains($"SELECT COUNT(*) INTO l_count FROM ALL_INDEXES WHERE OWNER = '{schema}' AND INDEX_NAME = '{indexName}'", script, StringComparison.Ordinal);

        var lookup = AssertStatementAt(script, $"FROM ALL_INDEXES WHERE OWNER = '{schema}' AND INDEX_NAME = '{indexName}'");
        var guard = script.IndexOf("IF l_count = 0 THEN", lookup, StringComparison.Ordinal);

        Assert.True(guard >= 0, $"Expected the lookup for \"{indexName}\" to be followed by an 'IF l_count = 0 THEN' guard:\n{script}");

        var create = AssertStatementAt(script, createStatementPrefix);

        Assert.True(create > guard, $"Expected '{createStatementPrefix}' to appear inside the 'IF l_count = 0 THEN' guard for \"{indexName}\":\n{script}");
    }

    private static int AssertStatementAt(string script, string statement)
    {
        var index = script.IndexOf(statement, StringComparison.Ordinal);

        Assert.True(index >= 0, $"Expected the generated script to contain '{statement}':\n{script}");

        return index;
    }

    private static string Qualify(string schema, string table) => $"\"{schema}\".\"{table}\"";

    private static string GenerateManagementScript(string fromMigration, string toMigration, string schema, MigrationsSqlGenerationOptions options) =>
        MigrationScriptGenerator.Generate<ManagementElsaDbContext>(
            builder => builder.UseElsaOracle(typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Management.V3_6).Assembly, "Data Source=unused", new ElsaDbContextOptions { SchemaName = schema }),
            fromMigration,
            toMigration,
            options);

    private static string GenerateRuntimeScript(string fromMigration, string toMigration, string schema, MigrationsSqlGenerationOptions options) =>
        MigrationScriptGenerator.Generate<RuntimeElsaDbContext>(
            builder => builder.UseElsaOracle(typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Runtime.V3_6).Assembly, "Data Source=unused", new ElsaDbContextOptions { SchemaName = schema }),
            fromMigration,
            toMigration,
            options);
}
