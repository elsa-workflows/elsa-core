using Elsa.Common.Multitenancy;

namespace Elsa.Secrets.Repositories;

/// <summary>
/// An in-memory secret repository that mirrors the EF Core tenant contract.
/// </summary>
/// <remarks>
/// Secrets are keyed by their stable ID so two tenants can own the same case-insensitive name. Name
/// uniqueness and updates are resolved under the ambient tenant, while reads are filtered through
/// <see cref="TenantVisibility"/>. This is deliberately the same shape as the persisted contract rather
/// than a test-only global name dictionary.
/// </remarks>
public class InMemorySecretRepository(ITenantAccessor? tenantAccessor = null) : ISecretRepository
{
    private readonly Dictionary<string, Secret> _secrets = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    public Task<Secret?> GetAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var secret = FindVisible(normalizedName);
            return Task.FromResult(secret is null ? null : Clone(secret));
        }
    }

    public Task<IReadOnlyCollection<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            var secrets = _secrets.Values
                .Where(x => SecretRepositoryTenant.IsVisible(x, tenantAccessor))
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyCollection<Secret>>(secrets);
        }
    }

    public Task AddAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        SecretRepositoryTenant.Stamp(secret, tenantAccessor);

        lock (_sync)
        {
            if (FindVisible(secret.Name) is not null)
                throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

            EnsureIdAvailable(secret);
            _secrets.Add(secret.Id, Clone(secret));
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryAddOrReplaceDeletedAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        SecretRepositoryTenant.Stamp(secret, tenantAccessor);

        lock (_sync)
        {
            var existing = FindVisible(secret.Name);
            if (existing is not null)
            {
                if (existing.Status != SecretStatus.Deleted || !SecretRepositoryTenant.CanReplace(existing, secret, tenantAccessor))
                    return Task.FromResult(false);

                var replacement = Clone(secret);
                replacement.TenantId = existing.TenantId;
                _secrets.Remove(existing.Id);
                EnsureIdAvailable(replacement);
                _secrets.Add(replacement.Id, replacement);
                return Task.FromResult(true);
            }

            EnsureIdAvailable(secret);
            _secrets.Add(secret.Id, Clone(secret));
            return Task.FromResult(true);
        }
    }

    public Task SaveAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        SecretRepositoryTenant.Stamp(secret, tenantAccessor);

        lock (_sync)
        {
            var existing = FindVisible(secret.Name);
            if (existing is not null)
            {
                if (!SecretRepositoryTenant.CanReplace(existing, secret, tenantAccessor))
                    throw new InvalidOperationException($"A secret named '{secret.Name}' belongs to another tenant.");

                // EF updates the row found by name, retaining its primary key and tenant ownership.
                var replacement = Clone(secret);
                replacement.Id = existing.Id;
                replacement.TenantId = existing.TenantId;
                _secrets[existing.Id] = replacement;
                return Task.CompletedTask;
            }

            EnsureIdAvailable(secret);
            _secrets.Add(secret.Id, Clone(secret));
        }

        return Task.CompletedTask;
    }

    private Secret? FindVisible(string name) =>
        _secrets.Values.FirstOrDefault(x => SecretRepositoryTenant.IsVisible(x, tenantAccessor) && SecretRepositoryTenant.HasName(x, name));

    private void EnsureIdAvailable(Secret secret)
    {
        if (_secrets.ContainsKey(secret.Id))
            throw new InvalidOperationException($"A secret with ID '{secret.Id}' already exists.");
    }

    private static Secret Clone(Secret secret)
    {
        return new Secret
        {
            Id = secret.Id,
            TenantId = secret.TenantId,
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
