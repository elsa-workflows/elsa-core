using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Multitenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Repositories;

public class FileSecretRepository(
    IOptions<SecretsOptions> options,
    ILogger<FileSecretRepository>? logger = null,
    ITenantAccessor? tenantAccessor = null) : ISecretRepository
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public async Task<Secret?> GetAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        var secrets = await ReadAllAsync(cancellationToken);
        return secrets.FirstOrDefault(x => SecretRepositoryTenant.IsVisible(x, tenantAccessor) && SecretRepositoryTenant.HasName(x, normalizedName));
    }

    public async Task<IReadOnlyCollection<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        return (await ReadAllAsync(cancellationToken)).Where(x => SecretRepositoryTenant.IsVisible(x, tenantAccessor)).ToList();
    }

    public async Task AddAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        SecretRepositoryTenant.Stamp(secret, tenantAccessor);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var secrets = await ReadAllUnsafeAsync(cancellationToken);
            if (secrets.Any(x => SecretRepositoryTenant.IsVisible(x, tenantAccessor) && SecretRepositoryTenant.HasName(x, secret.Name)))
                throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

            if (secrets.Any(x => SecretRepositoryTenant.HasSameTenantName(x, secret)))
                throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

            if (secrets.Any(x => x.Id == secret.Id))
                throw new InvalidOperationException($"A secret with ID '{secret.Id}' already exists.");

            secrets.Add(secret);
            await WriteAllUnsafeAsync(secrets, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> TryAddOrReplaceDeletedAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        SecretRepositoryTenant.Stamp(secret, tenantAccessor);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var secrets = await ReadAllUnsafeAsync(cancellationToken);
            var index = secrets.FindIndex(x => SecretRepositoryTenant.IsVisible(x, tenantAccessor) && SecretRepositoryTenant.HasName(x, secret.Name));
            if (index >= 0)
            {
                if (secrets[index].Status != SecretStatus.Deleted)
                    return false;

                if (!SecretRepositoryTenant.CanReplace(secrets[index], secret, tenantAccessor))
                    return false;

                secrets[index] = ReplaceTenantOwnedSecret(secrets[index], secret);
            }
            else
            {
                if (secrets.Any(x => SecretRepositoryTenant.HasSameTenantName(x, secret)))
                    return false;

                if (secrets.Any(x => x.Id == secret.Id))
                    return false;

                secrets.Add(secret);
            }

            await WriteAllUnsafeAsync(secrets, cancellationToken);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        SecretRepositoryTenant.Stamp(secret, tenantAccessor);

        await _lock.WaitAsync(cancellationToken);
        try
        {
            var secrets = await ReadAllUnsafeAsync(cancellationToken);
            var index = secrets.FindIndex(x => SecretRepositoryTenant.IsVisible(x, tenantAccessor) && SecretRepositoryTenant.HasName(x, secret.Name));
            if (index < 0)
            {
                if (secrets.Any(x => SecretRepositoryTenant.HasSameTenantName(x, secret)))
                    throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

                if (secrets.Any(x => x.Id == secret.Id))
                    throw new InvalidOperationException($"A secret with ID '{secret.Id}' already exists.");

                secrets.Add(secret);
            }
            else
            {
                if (!SecretRepositoryTenant.CanReplace(secrets[index], secret, tenantAccessor))
                    throw new InvalidOperationException($"A secret named '{secret.Name}' belongs to another tenant.");

                secrets[index] = ReplaceIdentityAndTenant(secrets[index], secret);
            }

            await WriteAllUnsafeAsync(secrets, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<Secret>> ReadAllAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            return await ReadAllUnsafeAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<List<Secret>> ReadAllUnsafeAsync(CancellationToken cancellationToken)
    {
        var path = GetPath();
        if (!File.Exists(path))
            return [];

        await using var stream = File.OpenRead(path);
        try
        {
            return await JsonSerializer.DeserializeAsync<List<Secret>>(stream, _jsonOptions, cancellationToken) ?? [];
        }
        catch (JsonException e)
        {
            logger?.LogError(e, "The secrets repository file '{Path}' could not be read because it contains invalid JSON.", path);
            return [];
        }
    }

    private async Task WriteAllUnsafeAsync(List<Secret> secrets, CancellationToken cancellationToken)
    {
        var path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = File.Create(temporaryPath))
                await JsonSerializer.SerializeAsync(stream, secrets.OrderBy(x => x.Name).ToList(), _jsonOptions, cancellationToken);

            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    /// <summary>
    /// Updates the aggregate payload while retaining the row identity and tenant ownership.
    /// </summary>
    private static Secret ReplaceTenantOwnedSecret(Secret existing, Secret incoming)
    {
        incoming.TenantId = existing.TenantId;
        return incoming;
    }

    private static Secret ReplaceIdentityAndTenant(Secret existing, Secret incoming)
    {
        incoming.Id = existing.Id;
        incoming.TenantId = existing.TenantId;
        return incoming;
    }

    private string GetPath() => options.Value.RepositoryFilePath ?? SecretsOptions.DefaultRepositoryFilePath;
}
