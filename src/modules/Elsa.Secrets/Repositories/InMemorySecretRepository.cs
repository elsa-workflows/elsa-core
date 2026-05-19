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
        return Task.FromResult(secret == null ? null : Clone(secret));
    }

    public Task<IReadOnlyCollection<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyCollection<Secret>>(_secrets.Values.Select(Clone).ToList());
    }

    public Task AddAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        if (!_secrets.TryAdd(secret.Name, Clone(secret)))
            throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

        return Task.CompletedTask;
    }

    public Task<bool> TryAddOrReplaceDeletedAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        var secretClone = Clone(secret);

        while (true)
        {
            if (!_secrets.TryGetValue(secret.Name, out var existingSecret))
                return Task.FromResult(_secrets.TryAdd(secret.Name, secretClone));

            if (existingSecret.Status != SecretStatus.Deleted)
                return Task.FromResult(false);

            if (_secrets.TryUpdate(secret.Name, secretClone, existingSecret))
                return Task.FromResult(true);
        }
    }

    public Task SaveAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        _secrets[secret.Name] = Clone(secret);
        return Task.CompletedTask;
    }

    private static Secret Clone(Secret secret)
    {
        return new Secret
        {
            Id = secret.Id,
            Name = secret.Name,
            DisplayName = secret.DisplayName,
            Description = secret.Description,
            TypeName = secret.TypeName,
            StoreName = secret.StoreName,
            Scope = secret.Scope,
            Tags = secret.Tags.ToHashSet(StringComparer.OrdinalIgnoreCase),
            Status = secret.Status,
            CreatedAt = secret.CreatedAt,
            UpdatedAt = secret.UpdatedAt,
            Versions = secret.Versions.Select(Clone).ToList()
        };
    }

    private static SecretVersion Clone(SecretVersion version)
    {
        return new SecretVersion
        {
            Version = version.Version,
            Status = version.Status,
            CreatedAt = version.CreatedAt,
            ExpiresAt = version.ExpiresAt,
            Payload = new SecretPayload
            {
                Value = version.Payload.Value,
                Metadata = new Dictionary<string, string>(version.Payload.Metadata, StringComparer.OrdinalIgnoreCase)
            }
        };
    }
}
