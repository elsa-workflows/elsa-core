using Elsa.Persistence.VNext.Document;

namespace Elsa.Persistence.VNext.MongoDb;

public record MongoDbCollectionPlan(
    string CollectionName,
    DocumentCollection Collection,
    IReadOnlyList<MongoDbIndexPlan> Indexes);
