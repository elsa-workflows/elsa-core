using Elsa.Common.Entities;
using Elsa.Secrets.Contracts;

namespace Elsa.Secrets.Management;

/// <summary>
/// A secret provider that gets secrets from the configured <see cref="ISecretStore"/>.
/// </summary>
public class StoreSecretProvider(ISecretStore store, IDecryptor decryptor) : ISecretProvider
{
    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        var filter = new SecretFilter
        {
            Name = name,
            Status = SecretStatus.Active
        };
        var orderBy = new SecretOrder<int>(x => x.Version, OrderDirection.Descending);
        var secret = await store.FindAsync(filter, orderBy, cancellationToken);

        if (secret == null)
            return null;
        
        var encryptedValue = secret.EncryptedValue;
        return await decryptor.DecryptAsync(encryptedValue, cancellationToken);
    }
}