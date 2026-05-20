namespace Elsa.Secrets.Services;

public class DefaultSecretManager(ISecretNameValidator nameValidator, ISecretStoreRegistry storeRegistry, ISecretTypeRegistry typeRegistry, ISecretRepository repository) : ISecretManager
{
    public async Task<Secret> CreateAsync(CreateSecretRequest request, CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name);
        var secret = await CreateSecretAsync(request, cancellationToken);
        if (!await repository.TryAddOrReplaceDeletedAsync(secret, cancellationToken))
            throw new InvalidOperationException($"A secret named '{request.Name}' already exists.");

        return secret;
    }

    public async Task<Secret?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        var secret = await repository.GetAsync(nameValidator.Normalize(name), cancellationToken);
        return secret is { Status: SecretStatus.Deleted } ? null : secret;
    }

    public async Task<IReadOnlyCollection<Secret>> ListAsync(ListSecretsRequest request, CancellationToken cancellationToken = default)
    {
        return (await ListPageAsync(request, cancellationToken)).Items;
    }

    public async Task<ListSecretsResult> ListPageAsync(ListSecretsRequest request, CancellationToken cancellationToken = default)
    {
        var secrets = await repository.ListAsync(cancellationToken);
        var query = ApplyFilters(secrets, request);
        var totalCount = query.LongCount();

        var pageSize = request.PageSize is > 0 ? Math.Min(request.PageSize.Value, 200) : 100;
        var page = request.Page is > 0 ? request.Page.Value : 0;

        var items = query
            .OrderBy(x => x.Name)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToList();

        return new ListSecretsResult { Items = items, TotalCount = totalCount };
    }

    public async Task<long> CountAsync(ListSecretsRequest request, CancellationToken cancellationToken = default)
    {
        var secrets = await repository.ListAsync(cancellationToken);
        return ApplyFilters(secrets, request).LongCount();
    }

    public async Task<Secret> RotateAsync(string name, RotateSecretRequest request, CancellationToken cancellationToken = default)
    {
        var secret = await GetExistingAsync(name, cancellationToken);
        if (secret.Status == SecretStatus.Revoked)
            throw new InvalidOperationException($"Secret '{secret.Name}' is revoked and cannot be rotated.");

        var provider = typeRegistry.Get(secret.TypeName);
        if (!provider.ValidateRotation(request, secret.StoreName, out var error))
            throw new InvalidOperationException(error);

        var store = storeRegistry.Get(secret.StoreName);
        EnsureCanWrite(store);
        var nextVersion = secret.Versions.Count == 0 ? 1 : secret.Versions.Max(x => x.Version) + 1;
        var version = new SecretVersion { Version = nextVersion, ExpiresAt = request.ExpiresAt };
        version.Payload = await store.WriteAsync(secret, version, CreatePayload(request), cancellationToken);

        foreach (var activeVersion in secret.Versions.Where(x => x.Status == SecretStatus.Active))
            activeVersion.Status = SecretStatus.Retired;

        secret.Versions.Add(version);
        secret.Status = SecretStatus.Active;
        secret.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveAsync(secret, cancellationToken);

        return secret;
    }

    public async Task<Secret?> RevokeAsync(string name, CancellationToken cancellationToken = default)
    {
        var secret = await GetAsync(name, cancellationToken);
        if (secret == null)
            return null;

        secret.Status = SecretStatus.Revoked;
        secret.UpdatedAt = DateTimeOffset.UtcNow;
        foreach (var version in secret.Versions.Where(x => x.Status == SecretStatus.Active))
            version.Status = SecretStatus.Revoked;

        await repository.SaveAsync(secret, cancellationToken);
        return secret;
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        var secret = await GetAsync(name, cancellationToken);
        if (secret == null)
            return false;

        await storeRegistry.Get(secret.StoreName).DeleteAsync(secret, cancellationToken);
        secret.Status = SecretStatus.Deleted;
        secret.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.SaveAsync(secret, cancellationToken);
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
        catch (InvalidOperationException e)
        {
            return new SecretTestResponse { Succeeded = false, Error = e.Message };
        }
        catch (KeyNotFoundException e)
        {
            return new SecretTestResponse { Succeeded = false, Error = e.Message };
        }
        catch (ArgumentException e)
        {
            return new SecretTestResponse { Succeeded = false, Error = e.Message };
        }
        catch (System.Security.Cryptography.CryptographicException e)
        {
            return new SecretTestResponse { Succeeded = false, Error = e.Message };
        }
        catch (FormatException e)
        {
            return new SecretTestResponse { Succeeded = false, Error = e.Message };
        }
    }

    public async Task<SecretPayload> ResolvePayloadAsync(string name, CancellationToken cancellationToken = default)
    {
        var secret = await GetExistingAsync(name, cancellationToken);
        return await ResolvePayloadAsync(secret, cancellationToken);
    }

    public async Task<SecretPayload> ResolvePayloadAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        var version = GetLatestActiveVersion(secret);
        var store = storeRegistry.Get(secret.StoreName);
        var payload = await store.ReadAsync(secret, version, cancellationToken);

        if (payload?.Value == null)
            throw new InvalidOperationException($"Secret '{secret.Name}' could not be resolved.");

        return payload;
    }

    private async Task<Secret> CreateSecretAsync(CreateSecretRequest request, CancellationToken cancellationToken)
    {
        var typeProvider = typeRegistry.Get(request.TypeName);
        var store = storeRegistry.Get(request.StoreName);
        EnsureCanWrite(store);

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
        version.Payload = await store.WriteAsync(secret, version, CreatePayload(request), cancellationToken);
        secret.Versions.Add(version);

        return secret;
    }

    private async Task<Secret> GetExistingAsync(string name, CancellationToken cancellationToken)
    {
        var secret = await GetAsync(name, cancellationToken);
        return secret == null ? throw new KeyNotFoundException($"Secret '{name}' was not found.") : secret;
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

    private static void EnsureCanWrite(ISecretStore store)
    {
        if (!store.Descriptor.Capabilities.HasFlag(SecretStoreCapabilities.Write))
            throw new InvalidOperationException($"Secret store '{store.Name}' does not support writing secrets.");
    }

    private static IEnumerable<Secret> ApplyFilters(IEnumerable<Secret> secrets, ListSecretsRequest request)
    {
        var query = secrets.Where(x => x.Status != SecretStatus.Deleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                x.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (x.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var typeNames = GetFilterValues(request.TypeName, request.TypeNames);
        if (typeNames.Count > 0)
            query = query.Where(x => typeNames.Contains(x.TypeName));

        var storeNames = GetFilterValues(request.StoreName, request.StoreNames);
        if (storeNames.Count > 0)
            query = query.Where(x => storeNames.Contains(x.StoreName));

        if (!string.IsNullOrWhiteSpace(request.Scope))
            query = query.Where(x => string.Equals(x.Scope, request.Scope, StringComparison.OrdinalIgnoreCase));

        if (request.Status != null)
            query = query.Where(x => x.Status == request.Status);

        return query;
    }

    private static HashSet<string> GetFilterValues(string? value, IEnumerable<string> values)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(value))
            result.Add(value.Trim());

        foreach (var item in values.Where(x => !string.IsNullOrWhiteSpace(x)))
            result.Add(item.Trim());

        return result;
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
