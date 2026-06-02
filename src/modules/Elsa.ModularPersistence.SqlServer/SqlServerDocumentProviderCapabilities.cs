using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.SqlServer;

public static class SqlServerDocumentProviderCapabilities
{
    public static ProviderCapabilities Create(bool useOptimizedIndexes = false) =>
        new(
            [StorageUnitKind.Document],
            Enum.GetValues<StorageFieldType>(),
            useOptimizedIndexes
                ? [PhysicalizationIntent.PortableDocument, PhysicalizationIntent.OptimizedIndexes]
                : [PhysicalizationIntent.PortableDocument]);
}
