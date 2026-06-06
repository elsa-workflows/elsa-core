using Elsa.Persistence.VNext.Contracts;
using Elsa.Persistence.VNext.Document;

namespace Elsa.Persistence.VNext.MongoDb;

public class MongoDbDatabasePlanner : IPersistenceSchemaPlanner<MongoDbDatabasePlan>
{
    private readonly DocumentDatabasePlanner _documentPlanner = new();

    public MongoDbDatabasePlan Plan(PersistenceSchema schema)
    {
        var documentPlan = _documentPlanner.Plan(schema);
        var collections = documentPlan.Collections.Select(PlanCollection).ToList();
        return new MongoDbDatabasePlan(collections);
    }

    private static MongoDbCollectionPlan PlanCollection(DocumentCollection collection)
    {
        var collectionName = NormalizeName(collection.Namespace, collection.Name);
        var indexes = collection.Indexes
            .Select(index => new MongoDbIndexPlan(index.Name, index.Fields.Select(field => $"IndexValues.{field}").ToList(), index.IsUnique))
            .ToList();

        return new MongoDbCollectionPlan(collectionName, collection, indexes);
    }

    private static string NormalizeName(params string?[] segments)
    {
        var value = string.Join("_", segments.Where(x => !string.IsNullOrWhiteSpace(x)));
        var chars = value.Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_').ToArray();
        return new string(chars);
    }
}
