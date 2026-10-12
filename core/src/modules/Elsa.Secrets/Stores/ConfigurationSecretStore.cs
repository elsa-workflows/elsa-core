using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Stores;

public class ConfigurationSecretStore(IConfiguration configuration, IOptions<SecretsOptions> options, ILogger<ConfigurationSecretStore>? logger) : ISecretStore
{
    public ConfigurationSecretStore(IConfiguration configuration, IOptions<SecretsOptions> options) : this(configuration, options, null)
    {
    }

    private const string ConfigurationKeyMetadataName = "configurationKey";

    public string Name => SecretStoreNames.Configuration;

    public SecretStoreDescriptor Descriptor { get; } = new(
        SecretStoreNames.Configuration,
        "Configuration",
        "Reads values from the configured secrets section of application configuration without storing the value in Elsa.",
        SecretStoreCapabilities.Read | SecretStoreCapabilities.Write | SecretStoreCapabilities.Test,
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

        var sectionName = options.Value.ConfigurationSectionName;
        var configuredValue = configuration.GetSection(sectionName)[key];

        if (configuredValue == null)
            logger?.LogDebug("Configuration secret key {ConfigurationKey} was not found under section {SectionName}.", key, sectionName);

        return configuredValue == null ? Task.FromResult<SecretPayload?>(null) : Task.FromResult<SecretPayload?>(SecretPayload.FromValue(configuredValue));
    }

    public Task DeleteAsync(Secret secret, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task<bool> TestAsync(Secret secret, SecretVersion version, CancellationToken cancellationToken = default)
    {
        var payload = await ReadAsync(secret, version, cancellationToken);
        return payload?.Value != null;
    }
}
