namespace Elsa.ModularPersistence.Descriptors;

/// <summary>
/// Describes a provider-neutral index over one or more storage fields.
/// </summary>
public sealed record StorageIndexDescriptor
{
    public StorageIndexDescriptor(
        string name,
        IEnumerable<StorageIndexFieldDescriptor> fields,
        bool isUnique = false,
        PhysicalizationIntent physicalizationIntent = PhysicalizationIntent.PortableDocument)
    {
        DescriptorValidation.EnsureEnumValue(physicalizationIntent, nameof(physicalizationIntent));

        Name = DescriptorValidation.RequireName(name, nameof(name));
        Fields = DescriptorValidation.RequireNonEmptyList(fields, nameof(fields));
        IsUnique = isUnique;
        PhysicalizationIntent = physicalizationIntent;

        DescriptorValidation.EnsureUnique(Fields, x => x.FieldName, "Index fields must be unique.", nameof(fields));
    }

    public string Name { get; }

    public IReadOnlyList<StorageIndexFieldDescriptor> Fields { get; }

    public bool IsUnique { get; }

    public PhysicalizationIntent PhysicalizationIntent { get; }
}
