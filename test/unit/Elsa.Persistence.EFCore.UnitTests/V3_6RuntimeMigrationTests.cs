using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public void GenerateScript_PostgreSql_Idempotent_TerminatesDropIndexStatements()
    {
        var script = GeneratePostgreSqlScript(MigrationsSqlGenerationOptions.Idempotent);

        AssertDropIndexStatementsAreTerminated(script);
        AssertWellFormedIdempotentBlock(script, WorkflowExecutionLogRecordIndexName);
        AssertWellFormedIdempotentBlock(script, ActivityExecutionRecordIndexName);
    }

    [Fact]
    public void GenerateScript_PostgreSql_Plain_TerminatesDropIndexStatements()
    {
        var script = GeneratePostgreSqlScript(MigrationsSqlGenerationOptions.Default);

        AssertDropIndexStatementsAreTerminated(script);
    }

    // Note: EF Core's SQLite provider does not support generating idempotent migration scripts
    // (SqliteHistoryRepository.GetEndIfScript throws NotSupportedException), so only the plain
    // script form is exercised here.
    [Fact]
    public void GenerateScript_Sqlite_Plain_TerminatesDropIndexStatements()
    {
        var script = GenerateSqliteScript(MigrationsSqlGenerationOptions.Default);

        AssertDropIndexStatementsAreTerminated(script);
    }

    private static string GeneratePostgreSqlScript(MigrationsSqlGenerationOptions options)
    {
        var migrationsAssembly = typeof(Elsa.Persistence.EFCore.PostgreSql.Migrations.Runtime.V3_6).Assembly;
        var dbContextOptions = (DbContextOptions<RuntimeElsaDbContext>)new DbContextOptionsBuilder<RuntimeElsaDbContext>()
            .UseElsaPostgreSql(migrationsAssembly, "Host=unused")
            .Options;

        using var dbContext = new RuntimeElsaDbContext(dbContextOptions, CreateServiceProvider());
        var migrator = dbContext.GetService<IMigrator>();

        return migrator.GenerateScript(fromMigration: "20250530104953_V3_5", toMigration: "20251204150341_V3_6", options: options);
    }

    private static string GenerateSqliteScript(MigrationsSqlGenerationOptions options)
    {
        var migrationsAssembly = typeof(Elsa.Persistence.EFCore.Sqlite.Migrations.Runtime.V3_6).Assembly;
        var dbContextOptions = (DbContextOptions<RuntimeElsaDbContext>)new DbContextOptionsBuilder<RuntimeElsaDbContext>()
            .UseElsaSqlite(migrationsAssembly, "Data Source=:memory:")
            .Options;

        using var dbContext = new RuntimeElsaDbContext(dbContextOptions, CreateServiceProvider());
        var migrator = dbContext.GetService<IMigrator>();

        return migrator.GenerateScript(fromMigration: "20250530104854_V3_5", toMigration: "20251204150006_V3_6", options: options);
    }

    private static IServiceProvider CreateServiceProvider() => new ServiceCollection().BuildServiceProvider();

    private static void AssertDropIndexStatementsAreTerminated(string script)
    {
        AssertDropIndexStatementIsTerminated(script, WorkflowExecutionLogRecordIndexName);
        AssertDropIndexStatementIsTerminated(script, ActivityExecutionRecordIndexName);
    }

    private static void AssertDropIndexStatementIsTerminated(string script, string indexName)
    {
        var line = script
            .Split('\n')
            .Select(l => l.Trim())
            .SingleOrDefault(l => l.StartsWith(DropIndexPrefix, StringComparison.Ordinal) && l.Contains(indexName, StringComparison.Ordinal));

        Assert.True(line != null, $"Expected to find a 'DROP INDEX IF EXISTS' statement for \"{indexName}\" in the generated script:\n{script}");
        Assert.EndsWith(";", line);
    }

    private static void AssertWellFormedIdempotentBlock(string script, string indexName)
    {
        var lines = script.Split('\n').Select(l => l.Trim()).ToList();
        var dropLineIndex = lines.FindIndex(l => l.StartsWith(DropIndexPrefix, StringComparison.Ordinal) && l.Contains(indexName, StringComparison.Ordinal));

        Assert.True(dropLineIndex >= 0, $"Expected to find a 'DROP INDEX IF EXISTS' statement for \"{indexName}\" in the generated script:\n{script}");

        // The statement inside the DO $EF$ ... IF NOT EXISTS(...) THEN <statement> END IF; block must be
        // terminated before the following END IF; line, otherwise the PL/pgSQL block fails to parse.
        var endIfLineIndex = lines.FindIndex(dropLineIndex, l => l.Equals("END IF;", StringComparison.Ordinal));

        Assert.True(endIfLineIndex >= 0, $"Expected an 'END IF;' line after the DROP INDEX statement for \"{indexName}\":\n{script}");
        Assert.EndsWith(";", lines[dropLineIndex]);
    }
}
