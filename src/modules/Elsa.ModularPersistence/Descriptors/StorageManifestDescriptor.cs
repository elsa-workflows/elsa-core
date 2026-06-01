namespace Elsa.ModularPersistence.Descriptors;

/// <summary>
/// Describes the storage intent owned by a module or runtime schema.
/// </summary>
public sealed record StorageManifestDescriptor
{
    public StorageManifestDescriptor(string schemaName, StorageManifestVersion version, IEnumerable<StorageUnitDescriptor> storageUnits)
    {
        SchemaName = DescriptorValidation.RequireName(schemaName, nameof(schemaName));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        StorageUnits = DescriptorValidation.RequireNonEmptyList(storageUnits, nameof(storageUnits));

        DescriptorValidation.EnsureUnique(StorageUnits, x => x.Name, "Storage unit names must be unique.", nameof(storageUnits));
    }

    public string SchemaName { get; }

    public StorageManifestVersion Version { get; }

    public IReadOnlyList<StorageUnitDescriptor> StorageUnits { get; }
}
