using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Validation;

namespace Elsa.ModularPersistence.PostgreSql;

public static class PostgreSqlDocumentProviderCapabilities
{
    public static ProviderCapabilities Create(bool useOptimizedJsonbIndexes = false) =>
        new(
            [StorageUnitKind.Document],
            Enum.GetValues<StorageFieldType>(),
            useOptimizedJsonbIndexes
                ? [PhysicalizationIntent.PortableDocument, PhysicalizationIntent.OptimizedIndexes]
                : [PhysicalizationIntent.PortableDocument]);
}
