using FluentMigrator;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Persistence.Dapper.UnitTests;

/// <summary>
/// Documents FluentMigrator 7.2's AddPostgres() processor identifiers.
/// The Dapper IfDatabase lists must include both "Postgres" and "PostgreSQL"
/// because 7.2 reports the latter (and "PostgreSQL15_0") but not the former.
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
        var fixedGuard = new[] { "SqlServer", "Oracle", "MySql", "Postgres", "PostgreSQL" };
        Assert.False(legacyGuard.Any(names.Contains), "IfDatabase(\"Postgres\") must not match FluentMigrator 7.2 AddPostgres().");
        Assert.True(fixedGuard.Any(names.Contains), "IfDatabase(..., \"PostgreSQL\") must match FluentMigrator 7.2 AddPostgres().");
    }
}
