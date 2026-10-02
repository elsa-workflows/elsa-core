using Elsa.Persistence.Dapper.Migrations;
using FluentMigrator;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Documents FluentMigrator 7.2 processor identifiers and the alias-aware IfDatabase match.
/// </summary>
public sealed class DapperPostgresProcessorNameTests
{
    private static readonly string[] SqliteProviders = ["Sqlite"];

    [Fact(DisplayName = "FluentMigrator 7.2 AddPostgres() reports PostgreSQL15_0 / PostgreSQL, not Postgres")]
    public void AddPostgres_Processor_ReportsPostgreSQL_NotPostgres()
    {
        var processor = ResolveIds(rb => rb.AddPostgres());

        Assert.Equal("PostgreSQL15_0", processor.DatabaseType);
        Assert.Contains("PostgreSQL", processor.Names);
        Assert.DoesNotContain("Postgres", processor.Names);

        var legacyGuard = new[] { "SqlServer", "Oracle", "MySql", "Postgres" };
        Assert.False(legacyGuard.Any(processor.Names.Contains), "IfDatabase(\"Postgres\") must not match FluentMigrator 7.2 AddPostgres().");
        Assert.True(Matches(MigrationDatabases.DateTimeOffsetProviders, processor));
    }

    [Fact(DisplayName = "AddSqlServer() takes the DateTimeOffset IfDatabase branch via the SqlServer alias")]
    public void AddSqlServer_TakesDateTimeOffsetBranch() =>
        AssertExactlyOneCreateBranch(ResolveIds(rb => rb.AddSqlServer()), dateTimeOffset: true);

    [Fact(DisplayName = "AddSQLite() takes the Sqlite IfDatabase branch")]
    public void AddSqlite_TakesSqliteBranch() =>
        AssertExactlyOneCreateBranch(ResolveIds(rb => rb.AddSQLite()), dateTimeOffset: false);

    [Fact(DisplayName = "AddPostgres() takes the DateTimeOffset IfDatabase branch via the PostgreSQL alias")]
    public void AddPostgres_TakesDateTimeOffsetBranch() =>
        AssertExactlyOneCreateBranch(ResolveIds(rb => rb.AddPostgres()), dateTimeOffset: true);

    [Fact(DisplayName = "AddMySql() takes the DateTimeOffset IfDatabase branch via the MySql alias")]
    public void AddMySql_TakesDateTimeOffsetBranch() =>
        AssertExactlyOneCreateBranch(ResolveIds(rb => rb.AddMySql()), dateTimeOffset: true);

    [Fact(DisplayName = "AddOracleManaged() takes the DateTimeOffset IfDatabase branch via the Oracle alias")]
    public void AddOracleManaged_TakesDateTimeOffsetBranch() =>
        AssertExactlyOneCreateBranch(ResolveIds(rb => rb.AddOracleManaged()), dateTimeOffset: true);

    [Fact]
    public void DateTimeOffsetProviders_MatchSqlServer2016ThroughAlias_NotDatabaseTypeAlone()
    {
        // AddSqlServer() reports DatabaseType SqlServer2016 with alias SqlServer.
        // The params overload matches aliases; a DatabaseType-only predicate would miss it.
        Assert.DoesNotContain("SqlServer2016", MigrationDatabases.DateTimeOffsetProviders);
        Assert.True(Matches(MigrationDatabases.DateTimeOffsetProviders, "SqlServer2016", ["SqlServer"]));
        Assert.False(Matches(MigrationDatabases.DateTimeOffsetProviders, "SqlServer2016", []));
    }

    [Theory]
    [InlineData("Postgres")]
    [InlineData("PostgreSQL")]
    [InlineData("PostgreSQL92")]
    [InlineData("SqlServer")]
    [InlineData("Oracle")]
    [InlineData("MySql")]
    public void DateTimeOffsetProviders_ContainsCanonicalNames(string name)
    {
        Assert.Contains(name, MigrationDatabases.DateTimeOffsetProviders);
    }

    [Fact]
    public void DateTimeOffsetProviders_DoesNotContainSqlite()
    {
        Assert.DoesNotContain("Sqlite", MigrationDatabases.DateTimeOffsetProviders, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("SQLite", MigrationDatabases.DateTimeOffsetProviders, StringComparer.OrdinalIgnoreCase);
    }

    private static void AssertExactlyOneCreateBranch(ProcessorIds processor, bool dateTimeOffset)
    {
        var dateTimeOffsetApplies = Matches(MigrationDatabases.DateTimeOffsetProviders, processor);
        var sqliteApplies = Matches(SqliteProviders, processor);
        Assert.NotEqual(dateTimeOffsetApplies, sqliteApplies);
        Assert.Equal(dateTimeOffset, dateTimeOffsetApplies);
    }

    private static bool Matches(IReadOnlyCollection<string> guard, ProcessorIds processor) =>
        Matches(guard, processor.DatabaseType, processor.Aliases);

    private static bool Matches(IReadOnlyCollection<string> guard, string databaseType, IEnumerable<string> aliases)
    {
        // Same semantics as FluentMigrator 7.2 IfDatabase(params string[]):
        // exact OrdinalIgnoreCase against DatabaseType ∪ DatabaseTypeAliases.
        var names = new HashSet<string>(aliases, StringComparer.OrdinalIgnoreCase) { databaseType };
        return guard.Any(names.Contains);
    }

    private static ProcessorIds ResolveIds(Action<IMigrationRunnerBuilder> addProvider)
    {
        using var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
            {
                addProvider(rb);
                rb.WithGlobalConnectionString("Host=localhost;Database=unused");
            })
            .BuildServiceProvider(false);
        using var scope = services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IMigrationProcessor>();
        return new ProcessorIds(processor.DatabaseType, processor.DatabaseTypeAliases.ToArray());
    }

    private sealed record ProcessorIds(string DatabaseType, string[] Aliases)
    {
        public HashSet<string> Names { get; } = new(Aliases, StringComparer.OrdinalIgnoreCase) { DatabaseType };
    }
}
