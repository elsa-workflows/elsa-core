namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// PerTenantLabelUniqueness must stamp null TenantId to "" and must not
/// auto-delete duplicate labels (associations would be at risk). A preflight
/// lists leftover keys and aborts; CreateIndex then fails loudly if any remain.
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

    [Theory]
    [InlineData("Elsa.Persistence.EFCore.Sqlite", "RAISE")]
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

    private static string FindMigration(string providerProject)
    {
        var repoRoot = FindRepoRoot();
        var labelsDir = Path.Combine(repoRoot, "src", "modules", providerProject, "Migrations", "Labels");
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
