namespace Elsa.Persistence.EFCore.UnitTests;

/// <summary>
/// PerTenantLabelUniqueness must stamp null TenantId to "" and must not
/// auto-delete duplicate labels (associations would be at risk). CreateIndex
/// then fails loudly if duplicates remain.
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
