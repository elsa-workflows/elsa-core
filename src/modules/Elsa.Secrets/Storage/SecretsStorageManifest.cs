using Elsa.ModularPersistence.Descriptors;

namespace Elsa.Secrets.Storage;

public static class SecretsStorageManifest
{
    public const string SchemaName = "elsa.secrets";
    public const string SecretsStorageUnitName = "Secrets";

    public static StorageManifestDescriptor Create() =>
        new(
            SchemaName,
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    SecretsStorageUnitName,
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Name", StorageFieldType.String, true),
                        new StorageFieldDescriptor("DisplayName", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Description", StorageFieldType.String),
                        new StorageFieldDescriptor("TypeName", StorageFieldType.String, true),
                        new StorageFieldDescriptor("StoreName", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Scope", StorageFieldType.String),
                        new StorageFieldDescriptor("Status", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Tags", StorageFieldType.Json),
                        new StorageFieldDescriptor("CreatedAt", StorageFieldType.DateTimeOffset, true),
                        new StorageFieldDescriptor("UpdatedAt", StorageFieldType.DateTimeOffset),
                        new StorageFieldDescriptor("CurrentVersion", StorageFieldType.Int32),
                        new StorageFieldDescriptor("CurrentVersionExpiresAt", StorageFieldType.DateTimeOffset),
                        new StorageFieldDescriptor("Versions", StorageFieldType.Json, true)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Secrets", ["Id"]),
                        new StorageKeyDescriptor("UK_Secrets_Name", ["Name"], StorageKeyKind.Unique)
                    ],
                    [
                        new StorageIndexDescriptor("UX_Secrets_Name", [new StorageIndexFieldDescriptor("Name")], true),
                        new StorageIndexDescriptor("IX_Secrets_Status_Name", [new StorageIndexFieldDescriptor("Status"), new StorageIndexFieldDescriptor("Name")]),
                        new StorageIndexDescriptor("IX_Secrets_TypeName_Name", [new StorageIndexFieldDescriptor("TypeName"), new StorageIndexFieldDescriptor("Name")]),
                        new StorageIndexDescriptor("IX_Secrets_StoreName_Name", [new StorageIndexFieldDescriptor("StoreName"), new StorageIndexFieldDescriptor("Name")]),
                        new StorageIndexDescriptor("IX_Secrets_Scope_Name", [new StorageIndexFieldDescriptor("Scope"), new StorageIndexFieldDescriptor("Name")])
                    ])
            ]);
}
