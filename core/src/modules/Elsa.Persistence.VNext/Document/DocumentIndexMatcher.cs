namespace Elsa.Persistence.VNext.Document;

public static class DocumentIndexMatcher
{
    public static DocumentIndex FindMatchingIndex(DocumentCollection collection, DocumentQuery query)
    {
        var filterFields = query.Filters.Keys.Order(StringComparer.Ordinal).ToArray();
        var index = collection.Indexes.SingleOrDefault(x => x.Fields.Order(StringComparer.Ordinal).SequenceEqual(filterFields, StringComparer.Ordinal));

        if (index is null)
            throw new DocumentQueryNotIndexedException(query.StorageUnit, filterFields);

        return index;
    }
}
