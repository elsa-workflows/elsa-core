using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// Regression tests for the V3_6 Runtime migration's raw SQL statements. `dotnet ef migrations script`
/// previously produced invalid SQL because the `DROP INDEX` statements were not terminated with a
/// semicolon, which corrupts both the idempotent PL/pgSQL block and the plain script.
/// </summary>
public class V3_6RuntimeMigrationTests
{
    private const string DropIndexPrefix = "DROP INDEX IF EXISTS";
    private const string WorkflowExecutionLogRecordIndexName = "IX_WorkflowExecutionLogRecord_ActivityNodeId";
    private const string ActivityExecutionRecordIndexName = "IX_ActivityExecutionRecord_ActivityNodeId";

    [Theory]
    [InlineData("Elsa")]
    [InlineData("custom_schema")]
    public void GenerateScript_PostgreSql_Idempotent_TerminatesDropIndexStatements(string schema)
    {
        var script = GeneratePostgreSqlScript(MigrationsSqlGenerationOptions.Idempotent, schema);

        AssertDropIndexStatementsAreTerminated(script, schema, requireTrailingEndIf: true);
    }

    [Theory]
    [InlineData("Elsa")]
    [InlineData("custom_schema")]
    public void GenerateScript_PostgreSql_Plain_TerminatesDropIndexStatements(string schema)
    {
        var script = GeneratePostgreSqlScript(MigrationsSqlGenerationOptions.Default, schema);

        AssertDropIndexStatementsAreTerminated(script, schema);
    }

    // Note: EF Core's SQLite provider does not support generating idempotent migration scripts
    // (SqliteHistoryRepository.GetEndIfScript throws NotSupportedException), so only the plain
    // script form is exercised here. The SQLite migration also never schema-qualifies its DROP
    // INDEX statements, so there is no schema-prefixed variant to cover.
    [Fact]
    public void GenerateScript_Sqlite_Plain_TerminatesDropIndexStatements()
    {
        var script = GenerateSqliteScript(MigrationsSqlGenerationOptions.Default);

        AssertDropIndexStatementsAreTerminated(script, schema: null);
    }

    // ElsaDbContextOptions.SchemaName always falls back to ElsaDbContextBase.ElsaSchema ("Elsa") when it
    // isn't set, so the branch the V3_6 migration guards against with
    // `_schema.Schema != null ? "\"{schema}\"." : ""` - a null schema - is unreachable through the
    // public options and is therefore not covered here.
    private static string GeneratePostgreSqlScript(MigrationsSqlGenerationOptions options, string schema)
    {
        var migrationsAssembly = typeof(Elsa.Persistence.EFCore.PostgreSql.Migrations.Runtime.V3_6).Assembly;
        var contextOptions = new ElsaDbContextOptions { SchemaName = schema };

        return MigrationScriptGenerator.Generate<RuntimeElsaDbContext>(
            builder => builder.UseElsaPostgreSql(migrationsAssembly, "Host=unused", contextOptions),
            fromMigration: "20250530104953_V3_5",
            toMigration: "20251204150341_V3_6",
            options);
    }

    private static string GenerateSqliteScript(MigrationsSqlGenerationOptions options)
    {
        var migrationsAssembly = typeof(Elsa.Persistence.EFCore.Sqlite.Migrations.Runtime.V3_6).Assembly;

        return MigrationScriptGenerator.Generate<RuntimeElsaDbContext>(
            builder => builder.UseElsaSqlite(migrationsAssembly, "Data Source=:memory:"),
            fromMigration: "20250530104854_V3_5",
            toMigration: "20251204150006_V3_6",
            options);
    }

    private static void AssertDropIndexStatementsAreTerminated(string script, string? schema, bool requireTrailingEndIf = false)
    {
        AssertDropIndexStatementIsTerminated(script, WorkflowExecutionLogRecordIndexName, schema, requireTrailingEndIf);
        AssertDropIndexStatementIsTerminated(script, ActivityExecutionRecordIndexName, schema, requireTrailingEndIf);
    }

    private static void AssertDropIndexStatementIsTerminated(string script, string indexName, string? schema, bool requireTrailingEndIf)
    {
        var expectedStatement = schema != null
            ? $"{DropIndexPrefix} \"{schema}\".\"{indexName}\";"
            : $"{DropIndexPrefix} \"{indexName}\";";

        var lines = script.Split('\n').Select(l => l.Trim()).ToList();
        var dropLineIndex = lines.FindIndex(l => l.StartsWith(DropIndexPrefix, StringComparison.Ordinal) && l.Contains(indexName, StringComparison.Ordinal));

        Assert.True(dropLineIndex >= 0, $"Expected to find a 'DROP INDEX IF EXISTS' statement for \"{indexName}\" in the generated script:\n{script}");
        Assert.Equal(expectedStatement, lines[dropLineIndex]);

        if (!requireTrailingEndIf)
            return;

        // The statement inside the DO $EF$ ... IF NOT EXISTS(...) THEN <statement> END IF; block must be
        // terminated before the following END IF; line, otherwise the PL/pgSQL block fails to parse.
        var endIfLineIndex = lines.FindIndex(dropLineIndex, l => l.Equals("END IF;", StringComparison.Ordinal));
        Assert.True(endIfLineIndex >= 0, $"Expected an 'END IF;' line after the DROP INDEX statement for \"{indexName}\":\n{script}");
    }
}
