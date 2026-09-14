using Elsa.Common.Multitenancy;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Services;
using Elsa.Tenants.Options;
using Microsoft.Extensions.Options;

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
public class InMemorySecretRepository : ISecretRepository
{
    private readonly ITenantAccessor? tenantAccessor;
    private readonly bool tenancyEnabled;
    private readonly ISecretNameValidator nameValidator;

    public InMemorySecretRepository(ITenantAccessor? tenantAccessor = null)
        : this(tenantAccessor, tenantAccessor is not null, new DefaultSecretNameValidator())
    {
    }

    // Keep the pre-tenancy parameterless constructor in the public binary surface. Optional parameters do
    // not emit a zero-argument constructor for existing binaries to bind to.
    public InMemorySecretRepository() : this(null)
    {
    }

    public InMemorySecretRepository(
        IOptions<TenantsOptions> tenantsOptions,
        ISecretNameValidator nameValidator,
        ITenantAccessor? tenantAccessor = null)
        : this(tenantAccessor, tenantsOptions.Value.IsEnabled, nameValidator)
    {
    }

    private InMemorySecretRepository(ITenantAccessor? tenantAccessor, bool tenancyEnabled, ISecretNameValidator nameValidator)
    {
        this.tenantAccessor = tenantAccessor;
        this.tenancyEnabled = tenancyEnabled;
        this.nameValidator = nameValidator;
    }

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
                .Where(x => SecretRepositoryTenant.IsVisible(x, tenantAccessor, tenancyEnabled))
                .Select(Clone)
                .ToList();
            return Task.FromResult<IReadOnlyCollection<Secret>>(secrets);
        }
    }

    public Task AddAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        SecretRepositoryTenant.Stamp(secret, tenantAccessor, tenancyEnabled);

        lock (_sync)
        {
            if (FindVisible(secret.Name) is not null)
                throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

            if (_secrets.Values.Any(x => SecretRepositoryTenant.HasSameTenantName(x, secret, nameValidator, tenancyEnabled)))
                throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

            EnsureIdAvailable(secret);
            _secrets.Add(secret.Id, Clone(secret));
        }

        return Task.CompletedTask;
    }

    public Task<bool> TryAddOrReplaceDeletedAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        SecretRepositoryTenant.Stamp(secret, tenantAccessor, tenancyEnabled);

        lock (_sync)
        {
            var existing = FindVisible(secret.Name);
            if (existing is not null)
            {
                if (existing.Status != SecretStatus.Deleted || !SecretRepositoryTenant.CanReplace(existing, secret, tenantAccessor, tenancyEnabled))
                    return Task.FromResult(false);

                var replacement = Clone(secret);
                replacement.TenantId = existing.TenantId;

                // Validate before removing the deleted row. Reusing its own ID is valid; another row's ID
                // is a collision and must leave the deleted row untouched when validation fails.
                if (replacement.Id != existing.Id && _secrets.ContainsKey(replacement.Id))
                    return Task.FromResult(false);

                _secrets.Remove(existing.Id);
                _secrets.Add(replacement.Id, replacement);
                return Task.FromResult(true);
            }

            if (_secrets.Values.Any(x => SecretRepositoryTenant.HasSameTenantName(x, secret, nameValidator, tenancyEnabled)))
                return Task.FromResult(false);

            // Insert-side ID collisions must have the same non-mutating Try contract as the file repository.
            if (_secrets.ContainsKey(secret.Id))
                return Task.FromResult(false);

            _secrets.Add(secret.Id, Clone(secret));
            return Task.FromResult(true);
        }
    }

    public Task SaveAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        SecretRepositoryTenant.Stamp(secret, tenantAccessor, tenancyEnabled);

        lock (_sync)
        {
            var existing = FindVisible(secret.Name);
            if (existing is not null)
            {
                if (!SecretRepositoryTenant.CanReplace(existing, secret, tenantAccessor, tenancyEnabled))
                    throw new InvalidOperationException($"A secret named '{secret.Name}' belongs to another tenant.");

                // EF updates the row found by name, retaining its primary key and tenant ownership.
                var replacement = Clone(secret);
                replacement.Id = existing.Id;
                replacement.TenantId = existing.TenantId;
                _secrets[existing.Id] = replacement;
                return Task.CompletedTask;
            }

            if (_secrets.Values.Any(x => SecretRepositoryTenant.HasSameTenantName(x, secret, nameValidator, tenancyEnabled)))
                throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

            EnsureIdAvailable(secret);
            _secrets.Add(secret.Id, Clone(secret));
        }

        return Task.CompletedTask;
    }

    private Secret? FindVisible(string name) =>
        _secrets.Values.FirstOrDefault(x => SecretRepositoryTenant.IsVisible(x, tenantAccessor, tenancyEnabled) && SecretRepositoryTenant.HasName(x, name, nameValidator));

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
