using System.Reflection;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.PostgreSql.Options;
using Elsa.ModularPersistence.PostgreSql.Services;

namespace Elsa.ModularPersistence.PostgreSql.UnitTests;

public class PostgreSqlDocumentSchemaMaterializerTests
{
    [Fact]
    public void OptimizedJsonbIndexesAreOptIn()
    {
        var options = new PostgreSqlModularPersistenceOptions();

        Assert.False(options.UseOptimizedJsonbIndexes);
    }

    [Fact]
    public void BuildOptimizedJsonbIndexSqlCreatesExpressionIndexesForDeclaredIndexes()
    {
        var sql = BuildOptimizedJsonbIndexSql(CreateManifest());

        Assert.Contains(sql, command => command.Contains("CREATE INDEX IF NOT EXISTS IX_ModularPersistenceDocuments_Jsonb_IX_Secrets_Name_Name", StringComparison.Ordinal));
        Assert.Contains(sql, command => command.Contains("((Data ->> 'Name'))", StringComparison.Ordinal));
        Assert.DoesNotContain(sql, command => command.Contains("Priority", StringComparison.Ordinal));
    }

    private static IReadOnlyCollection<string> BuildOptimizedJsonbIndexSql(StorageManifestDescriptor manifest)
    {
        var method = typeof(PostgreSqlDocumentSchemaMaterializer).GetMethod("BuildOptimizedJsonbIndexSql", BindingFlags.NonPublic | BindingFlags.Static)!;
        return ((IEnumerable<string>)method.Invoke(null, [manifest])!).ToList();
    }

    private static StorageManifestDescriptor CreateManifest() =>
        new(
            "sample.secrets",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Secrets",
                    [
                        new StorageFieldDescriptor("Name", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Priority", StorageFieldType.Int32)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Secrets", ["Name"])
                    ],
                    [
                        new StorageIndexDescriptor("IX_Secrets_Name", [new StorageIndexFieldDescriptor("Name")], physicalizationIntent: PhysicalizationIntent.OptimizedIndexes),
                        new StorageIndexDescriptor("IX_Secrets_Priority", [new StorageIndexFieldDescriptor("Priority")])
                    ])
            ]);
}
