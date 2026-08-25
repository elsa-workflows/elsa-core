using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.VNext.Document;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;

namespace Elsa.Secrets.Persistence.VNext.Repositories;

public class VNextSecretRepository(IDocumentStore documentStore, ITenantAccessor tenantAccessor) : ISecretRepository
{
    /// <summary>
    /// Refuses to serve a request made in a non-default tenant context.
    /// </summary>
    /// <remarks>
    /// Secrets are tenant-scoped entities, and the EF Core providers enforce that through the query filter
    /// every other entity uses. This provider inherits none of it: <c>Elsa.Persistence.VNext</c> has no tenant
    /// concept, and documents here are keyed by secret name alone, so every tenant would read and overwrite
    /// the same document. Making it tenant-aware means changing the document id scheme, which relocates
    /// existing documents and is a storage change to make deliberately rather than fold into a tenancy fix.
    /// <para>
    /// Until then this throws rather than quietly serving one tenant's secret to another. It is checked per
    /// call rather than at startup so it catches a tenant context entered at runtime, and it stays silent for
    /// the default tenant, which is every single-tenant deployment.
    /// </para>
    /// </remarks>
    private void EnsureDefaultTenant()
    {
        var tenantId = tenantAccessor.TenantId;

        if (!string.IsNullOrEmpty(tenantId))
            throw new NotSupportedException(
                $"The VNext secrets persistence provider does not support multitenancy, and the current tenant is '{tenantId}'. "
                + "It stores secrets keyed by name alone, so tenants would share them. Use an Entity Framework Core secrets provider.");
    }

    public const string StorageUnitName = "Secrets";

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Secret?> GetAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        EnsureDefaultTenant();
        var document = await documentStore.LoadAsync(StorageUnitName, NormalizeDocumentId(normalizedName), cancellationToken);
        return document is null ? null : Deserialize(document);
    }

    public async Task<IReadOnlyCollection<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        EnsureDefaultTenant();
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
        EnsureDefaultTenant();
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
        EnsureDefaultTenant();
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
        EnsureDefaultTenant();
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
