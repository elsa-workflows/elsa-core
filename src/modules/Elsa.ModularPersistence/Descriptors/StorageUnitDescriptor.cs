namespace Elsa.ModularPersistence.Descriptors;

/// <summary>
/// Describes a provider-neutral storage unit.
/// </summary>
public sealed record StorageUnitDescriptor
{
    public StorageUnitDescriptor(
        string name,
        IEnumerable<StorageFieldDescriptor> fields,
        IEnumerable<StorageKeyDescriptor>? keys = null,
        IEnumerable<StorageIndexDescriptor>? indexes = null,
        PhysicalizationIntent physicalizationIntent = PhysicalizationIntent.PortableDocument,
        StorageUnitKind kind = StorageUnitKind.Document)
    {
        DescriptorValidation.EnsureEnumValue(kind, nameof(kind));
        DescriptorValidation.EnsureEnumValue(physicalizationIntent, nameof(physicalizationIntent));

        Name = DescriptorValidation.RequireName(name, nameof(name));
        Kind = kind;
        Fields = DescriptorValidation.RequireNonEmptyList(fields, nameof(fields));
        Keys = (keys ?? []).ToArray();
        Indexes = (indexes ?? []).ToArray();
        PhysicalizationIntent = physicalizationIntent;

        ValidateFields();
        ValidateKeys();
        ValidateIndexes();
    }

    public string Name { get; }

    public StorageUnitKind Kind { get; }

    public IReadOnlyList<StorageFieldDescriptor> Fields { get; }

    public IReadOnlyList<StorageKeyDescriptor> Keys { get; }

    public IReadOnlyList<StorageIndexDescriptor> Indexes { get; }

    public PhysicalizationIntent PhysicalizationIntent { get; }

    private void ValidateFields()
    {
        DescriptorValidation.EnsureUnique(Fields, x => x.Name, "Field names must be unique.", "fields");
    }

    private void ValidateKeys()
    {
        DescriptorValidation.EnsureUnique(Keys, x => x.Name, "Key names must be unique.", nameof(Keys));
        var fieldNames = Fields.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var key in Keys)
        {
            foreach (var fieldName in key.FieldNames)
            {
                if (!fieldNames.Contains(fieldName))
                    throw new ArgumentException($"Key '{key.Name}' references unknown field '{fieldName}'.", nameof(Keys));
            }
        }

        if (Keys.Count(x => x.Kind == StorageKeyKind.Primary) > 1)
            throw new ArgumentException($"Storage unit '{Name}' declares more than one primary key.", nameof(Keys));
    }

    private void ValidateIndexes()
    {
        DescriptorValidation.EnsureUnique(Indexes, x => x.Name, "Index names must be unique.", nameof(Indexes));
        var fieldNames = Fields.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var index in Indexes)
        {
            foreach (var fieldName in index.Fields.Select(x => x.FieldName))
            {
                if (!fieldNames.Contains(fieldName))
                    throw new ArgumentException($"Index '{index.Name}' references unknown field '{fieldName}'.", nameof(Indexes));
            }
        }
    }
}
