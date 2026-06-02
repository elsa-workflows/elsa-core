using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Runtime;

public sealed record RuntimeStorageFieldDefinition(string Name, StorageFieldType Type, bool IsRequired = false);
