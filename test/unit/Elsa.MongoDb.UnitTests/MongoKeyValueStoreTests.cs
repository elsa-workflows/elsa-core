using Elsa.KeyValues.Entities;
using Elsa.MongoDb.Common;
using Elsa.MongoDb.Modules.Runtime;
using MongoDB.Driver;
using NSubstitute;

namespace Elsa.MongoDb.UnitTests;

public class MongoKeyValueStoreTests
{
    private readonly MongoDbStore<SerializedKeyValuePair> _mongoDbStore;

    public MongoKeyValueStoreTests()
    {
        var mongoCollectionMock = Substitute.For<IMongoCollection<SerializedKeyValuePair>>();
        mongoCollectionMock.FindOneAndReplaceAsync(
                Arg.Any<FilterDefinition<SerializedKeyValuePair>>(),
                Arg.Any<SerializedKeyValuePair>(),
                Arg.Any<FindOneAndReplaceOptions<SerializedKeyValuePair>>()
            )
            .Returns(new SerializedKeyValuePair());
        _mongoDbStore = new MongoDbStore<SerializedKeyValuePair>(mongoCollectionMock);
    }

    [Fact(DisplayName = "When saving a SerializedKeyValuePair document, don't throw an exception of missing ID property")]
    public async Task SaveAsync_WithSerializedKeyValuePairDocument_DoesNotThrowException()
    {
        var mongoKeyValueStore = new MongoKeyValueStore(_mongoDbStore);
        var keyValuePair = new SerializedKeyValuePair();

        var exception = await Record.ExceptionAsync(
            async () => await mongoKeyValueStore.SaveAsync(keyValuePair, default));

        Assert.Null(exception);
    }
}