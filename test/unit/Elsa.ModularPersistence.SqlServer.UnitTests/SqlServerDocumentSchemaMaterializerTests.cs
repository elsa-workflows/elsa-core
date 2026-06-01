using System.Reflection;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.SqlServer.Options;
using Elsa.ModularPersistence.SqlServer.Services;

namespace Elsa.ModularPersistence.SqlServer.UnitTests;

public class SqlServerDocumentSchemaMaterializerTests
{
    [Fact]
    public void OptimizedIndexesAreOptIn()
    {
        var options = new SqlServerModularPersistenceOptions();

        Assert.False(options.UseOptimizedIndexes);
    }

    [Fact]
    public void BuildOptimizedIndexSqlCreatesFilteredIndexesForDeclaredIndexes()
    {
        var sql = BuildOptimizedIndexSql(CreateManifest());

        Assert.Contains(sql, command => command.Contains("CREATE INDEX IX_ModularPersistenceDocumentIndexes_Optimized_IX_Secrets_Name_Name_StringValue", StringComparison.Ordinal));
        Assert.Contains(sql, command => command.Contains("WHERE IndexName = N'IX_Secrets_Name' AND FieldName = N'Name' AND NullValue = 0", StringComparison.Ordinal));
        Assert.DoesNotContain(sql, command => command.Contains("IX_Secrets_Priority", StringComparison.Ordinal));
    }

    private static IReadOnlyCollection<string> BuildOptimizedIndexSql(StorageManifestDescriptor manifest)
    {
        var method = typeof(SqlServerDocumentSchemaMaterializer).GetMethod("BuildOptimizedIndexSql", BindingFlags.NonPublic | BindingFlags.Static)!;
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
