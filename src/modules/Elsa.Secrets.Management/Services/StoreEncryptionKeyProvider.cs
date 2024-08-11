using Elsa.Common.Entities;
using Elsa.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Management;

[UsedImplicitly]
public class StoreEncryptionKeyProvider(ISecretStore store, IAlgorithmResolver algorithmResolver, IOptions<EncryptionKeyProviderOptions> options) : IEncryptionKeyProvider
{
    public Task<EncryptionKey> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var filter = new SecretFilter
        {
            Id = id,
            Type = SecretTypes.EncryptionKey,
            Status = SecretStatus.Active
        };

        return GetAsync(filter, cancellationToken);
    }

    private async Task<EncryptionKey> GetAsync(SecretFilter filter, CancellationToken cancellationToken = default)
    {
        var order = new SecretOrder<int>(x => x.Version, OrderDirection.Descending);
        var secret = await store.FindAsync(filter, order, cancellationToken);

        if (secret == null)
            throw new Exception($"No secret found for key {filter}");

        var encryptedKey = secret.EncryptedValue;
        var rootKey = options.Value.Key;
        var rootIV = options.Value.IV;
        var algorithmName = options.Value.Algorithm;
        var algorithm = await algorithmResolver.ResolveAsync(algorithmName, cancellationToken);
        var decryptedKey = algorithm.Decrypt(encryptedKey, rootKey, rootIV);

        return new EncryptionKey(secret.Id, decryptedKey, algorithmName);
    }
}