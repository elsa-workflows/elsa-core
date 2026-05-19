namespace Elsa.Secrets.Stores;

public class EncryptedSecretStore(ISecretValueProtector protector) : ISecretStore
{
    private const string ProtectedValueKey = "protectedValue";

    public string Name => SecretStoreNames.Encrypted;

    public SecretStoreDescriptor Descriptor { get; } = new(
        SecretStoreNames.Encrypted,
        "Elsa Encrypted Store",
        "Stores values in Elsa-managed encrypted payloads.",
        SecretStoreCapabilities.Read | SecretStoreCapabilities.Write | SecretStoreCapabilities.Delete | SecretStoreCapabilities.Test | SecretStoreCapabilities.ExportEncrypted | SecretStoreCapabilities.Versioned,
        false);

    public Task<SecretPayload> WriteAsync(Secret secret, SecretVersion version, SecretPayload payload, CancellationToken cancellationToken = default)
    {
        if (payload.Value == null)
            throw new InvalidOperationException("A value is required for encrypted secrets.");

        var protectedPayload = new SecretPayload();
        protectedPayload.Metadata[ProtectedValueKey] = protector.Protect(payload.Value);

        foreach (var item in payload.Metadata.Where(x => !string.Equals(x.Key, ProtectedValueKey, StringComparison.OrdinalIgnoreCase)))
            protectedPayload.Metadata[item.Key] = item.Value;

        return Task.FromResult(protectedPayload);
    }

    public Task<SecretPayload?> ReadAsync(Secret secret, SecretVersion version, CancellationToken cancellationToken = default)
    {
        if (!version.Payload.Metadata.TryGetValue(ProtectedValueKey, out var protectedValue))
            return Task.FromResult<SecretPayload?>(null);

        return Task.FromResult<SecretPayload?>(SecretPayload.FromValue(protector.Unprotect(protectedValue)));
    }

    public Task DeleteAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        foreach (var version in secret.Versions)
            version.Payload.Metadata.Remove(ProtectedValueKey);

        return Task.CompletedTask;
    }

    public async Task<bool> TestAsync(Secret secret, SecretVersion version, CancellationToken cancellationToken = default)
    {
        var payload = await ReadAsync(secret, version, cancellationToken);
        return payload?.Value != null;
    }
}
