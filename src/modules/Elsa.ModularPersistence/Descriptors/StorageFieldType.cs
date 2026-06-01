namespace Elsa.ModularPersistence.Descriptors;

/// <summary>
/// Provider-neutral field types used by manifests, keys, and indexes.
/// </summary>
public enum StorageFieldType
{
    String = 0,
    Boolean = 1,
    Int32 = 2,
    Int64 = 3,
    Decimal = 4,
    DateTimeOffset = 5,
    Guid = 6,
    Binary = 7,
    Json = 8
}
