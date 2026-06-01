using Elsa.ModularPersistence.Descriptors;

namespace Elsa.ModularPersistence.Services;

public sealed class StorageManifestMaterializationException : InvalidOperationException
{
    public StorageManifestMaterializationException(string providerName, StorageManifestDescriptor manifest, int attemptCount, Exception innerException)
        : base(CreateMessage(providerName, manifest, attemptCount, innerException), innerException)
    {
        ProviderName = providerName;
        SchemaName = manifest.SchemaName;
        Version = manifest.Version.ToString();
        AttemptCount = attemptCount;
    }

    public string ProviderName { get; }

    public string SchemaName { get; }

    public string Version { get; }

    public int AttemptCount { get; }

    private static string CreateMessage(string providerName, StorageManifestDescriptor manifest, int attemptCount, Exception innerException) =>
        $"Failed to materialize modular persistence manifest '{manifest.SchemaName}' version '{manifest.Version}' with provider '{providerName}' after {attemptCount} attempt(s). Provider materializers must run in explicit transactions and use idempotent schema/history operations so failed startup can be retried safely. Last error: {innerException.Message}";
}
