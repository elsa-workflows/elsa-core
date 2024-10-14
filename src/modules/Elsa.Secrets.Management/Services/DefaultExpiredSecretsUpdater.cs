using Elsa.Common;

namespace Elsa.Secrets.Management;

public class DefaultExpiredSecretsUpdater(ISecretStore store, ISystemClock systemClock) : IExpiredSecretsUpdater
{
    public async Task UpdateExpiredSecretsAsync(CancellationToken cancellationToken = default)
    {
        var now = systemClock.UtcNow;
        
        var filter = new SecretFilter
        {
            Status = SecretStatus.Active,
            ExpiresAtLessThan = now
        };
        
        var secrets = (await store.FindManyAsync(filter, cancellationToken)).ToList();
        
        foreach (var secret in secrets)
        {
            secret.Status = SecretStatus.Expired;
            secret.UpdatedAt = now;
            await store.UpdateAsync(secret, cancellationToken);
        }
    }
}