using System.Reflection;
using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.MongoDb.Options;
using Elsa.ModularPersistence.MongoDb.Services;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Elsa.ModularPersistence.MongoDb.UnitTests;

public class MongoDbDocumentSchemaMaterializerTests
{
    [Fact]
    public void BuildIndexModelsCreatesNativeIndexesForDeclaredIndexes()
    {
        var models = BuildIndexModels(CreateManifest(), MongoDbCollectionStrategy.SharedCollection);

        Assert.Contains(models, x => x.Options.Name == "IX_ModularPersistenceDocuments_Key");
        Assert.Contains(models, x => x.Options.Name == "IX_ModularPersistenceDocuments_Secrets_IX_Secrets_Name" && x.Options.Unique == false);
        Assert.Contains(models, x => x.Options.Name == "IX_ModularPersistenceDocuments_Secrets_UX_Secrets_Name_Priority" && x.Options.Unique == true);
    }

    [Fact]
    public void BuildIndexModelsIncludesPerTypeIndexesWithoutRelationalArtifacts()
    {
        var models = BuildIndexModels(CreateManifest(), MongoDbCollectionStrategy.CollectionPerType);

        Assert.Contains(models, x => x.Options.Name == "IX_ModularPersistenceDocuments_Secrets_IX_Secrets_Name");
        Assert.DoesNotContain(models, x => x.Options.Name.Contains("DocumentIndexes", StringComparison.Ordinal));
        Assert.DoesNotContain(models, x => x.Options.Name.Contains("SchemaHistory", StringComparison.Ordinal));
    }

    private static IReadOnlyCollection<CreateIndexModel<BsonDocument>> BuildIndexModels(StorageManifestDescriptor manifest, MongoDbCollectionStrategy collectionStrategy)
    {
        var method = typeof(MongoDbDocumentSchemaMaterializer).GetMethod("BuildIndexModels", BindingFlags.NonPublic | BindingFlags.Static)!;
        return ((IEnumerable<CreateIndexModel<BsonDocument>>)method.Invoke(null, [manifest.StorageUnits, collectionStrategy])!).ToArray();
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
                        new StorageIndexDescriptor("IX_Secrets_Name", [new StorageIndexFieldDescriptor("Name")]),
                        new StorageIndexDescriptor(
                            "UX_Secrets_Name_Priority",
                            [
                                new StorageIndexFieldDescriptor("Name"),
                                new StorageIndexFieldDescriptor("Priority", StorageIndexSortOrder.Descending)
                            ],
                            true)
                    ])
            ]);
}
