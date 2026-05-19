using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Stores;

public class ConfigurationSecretStore(IConfiguration configuration, IOptions<SecretsOptions> options) : ISecretStore
{
    private const string ConfigurationKeyMetadataName = "configurationKey";

    public string Name => SecretStoreNames.Configuration;

    public SecretStoreDescriptor Descriptor { get; } = new(
        SecretStoreNames.Configuration,
        "Configuration",
        "Reads values from application configuration without storing the value in Elsa.",
        SecretStoreCapabilities.Read | SecretStoreCapabilities.Test,
        true);

    public Task<SecretPayload> WriteAsync(Secret secret, SecretVersion version, SecretPayload payload, CancellationToken cancellationToken = default)
    {
        if (!payload.Metadata.TryGetValue(ConfigurationKeyMetadataName, out var key) || string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("A configuration key is required.");

        return Task.FromResult(new SecretPayload { Metadata = new Dictionary<string, string>(payload.Metadata, StringComparer.OrdinalIgnoreCase) });
    }

    public Task<SecretPayload?> ReadAsync(Secret secret, SecretVersion version, CancellationToken cancellationToken = default)
    {
        if (!version.Payload.Metadata.TryGetValue(ConfigurationKeyMetadataName, out var key) || string.IsNullOrWhiteSpace(key))
            return Task.FromResult<SecretPayload?>(null);

        var configuredValue = configuration[$"{options.Value.ConfigurationSectionName}:{key}"] ?? configuration[key];
        return configuredValue == null ? Task.FromResult<SecretPayload?>(null) : Task.FromResult<SecretPayload?>(SecretPayload.FromValue(configuredValue));
    }

    public Task DeleteAsync(Secret secret, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<bool> TestAsync(Secret secret, SecretVersion version, CancellationToken cancellationToken = default)
    {
        var payload = await ReadAsync(secret, version, cancellationToken);
        return payload?.Value != null;
    }
}
