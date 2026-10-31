using Elsa.Persistence.VNext.Contracts;

namespace Elsa.Persistence.VNext.Document;

public class DocumentDatabasePlanner : IPersistenceSchemaPlanner<DocumentDatabasePlan>
{
    public DocumentDatabasePlan Plan(PersistenceSchema schema)
    {
        var collections = schema.StorageUnits.Select(PlanCollection).ToList();
        return new DocumentDatabasePlan(collections);
    }

    private static DocumentCollection PlanCollection(PersistenceStorageUnit storageUnit)
    {
        var fields = storageUnit.Fields
            .Select(field => new DocumentField(field.Name, field.Type, field.IsNullable))
            .ToList();
        var indexes = storageUnit.Indexes
            .Select(index => new DocumentIndex(index.Name, storageUnit.Name, storageUnit.Namespace, index.Columns, index.IsUnique))
            .ToList();
        var keyFields = storageUnit.Key?.Columns ?? [];

        return new DocumentCollection(storageUnit.Name, storageUnit.Namespace, fields, keyFields, indexes);
    }
}
