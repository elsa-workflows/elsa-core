using Elsa.ModularPersistence.SqlServer;

namespace Elsa.ModularPersistence.SqlServer.UnitTests;

public class ModularPersistenceSqlServerAssemblyTests
{
    private readonly List<string?> _referencedAssemblyNames = typeof(ModularPersistenceSqlServerAssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

    private static readonly string[] RequiredReferences =
    [
        "Elsa.ModularPersistence.Relational",
        "Microsoft.Data.SqlClient"
    ];

    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.Sqlite",
        "FluentMigrator",
        "MongoDB"
    ];

    [Fact]
    public void AssemblyReferencesExpectedSqlServerDependencies()
    {
        foreach (var reference in RequiredReferences)
            Assert.Contains(reference, _referencedAssemblyNames);
    }

    [Fact]
    public void AssemblyDoesNotReferenceOtherProviderDependencies()
    {
        var forbiddenReferences = _referencedAssemblyNames.Where(name => name is not null && ForbiddenReferencePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToList();

        Assert.Empty(forbiddenReferences);
    }
}
