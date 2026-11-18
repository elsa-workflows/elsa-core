using Elsa.Common.Multitenancy;
using Elsa.Secrets.Stores;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Services;

/// <summary>Re-encrypts retained versions in the current tenant using the configured current key.</summary>
/// <remarks>Invoke from offline maintenance with exclusive access to the backed-up repository.</remarks>
public sealed class SecretKeyRotation(
    ISecretRepository repository,
    IOptions<SecretsOptions> options,
    ITenantAccessor? tenantAccessor = null)
{
    /// <summary>
    /// Verifies all retained payloads first. A dry run returns the number requiring an update without writing.
    /// Re-running after successful or partial application is safe while the old key remains configured.
    /// </summary>
    public async Task<int> ReencryptAsync(bool dryRun = true, CancellationToken cancellationToken = default)
    {
        var protector = new DefaultSecretValueProtector(options);
        var tenantId = tenantAccessor?.Tenant?.Id;
        var pending = new List<(Secret Secret, List<(SecretVersion Version, string Ciphertext)> Updates)>();

        foreach (var secret in await repository.ListAsync(cancellationToken))
        {
            if (!string.Equals(secret.TenantId ?? "", tenantId ?? "", StringComparison.Ordinal) ||
                !string.Equals(secret.StoreName, SecretStoreNames.Encrypted, StringComparison.OrdinalIgnoreCase))
                continue;

            var updates = new List<(SecretVersion Version, string Ciphertext)>();
            foreach (var version in secret.Versions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!version.Payload.Metadata.TryGetValue(EncryptedSecretStore.ProtectedValueKey, out var payload))
                    continue;

                var replacement = protector.Reencrypt(payload);
                if (payload != replacement)
                    updates.Add((version, replacement));
            }

            if (updates.Count > 0)
                pending.Add((secret, updates));
        }

        if (!dryRun)
        {
            foreach (var (secret, updates) in pending)
            {
                foreach (var (version, ciphertext) in updates)
                    version.Payload.Metadata[EncryptedSecretStore.ProtectedValueKey] = ciphertext;

                await repository.SaveAsync(secret, cancellationToken);
            }
        }

        return pending.Sum(item => item.Updates.Count);
    }
}
