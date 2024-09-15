using Elsa.Common.Contracts;
using Elsa.Mediator.Contracts;
using Elsa.Secrets.Management.Notifications;
using Elsa.Workflows.Contracts;

namespace Elsa.Secrets.Management;

public class DefaultSecretManager(ISecretStore store, IEncryptor encryptor, ISecretUpdater secretUpdater, IIdentityGenerator identityGenerator, ISystemClock systemClock, INotificationSender notificationSender) : ISecretManager
{
    public async Task<Secret> CreateAsync(SecretInputModel input, CancellationToken cancellationToken = default)
    {
        var encryptedValue = await encryptor.EncryptAsync(input.Value, cancellationToken);
        var now = systemClock.UtcNow;
        var secret = new Secret
        {
            Id = identityGenerator.GenerateId(),
            SecretId = identityGenerator.GenerateId(),
            Name = input.Name.Trim(),
            Scope = input.Scope?.Trim(),
            Description = input.Description.Trim(),
            EncryptedValue = encryptedValue,
            ExpiresIn = input.ExpiresIn,
            ExpiresAt = now + input.ExpiresIn,
            Status = SecretStatus.Active,
            Version = 1,
            CreatedAt = now,
            UpdatedAt = now,
            IsLatest = true
        };
        
        await store.AddAsync(secret, cancellationToken);
        await notificationSender.SendAsync(new SecretCreated(secret), cancellationToken);

        return secret;
    }

    public async Task<Secret> UpdateAsync(Secret entity, SecretInputModel input, CancellationToken cancellationToken = default)
    {
        var updatedEntity = await secretUpdater.UpdateAsync(entity, input, cancellationToken);
        await notificationSender.SendAsync(new SecretUpdated(entity), cancellationToken);
        return updatedEntity;
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
}