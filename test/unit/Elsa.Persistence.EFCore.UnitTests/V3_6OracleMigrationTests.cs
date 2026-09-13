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

    [Test]
    [Arguments("Elsa", MigrationsSqlGenerationOptions.Default)]
    [Arguments("Elsa", MigrationsSqlGenerationOptions.Idempotent)]
    [Arguments("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public async Task ManagementUp_ConvertsStringDataToJsonWithoutInPlaceAlter(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateManagementScript(ManagementV3_4, ManagementV3_6, schema, options);

        await AssertColumnConverted(script, schema, "WorkflowDefinitions", "StringData", "JSON", "JSON(TO_CLOB(\"StringData\"))", notNull: false);
    }

    [Test]
    [Arguments("Elsa", MigrationsSqlGenerationOptions.Default)]
    [Arguments("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public async Task ManagementDown_ConvertsStringDataBackToNclobWithoutInPlaceAlter(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateManagementScript(ManagementV3_6, ManagementV3_4, schema, options);

        await AssertColumnConverted(script, schema, "WorkflowDefinitions", "StringData", "NCLOB", "TO_NCLOB(JSON_SERIALIZE(\"StringData\" RETURNING CLOB))", notNull: false);
    }

    [Test]
    [Arguments("Elsa", MigrationsSqlGenerationOptions.Default)]
    [Arguments("Elsa", MigrationsSqlGenerationOptions.Idempotent)]
    [Arguments("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public async Task RuntimeUp_ConvertsActivityNodeIdToNclobWithoutInPlaceAlter(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateRuntimeScript(RuntimeV3_5, RuntimeV3_6, schema, options);

        foreach (var table in new[] { "WorkflowExecutionLogRecords", "ActivityExecutionRecords" })
        {
            await AssertColumnConverted(script, schema, table, "ActivityNodeId", "NCLOB", "TO_NCLOB(\"ActivityNodeId\")", notNull: true);
        }
    }

    [Test]
    [Arguments("Elsa", MigrationsSqlGenerationOptions.Default)]
    [Arguments("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public async Task RuntimeDown_ConvertsActivityNodeIdBackToNvarchar2WithoutInPlaceAlter(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateRuntimeScript(RuntimeV3_6, RuntimeV3_5, schema, options);

        foreach (var table in new[] { "WorkflowExecutionLogRecords", "ActivityExecutionRecords" })
        {
            await AssertColumnConverted(script, schema, table, "ActivityNodeId", "NVARCHAR2(450)", "DBMS_LOB.SUBSTR(\"ActivityNodeId\", 450, 1)", notNull: true);
        }
    }

    // Oracle commits DDL implicitly, so the OriginalSource column the migration adds right before the alteration that
    // used to fail stays behind. Re-running has to skip it instead of failing with ORA-01430.
    [Test]
    [Arguments("Elsa")]
    [Arguments("custom_schema")]
    public async Task ManagementUp_AddsOriginalSourceOnlyWhenItIsMissing(string schema)
    {
        var script = GenerateManagementScript(ManagementV3_4, ManagementV3_6, schema, MigrationsSqlGenerationOptions.Default);

        await AssertColumnLookup(script, schema, "WorkflowDefinitions", "COLUMN_NAME = 'OriginalSource'");
        await AssertStatementAt(script, $"ALTER TABLE {Qualify(schema, "WorkflowDefinitions")} ADD (\"OriginalSource\" NCLOB)");
    }

    [Test]
    [Arguments("Elsa")]
    [Arguments("custom_schema")]
    public async Task ManagementDown_DropsOriginalSourceOnlyWhenItIsPresent(string schema)
    {
        var script = GenerateManagementScript(ManagementV3_6, ManagementV3_4, schema, MigrationsSqlGenerationOptions.Default);

        await AssertColumnLookup(script, schema, "WorkflowDefinitions", "COLUMN_NAME = 'OriginalSource'");
        await AssertStatementAt(script, $"ALTER TABLE {Qualify(schema, "WorkflowDefinitions")} DROP COLUMN \"OriginalSource\"");
    }

    // The unique trigger index is created before the alteration that used to fail, so a re-run would find it already
    // there. A left-behind index is guarded against by an ALL_INDEXES lookup rather than by tolerating ORA-00955,
    // because swallowing that error would also mask a different object already holding the name. The dropped
    // indexes are guarded by tolerating ORA-01418 instead, since that error is specific to a missing index.
    [Test]
    [Arguments("Elsa")]
    [Arguments("custom_schema")]
    public async Task RuntimeUp_ToleratesIndexesLeftBehindByAPartiallyAppliedRun(string schema)
    {
        var script = GenerateRuntimeScript(RuntimeV3_5, RuntimeV3_6, schema, MigrationsSqlGenerationOptions.Default);

        await AssertIndexCreationGuardedByExistenceCheck(script, schema, "IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId", "Triggers", new[] { "WorkflowDefinitionId", "Hash", "ActivityId", "TenantId" }, unique: true);
        await AssertToleratesOracleError(script, $"DROP INDEX \"{schema}\".\"IX_WorkflowExecutionLogRecord_ActivityNodeId\"", -1418);
        await AssertToleratesOracleError(script, $"DROP INDEX \"{schema}\".\"IX_ActivityExecutionRecord_ActivityNodeId\"", -1418);
    }

    // A valid quoted Oracle schema can contain an apostrophe (for example "O'Brien"). OWNER = '...' comparisons must
    // double it to stay inside their single-quoted literal, and "..." identifiers embedded inside an EXECUTE
    // IMMEDIATE body - itself a single-quoted literal - must have their apostrophe doubled a second time.
    [Test]
    public async Task RuntimeUp_EscapesSchemaNameContainingAnApostrophe()
    {
        const string schema = "O'Brien";

        var script = GenerateRuntimeScript(RuntimeV3_5, RuntimeV3_6, schema, MigrationsSqlGenerationOptions.Default);

        await Assert.That(script).Contains("OWNER = 'O''Brien'");
        await Assert.That(script).Contains("ON \"O''Brien\".\"Triggers\"");

        // The invalid, unescaped form must never appear.
        await Assert.That(script).DoesNotContain("'O'Brien'");
    }

    [Test]
    [Arguments("Elsa")]
    [Arguments("custom_schema")]
    public async Task RuntimeDown_ToleratesIndexesLeftBehindByAPartiallyAppliedRun(string schema)
    {
        var script = GenerateRuntimeScript(RuntimeV3_6, RuntimeV3_5, schema, MigrationsSqlGenerationOptions.Default);

        await AssertToleratesOracleError(script, $"DROP INDEX \"{schema}\".\"IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId\"", -1418);
        await AssertIndexCreationGuardedByExistenceCheck(script, schema, "IX_WorkflowExecutionLogRecord_ActivityNodeId", "WorkflowExecutionLogRecords", new[] { "ActivityNodeId" }, unique: false);
        await AssertIndexCreationGuardedByExistenceCheck(script, schema, "IX_ActivityExecutionRecord_ActivityNodeId", "ActivityExecutionRecords", new[] { "ActivityNodeId" }, unique: false);
    }

    // The upgraded NCLOB column permits values longer than the NVARCHAR2(450) the downgrade converts back to.
    // Copying such a value would silently truncate it, so both tables are preflighted for oversized values -
    // while their columns are still LOBs - before any statement of the downgrade runs, and the downgrade is
    // refused before any data is copied or any DDL is committed.
    [Test]
    [Arguments("Elsa")]
    [Arguments("custom_schema")]
    public async Task RuntimeDown_RefusesToTruncateOversizedActivityNodeIdBeforeConverting(string schema)
    {
        var script = GenerateRuntimeScript(RuntimeV3_6, RuntimeV3_5, schema, MigrationsSqlGenerationOptions.Default);

        var firstDowngradeStatement = await AssertStatementAt(script, $"DROP INDEX \"{schema}\".\"IX_StoredTrigger_Unique_WorkflowDefinitionId_Hash_ActivityId_TenantId\"");

        foreach (var qualifiedTable in new[] { "WorkflowExecutionLogRecords", "ActivityExecutionRecords" }.Select(table => Qualify(schema, table)))
        {
            var lengthCheck = await AssertStatementAt(script, $"SELECT COUNT(*) FROM {qualifiedTable} WHERE DBMS_LOB.GETLENGTH(\"ActivityNodeId\") > 450");

            await Assert.That(lengthCheck < firstDowngradeStatement).IsTrue().Because($"Expected the length check for \"ActivityNodeId\" on {qualifiedTable} to run before the first statement of the downgrade, but found the check at {lengthCheck} and the first downgrade statement at {firstDowngradeStatement}:\n{script}");
        }

        await Assert.That(script).Contains("RAISE_APPLICATION_ERROR(-20004,");
    }

    /// <summary>
    /// Pins the direction that would silently look fine: a re-run against an already-converted column must not copy
    /// out of and then drop that column, so the conversion is skipped when the datatype is already the target one and
    /// refused outright when it is neither the source nor the target type.
    /// </summary>
    [Test]
    public async Task ConversionIsSkippedWhenAlreadyApplied()
    {
        var script = GenerateManagementScript(ManagementV3_4, ManagementV3_6, "Elsa", MigrationsSqlGenerationOptions.Default);

        await Assert.That(script).Contains("IF l_temp_type IS NULL AND l_source_type = 'JSON' THEN");
        await Assert.That(script).Contains("l_already_converted := TRUE;");
        await Assert.That(script).Contains("IF l_source_type IS NOT NULL AND l_source_type != 'NCLOB' THEN");
        await Assert.That(script).Contains("RAISE_APPLICATION_ERROR(-20002,");
    }

    private static async Task AssertColumnConverted(string script, string schema, string table, string column, string toColumnDefinition, string expectedCopyExpression, bool notNull)
    {
        var qualifiedTable = Qualify(schema, table);
        var tempColumn = $"{column}_New";

        // This is the statement Oracle rejects with ORA-22858 / ORA-22859 and the reason the migration could not apply.
        await Assert.That(script).DoesNotContain($"MODIFY \"{column}\"");

        var add = await AssertStatementAt(script, $"ALTER TABLE {qualifiedTable} ADD (\"{tempColumn}\" {toColumnDefinition})");

        // The copy is unconditional and NULL-preserving, so a retry after a partially committed copy reproduces the
        // current source exactly instead of leaving a stale converted value behind for a row whose source has since
        // become NULL.
        var copyStatement = $"UPDATE {qualifiedTable} SET \"{tempColumn}\" = CASE WHEN \"{column}\" IS NULL THEN NULL ELSE {expectedCopyExpression} END";
        var copy = await AssertStatementAt(script, copyStatement);
        await Assert.That(script).DoesNotContain($"{copyStatement} WHERE");

        var drop = await AssertStatementAt(script, $"ALTER TABLE {qualifiedTable} DROP COLUMN \"{column}\"");
        var rename = await AssertStatementAt(script, $"ALTER TABLE {qualifiedTable} RENAME COLUMN \"{tempColumn}\" TO \"{column}\"");

        await Assert.That(add < copy && copy < drop && drop < rename).IsTrue().Because($"Expected the add/copy/drop/rename sequence for \"{column}\" on {qualifiedTable} in that order, but found them at {add}, {copy}, {drop} and {rename}:\n{script}");

        // Both columns are looked up through the block's local function, which takes the column name as a parameter,
        // so the guard that makes the block re-runnable reads COLUMN_NAME = p_column rather than a literal.
        await AssertColumnLookup(script, schema, table, "COLUMN_NAME = p_column");

        var setNotNull = $"ALTER TABLE {qualifiedTable} MODIFY (\"{column}\" NOT NULL)";

        if (!notNull)
        {
            await Assert.That(script).DoesNotContain(setNotNull);
            return;
        }

        // The converted column starts out nullable because the copy needs it to; the constraint therefore has to land
        // after the rename, and only when the column is not already constrained.
        var constrain = await AssertStatementAt(script, setNotNull);
        await Assert.That(rename < constrain).IsTrue().Because($"Expected \"{column}\" on {qualifiedTable} to be constrained to NOT NULL after the rename, but the constraint appears at {constrain} and the rename at {rename}:\n{script}");
        await Assert.That(script).Contains("IF l_nullable = 'Y' THEN");
    }

    private static async Task AssertColumnLookup(string script, string schema, string table, string columnPredicate)
    {
        await Assert.That(script).Contains($"FROM ALL_TAB_COLUMNS WHERE OWNER = '{schema}' AND TABLE_NAME = '{table}' AND {columnPredicate}");
    }

    private static async Task AssertToleratesOracleError(string script, string statementPrefix, int sqlCode)
    {
        var statement = await AssertStatementAt(script, statementPrefix);
        var guard = script.IndexOf($"IF SQLCODE != {sqlCode} THEN RAISE; END IF;", statement, StringComparison.Ordinal);

        await Assert.That(guard >= 0).IsTrue().Because($"Expected '{statementPrefix}' to be followed by a handler that swallows ORA{sqlCode}:\n{script}");
    }

    // Index creation is guarded by an ALL_INDEXES lookup rather than by tolerating ORA-00955, because swallowing
    // that error would also mask a different object already holding the name. When an index with that name already
    // exists, its table, uniqueness and ordered column list must also be validated against ALL_IND_COLUMNS via
    // LISTAGG, so that a same-named index left behind by schema drift or manual recovery is not mistaken for a
    // completed run.
    private static async Task AssertIndexCreationGuardedByExistenceCheck(string script, string schema, string indexName, string table, string[] columns, bool unique)
    {
        await Assert.That(script).Contains($"SELECT COUNT(*) INTO l_count FROM ALL_INDEXES WHERE OWNER = '{schema}' AND INDEX_NAME = '{indexName}'");

        var lookup = await AssertStatementAt(script, $"FROM ALL_INDEXES WHERE OWNER = '{schema}' AND INDEX_NAME = '{indexName}'");
        var guard = script.IndexOf("IF l_count = 0 THEN", lookup, StringComparison.Ordinal);

        await Assert.That(guard >= 0).IsTrue().Because($"Expected the lookup for \"{indexName}\" to be followed by an 'IF l_count = 0 THEN' guard:\n{script}");

        var createStatementPrefix = $"CREATE {(unique ? "UNIQUE " : "")}INDEX \"{schema}\".\"{indexName}\"";
        var create = await AssertStatementAt(script, createStatementPrefix);

        await Assert.That(create > guard).IsTrue().Because($"Expected '{createStatementPrefix}' to appear inside the 'IF l_count = 0 THEN' guard for \"{indexName}\":\n{script}");

        var expectedColumns = string.Join(",", columns);
        var expectedUniqueness = unique ? "UNIQUE" : "NONUNIQUE";

        await Assert.That(script).Contains($"SELECT LISTAGG(COLUMN_NAME, ',') WITHIN GROUP (ORDER BY COLUMN_POSITION) INTO l_columns FROM ALL_IND_COLUMNS WHERE INDEX_OWNER = '{schema}' AND INDEX_NAME = '{indexName}'");
        await Assert.That(script).Contains($"l_table_name != '{table}' OR l_uniqueness != '{expectedUniqueness}' OR l_columns != '{expectedColumns}'");
    }

    private static async Task<int> AssertStatementAt(string script, string statement)
    {
        var index = script.IndexOf(statement, StringComparison.Ordinal);

        await Assert.That(index >= 0).IsTrue().Because($"Expected the generated script to contain '{statement}':\n{script}");

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
