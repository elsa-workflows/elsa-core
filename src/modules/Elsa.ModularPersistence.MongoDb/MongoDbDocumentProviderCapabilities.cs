using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.MongoDb;

/// <summary>
/// Provides MongoDB modular persistence capabilities.
/// </summary>
public static class MongoDbDocumentProviderCapabilities
{
    public static ProviderCapabilities Value { get; } = new(
        [StorageUnitKind.Document],
        Enum.GetValues<StorageFieldType>(),
        [
            PhysicalizationIntent.PortableDocument,
            PhysicalizationIntent.OptimizedIndexes
        ]);
}
