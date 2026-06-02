using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Queries;

/// <summary>
/// Describes a filter against an explicitly declared document index field.
/// </summary>
public sealed record DocumentQueryFilter
{
    public DocumentQueryFilter(string indexName, string fieldName, DocumentQueryFilterOperator @operator, IEnumerable<DocumentQueryValue>? values = null)
    {
        DescriptorValidation.EnsureEnumValue(@operator, nameof(@operator));

        IndexName = DescriptorValidation.RequireName(indexName, nameof(indexName));
        FieldName = DescriptorValidation.RequireName(fieldName, nameof(fieldName));
        Operator = @operator;
        Values = (values ?? []).ToArray();

        ValidateValueCount();
    }

    public string IndexName { get; }

    public string FieldName { get; }

    public DocumentQueryFilterOperator Operator { get; }

    public IReadOnlyList<DocumentQueryValue> Values { get; }

    public static DocumentQueryFilter Equal(string indexName, string fieldName, DocumentQueryValue value) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.Equals, [value]);

    public static DocumentQueryFilter NotEqual(string indexName, string fieldName, DocumentQueryValue value) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.NotEquals, [value]);

    public static DocumentQueryFilter In(string indexName, string fieldName, IEnumerable<DocumentQueryValue> values) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.In, values);

    public static DocumentQueryFilter GreaterThan(string indexName, string fieldName, DocumentQueryValue value) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.GreaterThan, [value]);

    public static DocumentQueryFilter GreaterThanOrEqual(string indexName, string fieldName, DocumentQueryValue value) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.GreaterThanOrEqual, [value]);

    public static DocumentQueryFilter LessThan(string indexName, string fieldName, DocumentQueryValue value) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.LessThan, [value]);

    public static DocumentQueryFilter LessThanOrEqual(string indexName, string fieldName, DocumentQueryValue value) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.LessThanOrEqual, [value]);

    public static DocumentQueryFilter Between(string indexName, string fieldName, DocumentQueryValue lower, DocumentQueryValue upper) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.Between, [lower, upper]);

    public static DocumentQueryFilter StartsWith(string indexName, string fieldName, DocumentQueryValue value) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.StartsWith, [value]);

    public static DocumentQueryFilter IsNull(string indexName, string fieldName) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.IsNull);

    public static DocumentQueryFilter IsNotNull(string indexName, string fieldName) =>
        new(indexName, fieldName, DocumentQueryFilterOperator.IsNotNull);

    private void ValidateValueCount()
    {
        int? expectedCount = Operator switch
        {
            DocumentQueryFilterOperator.IsNull or DocumentQueryFilterOperator.IsNotNull => 0,
            DocumentQueryFilterOperator.In => null,
            DocumentQueryFilterOperator.Between => 2,
            _ => 1
        };

        if (expectedCount is null && Values.Count == 0)
            throw new ArgumentException("The filter operator requires at least one value.", nameof(Values));

        if (expectedCount is not null && Values.Count != expectedCount)
            throw new ArgumentException($"The filter operator requires {expectedCount} value(s).", nameof(Values));
    }
}
