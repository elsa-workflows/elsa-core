using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// Regression tests for the Oracle <c>ActivityExecutionSerializedMetadataNclob</c> migration. V3_5 created
/// <c>ActivityExecutionRecords.SerializedMetadata</c> as NVARCHAR2(2000) and no later <c>Up()</c> widened it,
/// even though the runtime snapshot and <c>Configurations/Runtime.cs</c> already pin the column as NCLOB.
/// Oracle refuses an in-place datatype change to a LOB (ORA-22858), so the migration must add a new column,
/// copy the values across, drop the original and rename the new one.
/// </summary>
/// <remarks>
/// The scripts are generated offline through <see cref="IMigrator.GenerateScript"/>; no Oracle connection is opened.
/// </remarks>
public class ActivityExecutionSerializedMetadataNclobOracleMigrationTests
{
    private const string RuntimeV3_8 = "20260520121722_V3_8";
    private const string RuntimeSerializedMetadataNclob = "20261022120000_ActivityExecutionSerializedMetadataNclob";

    [Theory]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Default)]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Idempotent)]
    [InlineData("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public void Up_ConvertsSerializedMetadataToNclobWithoutInPlaceAlter(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateRuntimeScript(RuntimeV3_8, RuntimeSerializedMetadataNclob, schema, options);

        AssertColumnConverted(script, schema, "ActivityExecutionRecords", "SerializedMetadata", "NCLOB", "TO_NCLOB(\"SerializedMetadata\")", notNull: false);
    }

    [Theory]
    [InlineData("Elsa", MigrationsSqlGenerationOptions.Default)]
    [InlineData("custom_schema", MigrationsSqlGenerationOptions.Default)]
    public void Down_ConvertsSerializedMetadataBackToNvarchar2WithoutInPlaceAlter(string schema, MigrationsSqlGenerationOptions options)
    {
        var script = GenerateRuntimeScript(RuntimeSerializedMetadataNclob, RuntimeV3_8, schema, options);

        AssertColumnConverted(script, schema, "ActivityExecutionRecords", "SerializedMetadata", "NVARCHAR2(2000)", "DBMS_LOB.SUBSTR(\"SerializedMetadata\", 2000, 1)", notNull: false);
    }

    [Theory]
    [InlineData("Elsa")]
    [InlineData("custom_schema")]
    public void Down_RefusesToTruncateOversizedSerializedMetadataBeforeConverting(string schema)
    {
        var script = GenerateRuntimeScript(RuntimeSerializedMetadataNclob, RuntimeV3_8, schema, MigrationsSqlGenerationOptions.Default);

        var qualifiedTable = Qualify(schema, "ActivityExecutionRecords");
        var lengthCheck = AssertStatementAt(script, $"SELECT COUNT(*) FROM {qualifiedTable} WHERE DBMS_LOB.GETLENGTH(\"SerializedMetadata\") > 2000");
        var firstConversionStatement = AssertStatementAt(script, $"ALTER TABLE {qualifiedTable} ADD (\"SerializedMetadata_New\" NVARCHAR2(2000))");

        Assert.True(lengthCheck < firstConversionStatement, $"Expected the length check for \"SerializedMetadata\" on {qualifiedTable} to run before the conversion, but found the check at {lengthCheck} and the first conversion statement at {firstConversionStatement}:\n{script}");
        Assert.Contains("RAISE_APPLICATION_ERROR(-20004,", script, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeModel_HasNoPendingSerializedMetadataDiff()
    {
        var operations = RuntimeModelDiffer.GetDifferences(ConfigureOracle);
        var serializedMetadataAlters = operations
            .OfType<AlterColumnOperation>()
            .Where(operation =>
                string.Equals(operation.Table, "ActivityExecutionRecords", StringComparison.Ordinal)
                && string.Equals(operation.Name, "SerializedMetadata", StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            serializedMetadataAlters.Length == 0,
            "Expected no pending model diff for ActivityExecutionRecords.SerializedMetadata, but found:"
            + Environment.NewLine
            + RuntimeModelDiffer.Format(serializedMetadataAlters));

        Assert.Equal(
            "NCLOB",
            RuntimeModelDiffer.GetSnapshotColumnType(ConfigureOracle, typeof(ActivityExecutionRecord), "SerializedMetadata"));
    }

    private static void AssertColumnConverted(string script, string schema, string table, string column, string toColumnDefinition, string expectedCopyExpression, bool notNull)
    {
        var qualifiedTable = Qualify(schema, table);
        var tempColumn = $"{column}_New";

        Assert.DoesNotContain($"MODIFY \"{column}\"", script, StringComparison.Ordinal);

        var add = AssertStatementAt(script, $"ALTER TABLE {qualifiedTable} ADD (\"{tempColumn}\" {toColumnDefinition})");

        var copyStatement = $"UPDATE {qualifiedTable} SET \"{tempColumn}\" = CASE WHEN \"{column}\" IS NULL THEN NULL ELSE {expectedCopyExpression} END";
        var copy = AssertStatementAt(script, copyStatement);
        Assert.DoesNotContain($"{copyStatement} WHERE", script, StringComparison.Ordinal);

        var drop = AssertStatementAt(script, $"ALTER TABLE {qualifiedTable} DROP COLUMN \"{column}\"");
        var rename = AssertStatementAt(script, $"ALTER TABLE {qualifiedTable} RENAME COLUMN \"{tempColumn}\" TO \"{column}\"");

        Assert.True(add < copy && copy < drop && drop < rename, $"Expected the add/copy/drop/rename sequence for \"{column}\" on {qualifiedTable} in that order, but found them at {add}, {copy}, {drop} and {rename}:\n{script}");
        Assert.Contains($"FROM ALL_TAB_COLUMNS WHERE OWNER = '{schema}' AND TABLE_NAME = '{table}' AND COLUMN_NAME = p_column", script, StringComparison.Ordinal);

        var setNotNull = $"ALTER TABLE {qualifiedTable} MODIFY (\"{column}\" NOT NULL)";

        if (!notNull)
        {
            Assert.DoesNotContain(setNotNull, script, StringComparison.Ordinal);
            return;
        }

        var constrain = AssertStatementAt(script, setNotNull);
        Assert.True(rename < constrain, $"Expected \"{column}\" on {qualifiedTable} to be constrained to NOT NULL after the rename, but the constraint appears at {constrain} and the rename at {rename}:\n{script}");
    }

    private static int AssertStatementAt(string script, string statement)
    {
        var index = script.IndexOf(statement, StringComparison.Ordinal);

        Assert.True(index >= 0, $"Expected the generated script to contain '{statement}':\n{script}");

        return index;
    }

    private static string Qualify(string schema, string table) => $"\"{schema}\".\"{table}\"";

    private static void ConfigureOracle(DbContextOptionsBuilder<RuntimeElsaDbContext> builder) =>
        builder.UseElsaOracle(
            typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Runtime.V3_6).Assembly,
            "Data Source=unused",
            new ElsaDbContextOptions().ConfigureOracle());

    private static string GenerateRuntimeScript(string fromMigration, string toMigration, string schema, MigrationsSqlGenerationOptions options) =>
        MigrationScriptGenerator.Generate<RuntimeElsaDbContext>(
            builder => builder.UseElsaOracle(typeof(Elsa.Persistence.EFCore.Oracle.Migrations.Runtime.V3_6).Assembly, "Data Source=unused", new ElsaDbContextOptions { SchemaName = schema }),
            fromMigration,
            toMigration,
            options);
}
