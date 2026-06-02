namespace Elsa.ModularPersistence.Descriptors;

/// <summary>
/// Describes a field participating in an index.
/// </summary>
public sealed record StorageIndexFieldDescriptor
{
    public StorageIndexFieldDescriptor(string fieldName, StorageIndexSortOrder sortOrder = StorageIndexSortOrder.Ascending)
    {
        DescriptorValidation.EnsureEnumValue(sortOrder, nameof(sortOrder));

        FieldName = DescriptorValidation.RequireName(fieldName, nameof(fieldName));
        SortOrder = sortOrder;
    }

    public string FieldName { get; }

    public StorageIndexSortOrder SortOrder { get; }
}
