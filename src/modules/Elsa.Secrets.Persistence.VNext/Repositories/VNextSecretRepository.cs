using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Persistence.VNext.Document;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;

namespace Elsa.Secrets.Persistence.VNext.Repositories;

public class VNextSecretRepository(IDocumentStore documentStore) : ISecretRepository
{
    public const string StorageUnitName = "Secrets";

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Secret?> GetAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        var document = await documentStore.LoadAsync(StorageUnitName, NormalizeDocumentId(normalizedName), cancellationToken);
        return document is null ? null : Deserialize(document);
    }

    public async Task<IReadOnlyCollection<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<Secret>();
        foreach (var status in Enum.GetValues<SecretStatus>())
        {
            var documents = await documentStore.QueryAsync(
                new DocumentQuery(StorageUnitName, new Dictionary<string, string?> { [nameof(Secret.Status)] = status.ToString() }),
                cancellationToken);

            results.AddRange(documents.Select(Deserialize));
        }

        return results.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public async Task AddAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveAsync(secret, expectedVersion: 0, cancellationToken);
        }
        catch (DocumentStoreConcurrencyException)
        {
            throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");
        }
    }

    public async Task<bool> TryAddOrReplaceDeletedAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var existing = await LoadDocumentAsync(secret.Name, cancellationToken);
            if (existing?.Secret.Status is not null and not SecretStatus.Deleted)
                return false;

            try
            {
                await SaveAsync(secret, existing?.Document.Version ?? 0, cancellationToken);
                return true;
            }
            catch (DocumentStoreConcurrencyException)
            {
            }
        }
    }

    public async Task SaveAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        var existing = await LoadDocumentAsync(secret.Name, cancellationToken);
        await SaveAsync(secret, existing?.Document.Version ?? 0, cancellationToken);
    }

    private async Task SaveAsync(Secret secret, long expectedVersion, CancellationToken cancellationToken)
    {
        var request = new SaveDocumentRequest(
            StorageUnitName,
            NormalizeDocumentId(secret.Name),
            JsonSerializer.Serialize(secret, _jsonOptions),
            CreateIndexValues(secret),
            expectedVersion);

        await documentStore.SaveAsync(request, cancellationToken);
    }

    private async Task<(StoredDocument Document, Secret Secret)?> LoadDocumentAsync(string name, CancellationToken cancellationToken)
    {
        var document = await documentStore.LoadAsync(StorageUnitName, NormalizeDocumentId(name), cancellationToken);
        return document is null ? null : (document, Deserialize(document));
    }

    private Secret Deserialize(StoredDocument document)
    {
        return JsonSerializer.Deserialize<Secret>(document.Content, _jsonOptions)
            ?? throw new DocumentStoreValidationException($"Stored secret document '{document.Id}' could not be deserialized.");
    }

    private static Dictionary<string, string?> CreateIndexValues(Secret secret)
    {
        return new()
        {
            [nameof(Secret.Name)] = secret.Name,
            [nameof(Secret.TypeName)] = secret.TypeName,
            [nameof(Secret.StoreName)] = secret.StoreName,
            [nameof(Secret.Scope)] = secret.Scope,
            [nameof(Secret.Status)] = secret.Status.ToString()
        };
    }

    private static string NormalizeDocumentId(string name)
    {
        return name.Trim().ToLowerInvariant();
    }
}
