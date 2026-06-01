namespace Elsa.ModularPersistence.Descriptors;

/// <summary>
/// Describes a provider-neutral key over one or more storage fields.
/// </summary>
public sealed record StorageKeyDescriptor
{
    public StorageKeyDescriptor(string name, IEnumerable<string> fieldNames, StorageKeyKind kind = StorageKeyKind.Primary)
    {
        DescriptorValidation.EnsureEnumValue(kind, nameof(kind));

        Name = DescriptorValidation.RequireName(name, nameof(name));
        FieldNames = DescriptorValidation.RequireNonEmptyNames(fieldNames, nameof(fieldNames));
        Kind = kind;

        DescriptorValidation.EnsureUnique(FieldNames, x => x, "Key fields must be unique.", nameof(fieldNames));
    }

    public string Name { get; }

    public IReadOnlyList<string> FieldNames { get; }

    public StorageKeyKind Kind { get; }
}
