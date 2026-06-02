using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Runtime;

public sealed record RuntimeStorageDefinition
{
    public RuntimeStorageDefinition(
        string id,
        string schemaName,
        string storageUnitName,
        IEnumerable<RuntimeStorageFieldDefinition> fields,
        IEnumerable<RuntimeStorageIndexDefinition>? indexes = null,
        IEnumerable<string>? requiredPermissions = null,
        StorageManifestVersion? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id;
        SchemaName = schemaName;
        StorageUnitName = storageUnitName;
        Version = version ?? new StorageManifestVersion(1);
        Fields = fields?.ToArray() ?? throw new ArgumentNullException(nameof(fields));
        Indexes = (indexes ?? []).ToArray();
        RequiredPermissions = (requiredPermissions ?? []).ToArray();
    }

    public string Id { get; }

    public string SchemaName { get; }

    public StorageManifestVersion Version { get; }

    public string StorageUnitName { get; }

    public IReadOnlyList<RuntimeStorageFieldDefinition> Fields { get; }

    public IReadOnlyList<RuntimeStorageIndexDefinition> Indexes { get; }

    public IReadOnlyList<string> RequiredPermissions { get; }

    public RuntimeStorageDefinitionState State { get; init; } = RuntimeStorageDefinitionState.Draft;
}
