using System.Collections.Concurrent;

namespace Elsa.Secrets.Services;

public class DefaultSecretManager(ISecretNameValidator nameValidator, ISecretStoreRegistry storeRegistry, ISecretTypeRegistry typeRegistry) : ISecretManager
{
    private readonly ConcurrentDictionary<string, Secret> _secrets = new(StringComparer.OrdinalIgnoreCase);

    public Task<Secret> CreateAsync(CreateSecretRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name);
        var normalizedName = nameValidator.Normalize(request.Name);

        if (!_secrets.TryAdd(normalizedName, CreateSecret(request)))
            throw new InvalidOperationException($"A secret named '{request.Name}' already exists.");

        return Task.FromResult(_secrets[normalizedName]);
    }

    public Task<Secret?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        _secrets.TryGetValue(nameValidator.Normalize(name), out var secret);
        return Task.FromResult(secret is { Status: SecretStatus.Deleted } ? null : secret);
    }

    public Task<IReadOnlyCollection<Secret>> ListAsync(ListSecretsRequest request, CancellationToken cancellationToken = default)
    {
        var query = _secrets.Values.Where(x => x.Status != SecretStatus.Deleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (!string.IsNullOrWhiteSpace(request.TypeName))
            query = query.Where(x => string.Equals(x.TypeName, request.TypeName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.StoreName))
            query = query.Where(x => string.Equals(x.StoreName, request.StoreName, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(request.Scope))
            query = query.Where(x => string.Equals(x.Scope, request.Scope, StringComparison.OrdinalIgnoreCase));

        if (request.Status != null)
            query = query.Where(x => x.Status == request.Status);

        var pageSize = request.PageSize is > 0 ? Math.Min(request.PageSize.Value, 200) : 100;
        var page = request.Page is > 0 ? request.Page.Value : 0;

        return Task.FromResult<IReadOnlyCollection<Secret>>(query
            .OrderBy(x => x.Name)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToList());
    }

    public async Task<Secret> RotateAsync(string name, RotateSecretRequest request, CancellationToken cancellationToken = default)
    {
        var secret = await GetExistingAsync(name, cancellationToken);
        var provider = typeRegistry.Get(secret.TypeName);
        if (!provider.ValidateRotation(request, secret.StoreName, out var error))
            throw new InvalidOperationException(error);

        var store = storeRegistry.Get(secret.StoreName);
        var nextVersion = secret.Versions.Count == 0 ? 1 : secret.Versions.Max(x => x.Version) + 1;
        var version = new SecretVersion { Version = nextVersion, ExpiresAt = request.ExpiresAt };
        version.Payload = await store.WriteAsync(secret, version, CreatePayload(request), cancellationToken);

        foreach (var activeVersion in secret.Versions.Where(x => x.Status == SecretStatus.Active))
            activeVersion.Status = SecretStatus.Retired;

        secret.Versions.Add(version);
        secret.Status = SecretStatus.Active;
        secret.UpdatedAt = DateTimeOffset.UtcNow;

        return secret;
    }

    public async Task<bool> RevokeAsync(string name, CancellationToken cancellationToken = default)
    {
        var secret = await GetAsync(name, cancellationToken);
        if (secret == null)
            return false;

        secret.Status = SecretStatus.Revoked;
        secret.UpdatedAt = DateTimeOffset.UtcNow;
        foreach (var version in secret.Versions.Where(x => x.Status == SecretStatus.Active))
            version.Status = SecretStatus.Revoked;

        return true;
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        var secret = await GetAsync(name, cancellationToken);
        if (secret == null)
            return false;

        await storeRegistry.Get(secret.StoreName).DeleteAsync(secret, cancellationToken);
        secret.Status = SecretStatus.Deleted;
        secret.UpdatedAt = DateTimeOffset.UtcNow;
        return true;
    }

    public async Task<SecretTestResponse> TestAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var secret = await GetExistingAsync(name, cancellationToken);
            var version = GetLatestActiveVersion(secret);
            var succeeded = await storeRegistry.Get(secret.StoreName).TestAsync(secret, version, cancellationToken);
            return new SecretTestResponse { Succeeded = succeeded, Error = succeeded ? null : "Secret value is unavailable." };
        }
        catch (Exception e)
        {
            return new SecretTestResponse { Succeeded = false, Error = e.Message };
        }
    }

    internal async Task<SecretPayload> ResolvePayloadAsync(string name, CancellationToken cancellationToken = default)
    {
        var secret = await GetExistingAsync(name, cancellationToken);
        var version = GetLatestActiveVersion(secret);
        var store = storeRegistry.Get(secret.StoreName);
        var payload = await store.ReadAsync(secret, version, cancellationToken);

        if (payload?.Value == null)
            throw new InvalidOperationException($"Secret '{secret.Name}' could not be resolved.");

        return payload;
    }

    private Secret CreateSecret(CreateSecretRequest request)
    {
        var typeProvider = typeRegistry.Get(request.TypeName);
        var store = storeRegistry.Get(request.StoreName);

        if (!typeProvider.Descriptor.SupportedStoreNames.Contains(store.Name, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Secret type '{request.TypeName}' does not support store '{request.StoreName}'.");

        if (!typeProvider.Validate(request, out var error))
            throw new InvalidOperationException(error);

        var secret = new Secret
        {
            Name = nameValidator.Normalize(request.Name),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Name.Trim() : request.DisplayName.Trim(),
            Description = request.Description,
            TypeName = typeProvider.Descriptor.Name,
            StoreName = store.Name,
            Scope = request.Scope,
            Tags = request.Tags.ToHashSet(StringComparer.OrdinalIgnoreCase)
        };

        var version = new SecretVersion { Version = 1, ExpiresAt = request.ExpiresAt };
        version.Payload = store.WriteAsync(secret, version, CreatePayload(request)).GetAwaiter().GetResult();
        secret.Versions.Add(version);

        return secret;
    }

    private async Task<Secret> GetExistingAsync(string name, CancellationToken cancellationToken)
    {
        var secret = await GetAsync(name, cancellationToken);
        return secret == null ? throw new InvalidOperationException($"Secret '{name}' was not found.") : secret;
    }

    private static SecretVersion GetLatestActiveVersion(Secret secret)
    {
        if (secret.Status != SecretStatus.Active)
            throw new InvalidOperationException($"Secret '{secret.Name}' is not active.");

        return secret.LatestActiveVersion ?? throw new InvalidOperationException($"Secret '{secret.Name}' has no active version.");
    }

    private void ValidateName(string name)
    {
        if (!nameValidator.IsValid(name, out var error))
            throw new InvalidOperationException(error);
    }

    private static SecretPayload CreatePayload(CreateSecretRequest request)
    {
        var payload = new SecretPayload { Value = request.Value, Metadata = new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase) };
        if (!string.IsNullOrWhiteSpace(request.ConfigurationKey))
            payload.Metadata["configurationKey"] = request.ConfigurationKey.Trim();
        return payload;
    }

    private static SecretPayload CreatePayload(RotateSecretRequest request)
    {
        var payload = new SecretPayload { Value = request.Value, Metadata = new Dictionary<string, string>(request.Metadata, StringComparer.OrdinalIgnoreCase) };
        if (!string.IsNullOrWhiteSpace(request.ConfigurationKey))
            payload.Metadata["configurationKey"] = request.ConfigurationKey.Trim();
        return payload;
    }
}
