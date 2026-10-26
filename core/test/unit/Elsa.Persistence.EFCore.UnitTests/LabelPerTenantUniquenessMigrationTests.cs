using Elsa.Persistence.EFCore.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// PerTenantLabelUniqueness must stamp null TenantId to "" and must not
/// auto-delete duplicate labels (associations would be at risk). Providers that
/// can abort from raw SQL list leftover keys first; SQLite cannot, so CreateIndex
/// is the fail-loud path.
/// SQL Server/MySQL/Oracle also preflight over-length Name/NormalizedName
/// before narrowing those columns. PostgreSQL/SQLite keep provider column types.
/// </summary>
public class LabelPerTenantUniquenessMigrationTests
{
    [Theory]
    [InlineData("Elsa.Persistence.EFCore.Sqlite")]
    [InlineData("Elsa.Persistence.EFCore.SqlServer")]
    [InlineData("Elsa.Persistence.EFCore.PostgreSql")]
    [InlineData("Elsa.Persistence.EFCore.MySql")]
    [InlineData("Elsa.Persistence.EFCore.Oracle")]
    public void PerTenantLabelUniqueness_StampsNullTenantIdAndDoesNotDeleteDuplicates(string providerProject)
    {
        var migration = FindMigration(providerProject);

        Assert.Contains("TenantId", migration, StringComparison.Ordinal);
        Assert.Contains("IS NULL", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeleteData", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COALESCE", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PerTenantLabelUniqueness_SqliteFailsLoudlyAtCreateIndex()
    {
        // SQLite cannot abort from a standalone SELECT, so CreateIndex unique is the fail-loud path.
        var migration = FindMigration("Elsa.Persistence.EFCore.Sqlite");
        Assert.DoesNotContain("RAISE", migration, StringComparison.Ordinal);
        Assert.Contains("CreateIndex", migration, StringComparison.Ordinal);
        Assert.Contains("unique: true", migration, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PerTenantLabelUniqueness_SqliteMigrateAsync_SucceedsOnEmptyDatabase()
    {
        var databasePath = Path.Join(Path.GetTempPath(), $"elsa-labels-uniqueness-{Guid.NewGuid():N}.db");

        try
        {
            await using var dbContext = new LabelsDbContextFactory()
                .CreateDbContext(["--connectionString", $"Data Source={databasePath}"]);

            await dbContext.Database.MigrateAsync();

            var applied = await dbContext.Database.GetAppliedMigrationsAsync();
            Assert.Contains(applied, id => id.Contains("PerTenantLabelUniqueness", StringComparison.Ordinal));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Theory]
    [InlineData("Elsa.Persistence.EFCore.SqlServer", "THROW")]
    [InlineData("Elsa.Persistence.EFCore.PostgreSql", "RAISE EXCEPTION")]
    [InlineData("Elsa.Persistence.EFCore.MySql", "SIGNAL")]
    [InlineData("Elsa.Persistence.EFCore.Oracle", "RAISE_APPLICATION_ERROR")]
    public void PerTenantLabelUniqueness_PreflightsDuplicateKeys(string providerProject, string abortKeyword)
    {
        var migration = FindMigration(providerProject);

        Assert.Contains(abortKeyword, migration, StringComparison.Ordinal);
        Assert.Contains("COUNT(*)", migration, StringComparison.Ordinal);
        Assert.Contains("Duplicate keys", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HAVING COUNT(*) > 1", migration, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Elsa.Persistence.EFCore.SqlServer")]
    [InlineData("Elsa.Persistence.EFCore.MySql")]
    [InlineData("Elsa.Persistence.EFCore.Oracle")]
    public void PerTenantLabelUniqueness_PreflightsOverLengthRowsBeforeAlter(string providerProject)
    {
        var migration = FindMigration(providerProject);

        Assert.Contains("255", migration, StringComparison.Ordinal);
        Assert.Contains("Over-length", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AlterColumn", migration, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Elsa.Persistence.EFCore.Sqlite")]
    [InlineData("Elsa.Persistence.EFCore.PostgreSql")]
    public void PerTenantLabelUniqueness_KeepsUnboundedProviderColumnTypes(string providerProject)
    {
        var migration = FindMigration(providerProject);

        Assert.DoesNotContain("AlterColumn", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("varchar(255)", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nvarchar(255)", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Elsa.Persistence.EFCore.SqlServer", "[TenantId] IS NOT NULL")]
    [InlineData("Elsa.Persistence.EFCore.Oracle", "\\\"TenantId\\\" IS NOT NULL")]
    public void PerTenantLabelUniqueness_KeepsFilteredUniqueIndex(string providerProject, string filter)
    {
        var migration = FindMigration(providerProject);
        Assert.Contains(filter, migration, StringComparison.Ordinal);
    }

    [Fact]
    public void PerTenantLabelUniqueness_MySqlRawSqlDoesNotQualifySchema()
    {
        // Pomelo SchemaBehavior.Ignore creates tables in DATABASE(), not a database named Elsa.
        // This repo has no MySQL Testcontainers coverage, so assert the emitted SQL instead.
        var migration = FindMigration("Elsa.Persistence.EFCore.MySql");

        Assert.DoesNotContain("`{_schema.Schema}`.", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("`Elsa`.", migration, StringComparison.Ordinal);
        Assert.Contains("UPDATE `Labels`", migration, StringComparison.Ordinal);
        Assert.Contains("FROM `Labels`", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void PerTenantLabelUniqueness_SqliteRawSqlDoesNotQualifySchema()
    {
        // SQLite ignores schemas; "Elsa"."Labels" looks for an attached database named Elsa.
        var migration = FindMigration("Elsa.Persistence.EFCore.Sqlite");

        Assert.DoesNotContain("\"{_schema.Schema}\".\"Labels\"", migration, StringComparison.Ordinal);
        Assert.Contains("UPDATE {table}", migration, StringComparison.Ordinal);
        Assert.Contains("const string table = \"\\\"Labels\\\"\"", migration, StringComparison.Ordinal);
    }

    private static string FindMigration(string providerProject)
    {
        var repoRoot = FindRepoRoot();
        var labelsDir = Path.Combine(repoRoot, "core", "src", "modules", providerProject, "Migrations", "Labels");
        var path = Directory.GetFiles(labelsDir, "*PerTenantLabelUniqueness.cs")
            .Single(file => !file.EndsWith(".Designer.cs", StringComparison.Ordinal));
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Elsa.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find Elsa.sln from the test output directory.");
    }
}
