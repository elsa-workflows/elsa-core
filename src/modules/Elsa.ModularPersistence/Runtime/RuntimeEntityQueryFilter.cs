using Elsa.ModularPersistence.Queries;

namespace Elsa.ModularPersistence.Runtime;

public sealed record RuntimeEntityQueryFilter
{
    public RuntimeEntityQueryFilter(string fieldName, DocumentQueryFilterOperator @operator, IEnumerable<DocumentQueryValue>? values = null, string? indexName = null)
    {
        FieldName = fieldName;
        Operator = @operator;
        Values = (values ?? []).ToArray();
        IndexName = indexName;
    }

    public string FieldName { get; }

    public DocumentQueryFilterOperator Operator { get; }

    public IReadOnlyList<DocumentQueryValue> Values { get; }

    public string? IndexName { get; }
}
