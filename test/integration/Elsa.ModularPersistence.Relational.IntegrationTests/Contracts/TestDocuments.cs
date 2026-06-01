using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;

namespace Elsa.ModularPersistence.Relational.IntegrationTests.Contracts;

public static class TestDocuments
{
    public static DocumentEnvelope CreateSecret(
        long version,
        string data = """{"TenantId":"tenant-a","Name":"Alpha","Priority":1,"ExpiresAt":null}""",
        IReadOnlyDictionary<string, string>? metadata = null) =>
        CreateSecret("secret-1", version, data, metadata);

    public static DocumentEnvelope CreateSecret(
        string id,
        long version,
        string data,
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            id,
            "Secrets",
            "tenant-a",
            version,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 1, 1, 0, 0, (int)version, TimeSpan.Zero),
            data,
            metadata);

    public static StorageManifestDescriptor CreateManifest() =>
        new(
            "sample.secrets",
            new StorageManifestVersion(1),
            [
                new StorageUnitDescriptor(
                    "Secrets",
                    [
                        new StorageFieldDescriptor("Id", StorageFieldType.String, true),
                        new StorageFieldDescriptor("TenantId", StorageFieldType.String),
                        new StorageFieldDescriptor("Name", StorageFieldType.String, true),
                        new StorageFieldDescriptor("Priority", StorageFieldType.Int32),
                        new StorageFieldDescriptor("ExpiresAt", StorageFieldType.DateTimeOffset)
                    ],
                    [
                        new StorageKeyDescriptor("PK_Secrets", ["Id"])
                    ],
                    [
                        new StorageIndexDescriptor("IX_Secrets_TenantId", [new StorageIndexFieldDescriptor("TenantId")]),
                        new StorageIndexDescriptor("IX_Secrets_Name", [new StorageIndexFieldDescriptor("Name")]),
                        new StorageIndexDescriptor("IX_Secrets_Priority", [new StorageIndexFieldDescriptor("Priority")]),
                        new StorageIndexDescriptor("IX_Secrets_ExpiresAt", [new StorageIndexFieldDescriptor("ExpiresAt")])
                    ])
            ]);
}
