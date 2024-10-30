using Elsa.Common;
using Elsa.Mediator.Contracts;
using Elsa.Secrets.Management.Notifications;

namespace Elsa.Secrets.Management;

public class DefaultExpiredSecretsUpdater(ISecretStore store, IMediator mediator, ISystemClock systemClock) : IExpiredSecretsUpdater
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
            await mediator.SendAsync(new SecretExpired(secret), cancellationToken);
        }
    }
}