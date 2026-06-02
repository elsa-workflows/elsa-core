using Elsa.ModularPersistence.Relational;

namespace Elsa.ModularPersistence.Relational.UnitTests;

public class ModularPersistenceRelationalAssemblyTests
{
    private readonly List<string?> _referencedAssemblyNames = typeof(ModularPersistenceRelationalAssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.Sqlite",
        "FluentMigrator",
        "MongoDB",
        "System.Data.SqlClient",
        "Microsoft.Data.SqlClient",
        "CShells"
    ];

    [Fact]
    public void AssemblyReferencesCore()
    {
        Assert.Contains("Elsa.ModularPersistence", _referencedAssemblyNames);
    }

    [Fact]
    public void AssemblyDoesNotReferenceProviderSpecificDependencies()
    {
        var forbiddenReferences = _referencedAssemblyNames.Where(name => name is not null && ForbiddenReferencePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToList();

        Assert.Empty(forbiddenReferences);
    }
}
