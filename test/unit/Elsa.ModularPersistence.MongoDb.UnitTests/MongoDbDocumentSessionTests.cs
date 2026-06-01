using System.Reflection;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.MongoDb.Services;
using MongoDB.Bson;

namespace Elsa.ModularPersistence.MongoDb.UnitTests;

public class MongoDbDocumentSessionTests
{
    [Fact]
    public void CreateMongoDocumentStoresRoundTripJsonAndParsedBson()
    {
        var envelope = CreateDocument();

        var document = CreateMongoDocument(envelope);

        Assert.Equal("""{"Name":"Alpha","Priority":42}""", document["DataJson"].AsString);
        Assert.Equal("Alpha", document["Data"].AsBsonDocument["Name"].AsString);
        Assert.Equal(42, document["Data"].AsBsonDocument["Priority"].ToInt32());
        Assert.Equal("test", document["Metadata"].AsBsonDocument["source"].AsString);
    }

    [Fact]
    public void ReadDocumentRestoresEnvelopeFromMongoDocument()
    {
        var envelope = CreateDocument();
        var document = CreateMongoDocument(envelope);

        var restored = ReadDocument(document);

        Assert.Equal(envelope.Id, restored.Id);
        Assert.Equal(envelope.Type, restored.Type);
        Assert.Equal(envelope.TenantId, restored.TenantId);
        Assert.Equal(envelope.Version, restored.Version);
        Assert.Equal(envelope.Data, restored.Data);
        Assert.Equal("test", restored.Metadata["source"]);
    }

    private static DocumentEnvelope CreateDocument() =>
        new(
            "secret-1",
            "Secrets",
            "tenant-a",
            1,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 0, 0, 1, TimeSpan.Zero),
            """{"Name":"Alpha","Priority":42}""",
            new Dictionary<string, string> { ["source"] = "test" });

    private static BsonDocument CreateMongoDocument(DocumentEnvelope document)
    {
        var method = typeof(MongoDbDocumentSession).GetMethod("CreateMongoDocument", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (BsonDocument)method.Invoke(null, [document])!;
    }

    private static DocumentEnvelope ReadDocument(BsonDocument document)
    {
        var method = typeof(MongoDbDocumentSession).GetMethod("ReadDocument", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (DocumentEnvelope)method.Invoke(null, [document])!;
    }
}
