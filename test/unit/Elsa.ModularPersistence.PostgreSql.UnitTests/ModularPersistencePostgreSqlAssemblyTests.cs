using Elsa.ModularPersistence.PostgreSql;

namespace Elsa.ModularPersistence.PostgreSql.UnitTests;

public class ModularPersistencePostgreSqlAssemblyTests
{
    private readonly List<string?> _referencedAssemblyNames = typeof(ModularPersistencePostgreSqlAssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

    private static readonly string[] RequiredReferences =
    [
        "Elsa.ModularPersistence.Relational",
        "Npgsql"
    ];

    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.Sqlite",
        "Microsoft.Data.SqlClient",
        "FluentMigrator",
        "MongoDB",
        "CShells"
    ];

    [Fact]
    public void AssemblyReferencesExpectedPostgreSqlDependencies()
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
