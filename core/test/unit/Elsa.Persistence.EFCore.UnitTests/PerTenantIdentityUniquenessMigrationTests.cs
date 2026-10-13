namespace Elsa.Persistence.EFCore.UnitTests;

public class PerTenantIdentityUniquenessMigrationTests
{
    [Theory]
    [InlineData("Elsa.Persistence.EFCore.Sqlite")]
    [InlineData("Elsa.Persistence.EFCore.SqlServer")]
    [InlineData("Elsa.Persistence.EFCore.PostgreSql")]
    [InlineData("Elsa.Persistence.EFCore.MySql")]
    [InlineData("Elsa.Persistence.EFCore.Oracle")]
    public void PerTenantIdentityUniqueness_UsesConfiguredSchema(string providerProject)
    {
        var migration = FindMigration(providerProject);

        Assert.Contains("IElsaDbContextSchema _schema", migration, StringComparison.Ordinal);
        Assert.Contains("PerTenantIdentityUniqueness(Elsa.Persistence.EFCore.IElsaDbContextSchema schema)", migration, StringComparison.Ordinal);
        Assert.Contains("schema: _schema.Schema", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("schema: \"Elsa\"", migration, StringComparison.Ordinal);
    }

    private static string FindMigration(string providerProject)
    {
        var repoRoot = FindRepoRoot();
        var identityDirectory = Path.Combine(repoRoot, "core", "src", "modules", providerProject, "Migrations", "Identity");
        var path = Directory.GetFiles(identityDirectory, "*PerTenantIdentityUniqueness.cs")
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