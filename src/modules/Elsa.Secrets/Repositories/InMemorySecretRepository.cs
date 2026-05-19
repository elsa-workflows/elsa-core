using System.Collections.Concurrent;

namespace Elsa.Secrets.Repositories;

public class InMemorySecretRepository : ISecretRepository
{
    private readonly ConcurrentDictionary<string, Secret> _secrets = new(StringComparer.OrdinalIgnoreCase);

    public Task<bool> ExistsAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_secrets.ContainsKey(normalizedName));
    }

    public Task<Secret?> GetAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        _secrets.TryGetValue(normalizedName, out var secret);
        return Task.FromResult(secret);
    }

    public Task<IReadOnlyCollection<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Secret>>(_secrets.Values.ToList());
    }

    public Task AddAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        if (!_secrets.TryAdd(secret.Name, secret))
            throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

        return Task.CompletedTask;
    }

    public Task<bool> TryAddOrReplaceDeletedAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (!_secrets.TryGetValue(secret.Name, out var existingSecret))
                return Task.FromResult(_secrets.TryAdd(secret.Name, secret));

            if (existingSecret.Status != SecretStatus.Deleted)
                return Task.FromResult(false);

            if (_secrets.TryUpdate(secret.Name, secret, existingSecret))
                return Task.FromResult(true);
        }
    }

    public Task SaveAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        _secrets[secret.Name] = secret;
        return Task.CompletedTask;
    }
}
