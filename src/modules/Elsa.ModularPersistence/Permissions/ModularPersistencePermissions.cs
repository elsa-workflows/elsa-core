namespace Elsa.ModularPersistence.Permissions;

public static class ModularPersistencePermissions
{
    public const string ReadDiagnostics = "read:diagnostics:modular-persistence";
    public const string ReadRuntimeStorageDefinitions = "read:modular-persistence:runtime-storage-definitions";
    public const string WriteRuntimeStorageDefinitions = "write:modular-persistence:runtime-storage-definitions";
    public const string PublishRuntimeStorageDefinitions = "publish:modular-persistence:runtime-storage-definitions";
    public const string DeleteRuntimeStorageDefinitions = "delete:modular-persistence:runtime-storage-definitions";
    public const string ReadRuntimeEntities = "read:modular-persistence:runtime-entities";
    public const string WriteRuntimeEntities = "write:modular-persistence:runtime-entities";
    public const string DeleteRuntimeEntities = "delete:modular-persistence:runtime-entities";
}
