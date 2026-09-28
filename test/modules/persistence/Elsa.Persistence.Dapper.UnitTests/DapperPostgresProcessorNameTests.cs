using Elsa.Persistence.Dapper.Migrations;
using FluentMigrator;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Documents FluentMigrator 7.2 processor identifiers and the Dapper IfDatabase match.
/// </summary>
public sealed class DapperPostgresProcessorNameTests
{
    [Fact(DisplayName = "FluentMigrator 7.2 AddPostgres() reports PostgreSQL15_0 / PostgreSQL, not Postgres")]
    public void AddPostgres_Processor_ReportsPostgreSQL_NotPostgres()
    {
        using var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb => rb
                .AddPostgres()
                .WithGlobalConnectionString("Host=localhost;Database=unused"))
            .BuildServiceProvider(false);
        using var scope = services.CreateScope();
        var processor = scope.ServiceProvider.GetRequiredService<IMigrationProcessor>();

        var names = new HashSet<string>(processor.DatabaseTypeAliases, StringComparer.OrdinalIgnoreCase)
        {
            processor.DatabaseType
        };

        Assert.Equal("PostgreSQL15_0", processor.DatabaseType);
        Assert.Contains("PostgreSQL", names);
        Assert.DoesNotContain("Postgres", names);

        var legacyGuard = new[] { "SqlServer", "Oracle", "MySql", "Postgres" };
        Assert.False(legacyGuard.Any(names.Contains), "IfDatabase(\"Postgres\") must not match FluentMigrator 7.2 AddPostgres().");
        Assert.True(MigrationDatabases.IsDateTimeOffsetProvider(processor.DatabaseType));
    }

    [Theory]
    [InlineData("Postgres")]
    [InlineData("PostgreSQL")]
    [InlineData("PostgreSQL10_0")]
    [InlineData("PostgreSQL11_0")]
    [InlineData("PostgreSQL15_0")]
    [InlineData("Postgres92")]
    [InlineData("PostgreSQL92")]
    [InlineData("postgres")]
    [InlineData("postgresql15_0")]
    [InlineData("SqlServer")]
    [InlineData("Oracle")]
    [InlineData("MySql")]
    public void DateTimeOffsetProvider_MatchesSqlServerOracleMySql_AndEveryFluentMigrator72PostgresName(string databaseType)
    {
        Assert.True(MigrationDatabases.IsDateTimeOffsetProvider(databaseType));
    }

    [Theory]
    [InlineData("Sqlite")]
    [InlineData("SQLite")]
    [InlineData("SqlServer2008")]
    [InlineData("MySql8")]
    [InlineData("Oracle12c")]
    public void DateTimeOffsetProvider_DoesNotChangeOtherProviders(string databaseType)
    {
        Assert.False(MigrationDatabases.IsDateTimeOffsetProvider(databaseType));
    }
}
