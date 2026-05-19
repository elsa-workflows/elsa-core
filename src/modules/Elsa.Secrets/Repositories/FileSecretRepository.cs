using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Repositories;

public class FileSecretRepository(IOptions<SecretsOptions> options) : ISecretRepository
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    public async Task<bool> ExistsAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        var secret = await GetAsync(normalizedName, cancellationToken);
        return secret != null;
    }

    public async Task<Secret?> GetAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        var secrets = await ReadAllAsync(cancellationToken);
        return secrets.FirstOrDefault(x => string.Equals(x.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyCollection<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await ReadAllAsync(cancellationToken);
    }

    public async Task AddAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var secrets = await ReadAllUnsafeAsync(cancellationToken);
            if (secrets.Any(x => string.Equals(x.Name, secret.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

            secrets.Add(secret);
            await WriteAllUnsafeAsync(secrets, cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var secrets = await ReadAllUnsafeAsync(cancellationToken);
            var index = secrets.FindIndex(x => string.Equals(x.Name, secret.Name, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                secrets.Add(secret);
            else
                secrets[index] = secret;

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
        return await JsonSerializer.DeserializeAsync<List<Secret>>(stream, _jsonOptions, cancellationToken) ?? [];
    }

    private async Task WriteAllUnsafeAsync(List<Secret> secrets, CancellationToken cancellationToken)
    {
        var path = GetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await using (var stream = File.Create(temporaryPath))
            await JsonSerializer.SerializeAsync(stream, secrets.OrderBy(x => x.Name).ToList(), _jsonOptions, cancellationToken);

        File.Move(temporaryPath, path, true);
    }

    private string GetPath() => options.Value.RepositoryFilePath ?? SecretsOptions.DefaultRepositoryFilePath;
}
