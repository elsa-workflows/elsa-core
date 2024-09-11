using Elsa.Mediator.Contracts;
using Elsa.Secrets.Management.Notifications;

namespace Elsa.Secrets.Management;

public class DefaultSecretManager(ISecretStore store, INotificationSender notificationSender) : ISecretManager
{
    public async Task AddAsync(Secret entity, CancellationToken cancellationToken = default)
    {
        await store.AddAsync(entity, cancellationToken);
        await notificationSender.SendAsync(new SecretCreated(entity), cancellationToken);
    }

    public async Task UpdateAsync(Secret entity, CancellationToken cancellationToken = default)
    {
        await store.UpdateAsync(entity, cancellationToken);
        await notificationSender.SendAsync(new SecretUpdated(entity), cancellationToken);
    }

    public Task<Secret?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return store.GetAsync(id, cancellationToken);
    }

    public Task<Secret?> FindAsync(SecretFilter filter, CancellationToken cancellationToken = default)
    {
        return store.FindAsync(filter, cancellationToken);
    }

    public Task<IEnumerable<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        return store.ListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Secret entity, CancellationToken cancellationToken = default)
    {
        await store.DeleteAsync(entity, cancellationToken);
        await notificationSender.SendAsync(new SecretDeleted(entity), cancellationToken);
    }

    public async Task<long> DeleteManyAsync(SecretFilter filter, CancellationToken cancellationToken = default)
    {
        var count = await store.DeleteManyAsync(filter, cancellationToken);
        await notificationSender.SendAsync(new SecretsDeletedInBulk(), cancellationToken);
        return count;
    }

    public async Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken)
    {
        const int maxAttempts = 100;
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            var name = $"Secret {++attempt}";
            var isUnique = await IsNameUniqueAsync(name, cancellationToken: cancellationToken);

            if (isUnique)
                return name;
        }

        throw new Exception($"Failed to generate a unique workflow name after {maxAttempts} attempts.");
    }

    public async Task<bool> IsNameUniqueAsync(string name, string? notId = null, CancellationToken cancellationToken = default)
    {
        var filter = new SecretFilter
        {
            Name = name,
            NotId = notId
        };
        return await FindAsync(filter, cancellationToken) == null;
    }
}