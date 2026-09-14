namespace Elsa.Secrets.UnitTests;

/// <summary>
/// SecretDefaultTenantUniqueness must stamp null TenantId to "" and must not
/// auto-delete duplicate secrets. A preflight lists leftover keys and aborts;
/// CreateIndex then fails loudly if any remain. SQL Server/Oracle keep the
/// filtered unique index (TenantId IS NOT NULL) after leftover nulls become "".
/// </summary>
public class SecretDefaultTenantUniquenessMigrationTests
{
    [Theory]
    [InlineData("Elsa.Secrets.Persistence.EFCore.Sqlite")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.SqlServer")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.PostgreSql")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.MySql")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.Oracle")]
    public void SecretDefaultTenantUniqueness_StampsNullTenantIdAndDoesNotDeleteDuplicates(string providerProject)
    {
        var migration = FindMigration(providerProject);

        Assert.Contains("TenantId", migration, StringComparison.Ordinal);
        Assert.Contains("IS NULL", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE FROM", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DeleteData", migration, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COALESCE", migration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SecretDefaultTenantUniqueness_SqliteFailsLoudlyAtCreateIndex()
    {
        // SQLite cannot abort from a standalone SELECT, so CreateIndex unique is the fail-loud path.
        var migration = FindMigration("Elsa.Secrets.Persistence.EFCore.Sqlite");
        Assert.DoesNotContain("RAISE", migration, StringComparison.Ordinal);
        Assert.Contains("CreateIndex", migration, StringComparison.Ordinal);
        Assert.Contains("unique: true", migration, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Elsa.Secrets.Persistence.EFCore.SqlServer", "THROW")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.PostgreSql", "RAISE EXCEPTION")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.MySql", "SIGNAL")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.Oracle", "RAISE_APPLICATION_ERROR")]
    public void SecretDefaultTenantUniqueness_PreflightsDuplicateKeys(string providerProject, string abortKeyword)
    {
        var migration = FindMigration(providerProject);

        Assert.Contains(abortKeyword, migration, StringComparison.Ordinal);
        Assert.Contains("COUNT(*)", migration, StringComparison.Ordinal);
        Assert.Contains("Duplicate keys", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HAVING COUNT(*) > 1", migration, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Elsa.Secrets.Persistence.EFCore.SqlServer", "[TenantId] IS NOT NULL")]
    [InlineData("Elsa.Secrets.Persistence.EFCore.Oracle", "\\\"TenantId\\\" IS NOT NULL")]
    public void SecretDefaultTenantUniqueness_KeepsFilteredUniqueIndex(string providerProject, string filter)
    {
        var migration = FindMigration(providerProject);
        Assert.Contains(filter, migration, StringComparison.Ordinal);
    }

    private static string FindMigration(string providerProject)
    {
        var repoRoot = FindRepoRoot();
        var secretsDir = Path.Combine(repoRoot, "src", "modules", providerProject, "Migrations", "Secrets");
        var path = Directory.GetFiles(secretsDir, "*SecretDefaultTenantUniqueness.cs")
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
