using System.IO;
using System.Threading.Tasks;

namespace Elsa.Secrets.UnitTests;

/// <summary>
/// SecretDefaultTenantUniqueness must stamp null TenantId to "" and must not
/// auto-delete duplicate secrets. A preflight lists leftover keys and aborts;
/// CreateIndex then fails loudly if any remain. SQL Server keeps the filtered
/// unique index (TenantId IS NOT NULL) after leftover nulls become "". Oracle
/// stores '' as NULL, so it uses NVL(TenantId, CHR(1)) instead of a filter.
/// </summary>
public class SecretDefaultTenantUniquenessMigrationTests
{
    [Test]
    [Arguments("Elsa.Secrets.Persistence.EFCore.Sqlite")]
    [Arguments("Elsa.Secrets.Persistence.EFCore.SqlServer")]
    [Arguments("Elsa.Secrets.Persistence.EFCore.PostgreSql")]
    [Arguments("Elsa.Secrets.Persistence.EFCore.MySql")]
    [Arguments("Elsa.Secrets.Persistence.EFCore.Oracle")]
    public async Task SecretDefaultTenantUniqueness_StampsNullTenantIdAndDoesNotDeleteDuplicates(string providerProject)
    {
        var migration = FindMigration(providerProject);

        await Assert.That(migration).Contains("TenantId");
        await Assert.That(migration).Contains("IS NULL");
        await Assert.That(migration).DoesNotContain("DELETE FROM");
        await Assert.That(migration).DoesNotContain("DeleteData");
        await Assert.That(migration).DoesNotContain("COALESCE");
    }

    [Test]
    public async Task SecretDefaultTenantUniqueness_SqliteFailsLoudlyAtCreateIndex()
    {
        // SQLite cannot abort from a standalone SELECT, so CreateIndex unique is the fail-loud path.
        var migration = FindMigration("Elsa.Secrets.Persistence.EFCore.Sqlite");
        await Assert.That(migration).DoesNotContain("RAISE");
        await Assert.That(migration).Contains("CreateIndex");
        await Assert.That(migration).Contains("unique: true");
    }

    [Test]
    [Arguments("Elsa.Secrets.Persistence.EFCore.SqlServer", "THROW")]
    [Arguments("Elsa.Secrets.Persistence.EFCore.PostgreSql", "RAISE EXCEPTION")]
    [Arguments("Elsa.Secrets.Persistence.EFCore.MySql", "SIGNAL")]
    [Arguments("Elsa.Secrets.Persistence.EFCore.Oracle", "RAISE_APPLICATION_ERROR")]
    public async Task SecretDefaultTenantUniqueness_PreflightsDuplicateKeys(string providerProject, string abortKeyword)
    {
        var migration = FindMigration(providerProject);

        await Assert.That(migration).Contains(abortKeyword);
        await Assert.That(migration).Contains("COUNT(*)");
        await Assert.That(migration).Contains("Duplicate keys");
        await Assert.That(migration).Contains("HAVING COUNT(*) > 1");
    }

    [Test]
    public async Task SecretDefaultTenantUniqueness_SqlServerKeepsFilteredUniqueIndex()
    {
        var migration = FindMigration("Elsa.Secrets.Persistence.EFCore.SqlServer");
        await Assert.That(migration).Contains("[TenantId] IS NOT NULL");
    }

    [Test]
    public async Task SecretDefaultTenantUniqueness_OracleUsesNvlBecauseEmptyStringIsNull()
    {
        var migration = FindMigration("Elsa.Secrets.Persistence.EFCore.Oracle");
        await Assert.That(migration).Contains("NVL(\"TenantId\", CHR(1))");
        await Assert.That(migration).Contains("CREATE UNIQUE INDEX");
    }

    private static string FindMigration(string providerProject)
    {
        if (Path.IsPathRooted(providerProject))
            throw new ArgumentException("Provider project must be a relative path.", nameof(providerProject));

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
            if (File.Exists(Path.Join(directory.FullName, "Elsa.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not find Elsa.sln from the test output directory.");
    }
}
