using Elsa.ModularPersistence.MongoDb;

namespace Elsa.ModularPersistence.MongoDb.UnitTests;

public class ModularPersistenceMongoDbAssemblyTests
{
    private readonly List<string?> _referencedAssemblyNames = typeof(ModularPersistenceMongoDbAssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToList();

    private static readonly string[] RequiredReferences =
    [
        "Elsa.ModularPersistence",
        "MongoDB.Driver"
    ];

    private static readonly string[] ForbiddenReferencePrefixes =
    [
        "Elsa.ModularPersistence.Relational",
        "Microsoft.EntityFrameworkCore",
        "Microsoft.Data.Sqlite",
        "Microsoft.Data.SqlClient",
        "FluentMigrator"
    ];

    [Fact]
    public void AssemblyReferencesExpectedMongoDbDependencies()
    {
        foreach (var reference in RequiredReferences)
            Assert.Contains(reference, _referencedAssemblyNames);
    }

    [Fact]
    public void AssemblyDoesNotReferenceRelationalProviderDependencies()
    {
        var forbiddenReferences = _referencedAssemblyNames.Where(name => name is not null && ForbiddenReferencePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal))).ToList();

        Assert.Empty(forbiddenReferences);
    }
}
