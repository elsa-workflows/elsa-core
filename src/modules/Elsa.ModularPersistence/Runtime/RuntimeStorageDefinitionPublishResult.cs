using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.Runtime;

public sealed record RuntimeStorageDefinitionPublishResult(
    RuntimeStorageDefinition Definition,
    StorageManifestDescriptor? Manifest,
    IReadOnlyCollection<StorageManifestValidationError> Errors)
{
    public bool Succeeded => Errors.Count == 0;

    public static RuntimeStorageDefinitionPublishResult Success(RuntimeStorageDefinition definition, StorageManifestDescriptor manifest) =>
        new(definition, manifest, []);

    public static RuntimeStorageDefinitionPublishResult Failed(RuntimeStorageDefinition definition, IEnumerable<StorageManifestValidationError> errors) =>
        new(definition, null, errors.ToArray());
}
