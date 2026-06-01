using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Queries;

/// <summary>
/// Describes a sort against an explicitly declared document index field.
/// </summary>
public sealed record DocumentQuerySort
{
    public DocumentQuerySort(string indexName, string fieldName, StorageIndexSortOrder sortOrder = StorageIndexSortOrder.Ascending)
    {
        DescriptorValidation.EnsureEnumValue(sortOrder, nameof(sortOrder));

        IndexName = DescriptorValidation.RequireName(indexName, nameof(indexName));
        FieldName = DescriptorValidation.RequireName(fieldName, nameof(fieldName));
        SortOrder = sortOrder;
    }

    public string IndexName { get; }

    public string FieldName { get; }

    public StorageIndexSortOrder SortOrder { get; }
}
