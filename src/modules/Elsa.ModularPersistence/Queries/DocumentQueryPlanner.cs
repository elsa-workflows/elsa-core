using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Diagnostics;

namespace Elsa.ModularPersistence.Queries;

/// <summary>
/// Validates document queries against storage manifests and provider query capabilities.
/// </summary>
public sealed class DocumentQueryPlanner
{
    public DocumentQueryPlan Plan(StorageManifestDescriptor manifest, DocumentQuery query, DocumentQueryCapabilities? capabilities = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(query);

        capabilities ??= DocumentQueryCapabilities.Portable;

        var diagnostics = new List<StoragePlanDiagnostic>();
        var referencedIndexes = new Dictionary<string, StorageIndexDescriptor>(StringComparer.Ordinal);
        var storageUnit = manifest.StorageUnits.SingleOrDefault(x => x.Name == query.DocumentType);

        if (storageUnit is null)
        {
            diagnostics.Add(Error("UnknownStorageUnit", $"Storage unit '{query.DocumentType}' is not declared.", "documentType"));
            return new DocumentQueryPlan(query, null, referencedIndexes.Values.ToArray(), diagnostics);
        }

        if (query.Filters.Count == 0)
            diagnostics.Add(Error("UnboundedQuery", "Document queries must include at least one declared-index filter.", "filters"));

        for (var i = 0; i < query.Filters.Count; i++)
            PlanFilter(storageUnit, query.Filters[i], i, capabilities, diagnostics, referencedIndexes);

        for (var i = 0; i < query.Sorts.Count; i++)
            PlanSort(storageUnit, query.Sorts[i], i, diagnostics, referencedIndexes);

        return new DocumentQueryPlan(query, storageUnit, referencedIndexes.Values.ToArray(), diagnostics);
    }

    private static void PlanFilter(
        StorageUnitDescriptor storageUnit,
        DocumentQueryFilter filter,
        int index,
        DocumentQueryCapabilities capabilities,
        ICollection<StoragePlanDiagnostic> diagnostics,
        IDictionary<string, StorageIndexDescriptor> referencedIndexes)
    {
        var path = $"filters[{index}]";
        if (!capabilities.SupportedOperators.Contains(filter.Operator))
            diagnostics.Add(Error("UnsupportedQueryOperator", $"Query operator '{filter.Operator}' is not supported by this provider.", $"{path}.operator"));

        var indexDescriptor = FindIndex(storageUnit, filter.IndexName, filter.FieldName, $"{path}.indexName", diagnostics);
        if (indexDescriptor is not null)
            referencedIndexes.TryAdd(indexDescriptor.Name, indexDescriptor);

        var field = storageUnit.Fields.SingleOrDefault(x => x.Name == filter.FieldName);
        if (field is null)
        {
            diagnostics.Add(Error("UnknownField", $"Field '{filter.FieldName}' is not declared by storage unit '{storageUnit.Name}'.", $"{path}.fieldName"));
            return;
        }

        for (var i = 0; i < filter.Values.Count; i++)
        {
            var value = filter.Values[i];
            if (value.Type != field.Type)
                diagnostics.Add(Error("QueryValueTypeMismatch", $"Query value type '{value.Type}' does not match field '{field.Name}' type '{field.Type}'.", $"{path}.values[{i}]"));
        }
    }

    private static void PlanSort(
        StorageUnitDescriptor storageUnit,
        DocumentQuerySort sort,
        int index,
        ICollection<StoragePlanDiagnostic> diagnostics,
        IDictionary<string, StorageIndexDescriptor> referencedIndexes)
    {
        var indexDescriptor = FindIndex(storageUnit, sort.IndexName, sort.FieldName, $"sorts[{index}].indexName", diagnostics);
        if (indexDescriptor is not null)
            referencedIndexes.TryAdd(indexDescriptor.Name, indexDescriptor);
    }

    private static StorageIndexDescriptor? FindIndex(
        StorageUnitDescriptor storageUnit,
        string indexName,
        string fieldName,
        string path,
        ICollection<StoragePlanDiagnostic> diagnostics)
    {
        var index = storageUnit.Indexes.SingleOrDefault(x => x.Name == indexName);
        if (index is null)
        {
            diagnostics.Add(Error("UnknownIndex", $"Index '{indexName}' is not declared by storage unit '{storageUnit.Name}'.", path));
            return null;
        }

        if (index.Fields.All(x => x.FieldName != fieldName))
            diagnostics.Add(Error("FieldNotInIndex", $"Field '{fieldName}' is not declared by index '{indexName}'.", path));

        return index;
    }

    private static StoragePlanDiagnostic Error(string code, string message, string path) =>
        new(StoragePlanDiagnosticSeverity.Error, code, message, path);
}
