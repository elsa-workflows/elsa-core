using Elsa.ModularPersistence;

namespace Elsa.ModularPersistence.UnitTests;

public class ModularPersistenceAssemblyTests
{
    private readonly List<string?> _referencedAssemblyNames = typeof(ModularPersistenceAssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.Sqlite",
        "FluentMigrator",
        "MongoDB",
        "System.Data.SqlClient",
        "Microsoft.Data.SqlClient"
    ];

    private static readonly string[] RequiredReferences =
    [
        "Elsa.Api.Common",
        "Elsa.Common",
        "Elsa.Features"
    ];

    [Fact]
    public void AssemblyReferencesHostingInfrastructureOnly()
    {
        foreach (var reference in RequiredReferences)
            Assert.Contains(reference, _referencedAssemblyNames);
    }

    [Fact]
    public void AssemblyDoesNotReferenceProviderDependencies()
    {
        var forbiddenReferences = _referencedAssemblyNames.Where(name => name is not null && ForbiddenReferencePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToList();

        Assert.Empty(forbiddenReferences);
    }
}
