using Elsa.ModularPersistence.Sqlite;

namespace Elsa.ModularPersistence.Sqlite.IntegrationTests;

public class ModularPersistenceSqliteAssemblyTests
{
    private readonly List<string?> _referencedAssemblyNames = typeof(ModularPersistenceSqliteAssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "MongoDB",
        "System.Data.SqlClient",
        "Microsoft.Data.SqlClient",
        "CShells"
    ];

    [Fact]
    public void AssemblyReferencesRelational()
    {
        Assert.Contains("Elsa.ModularPersistence.Relational", _referencedAssemblyNames);
    }

    [Fact]
    public void AssemblyDoesNotReferenceNonSqliteProviderDependencies()
    {
        var forbiddenReferences = _referencedAssemblyNames.Where(name => name is not null && ForbiddenReferencePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToList();

        Assert.Empty(forbiddenReferences);
    }
}
