namespace Elsa.ModularPersistence.Descriptors;

/// <summary>
/// Describes a provider-neutral field that can participate in keys and indexes.
/// </summary>
public sealed record StorageFieldDescriptor
{
    public StorageFieldDescriptor(string name, StorageFieldType type, bool isRequired = false)
    {
        DescriptorValidation.EnsureEnumValue(type, nameof(type));

        Name = DescriptorValidation.RequireName(name, nameof(name));
        Type = type;
        IsRequired = isRequired;
    }

    public string Name { get; }

    public StorageFieldType Type { get; }

    public bool IsRequired { get; }
}
