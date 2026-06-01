using Elsa.ModularPersistence;

namespace Elsa.ModularPersistence.UnitTests;

public class ModularPersistenceAssemblyTests
{
    private readonly List<string?> _referencedAssemblyNames = typeof(ModularPersistenceAssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Elsa.",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.Sqlite",
        "FluentMigrator",
        "MongoDB",
        "System.Data.SqlClient",
        "Microsoft.Data.SqlClient",
        "CShells"
    ];

    [Fact]
    public void AssemblyDoesNotReferenceForbiddenDependencies()
    {
        var forbiddenReferences = _referencedAssemblyNames.Where(name => name is not null && ForbiddenReferencePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToList();

        Assert.Empty(forbiddenReferences);
    }
}
