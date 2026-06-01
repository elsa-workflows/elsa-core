using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Queries;
using Elsa.Secrets.Storage;

namespace Elsa.Secrets.Repositories;

public class ModularPersistenceSecretRepository(IDocumentStore documentStore) : ISecretRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<Secret?> GetAsync(string normalizedName, CancellationToken cancellationToken = default)
    {
        await using var session = await documentStore.OpenSessionAsync(cancellationToken);
        var envelope = await FindByNameAsync(session, normalizedName, cancellationToken);
        return envelope == null ? null : ToSecret(envelope);
    }

    public async Task<IReadOnlyCollection<Secret>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var session = await documentStore.OpenSessionAsync(cancellationToken);
        var documents = await session.QueryAsync(new DocumentQuery(
            SecretsStorageManifest.SecretsStorageUnitName,
            [
                DocumentQueryFilter.IsNotNull(SecretsStorageManifest.NameIndexName, "Name")
            ]), cancellationToken);
        return documents.Select(ToSecret).ToList();
    }

    public async Task AddAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        await using var session = await documentStore.OpenSessionAsync(cancellationToken);
        if (await FindByNameAsync(session, secret.Name, cancellationToken) != null)
            throw new InvalidOperationException($"A secret named '{secret.Name}' already exists.");

        await session.SaveAsync(ToEnvelope(secret, 1), ExpectedDocumentVersion.New, cancellationToken);
    }

    public async Task<bool> TryAddOrReplaceDeletedAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        await using var session = await documentStore.OpenSessionAsync(cancellationToken);
        var existingEnvelope = await FindByNameAsync(session, secret.Name, cancellationToken);
        if (existingEnvelope == null)
        {
            await session.SaveAsync(ToEnvelope(secret, 1), ExpectedDocumentVersion.New, cancellationToken);
            return true;
        }

        var existingSecret = ToSecret(existingEnvelope);
        if (existingSecret.Status != SecretStatus.Deleted)
            return false;

        await session.DeleteAsync(existingEnvelope.Key, ExpectedDocumentVersion.Exact(existingEnvelope.Version), cancellationToken);
        await session.SaveAsync(ToEnvelope(secret, 1), ExpectedDocumentVersion.New, cancellationToken);
        return true;
    }

    public async Task SaveAsync(Secret secret, CancellationToken cancellationToken = default)
    {
        await using var session = await documentStore.OpenSessionAsync(cancellationToken);
        var key = CreateKey(secret.Id);
        var existingEnvelope = await session.LoadAsync(key, cancellationToken);
        var nextVersion = existingEnvelope?.Version + 1 ?? 1;
        var expectedVersion = existingEnvelope == null ? ExpectedDocumentVersion.New : ExpectedDocumentVersion.Exact(existingEnvelope.Version);
        await session.SaveAsync(ToEnvelope(secret, nextVersion), expectedVersion, cancellationToken);
    }

    private static async ValueTask<DocumentEnvelope?> FindByNameAsync(IDocumentSession session, string normalizedName, CancellationToken cancellationToken)
    {
        var query = new DocumentQuery(
            SecretsStorageManifest.SecretsStorageUnitName,
            [
                DocumentQueryFilter.Equal(
                    SecretsStorageManifest.NameIndexName,
                    "Name",
                    DocumentQueryValue.String(normalizedName))
            ],
            page: new DocumentQueryPage(1));
        var results = await session.QueryAsync(query, cancellationToken);
        return results.SingleOrDefault();
    }

    private static DocumentEnvelope ToEnvelope(Secret secret, long documentVersion)
    {
        var now = DateTimeOffset.UtcNow;
        var createdAt = secret.CreatedAt == default ? now : secret.CreatedAt;
        var updatedAt = secret.UpdatedAt ?? createdAt;
        var document = SecretDocument.FromSecret(secret);
        var data = JsonSerializer.Serialize(document, JsonOptions);
        return new DocumentEnvelope(secret.Id, SecretsStorageManifest.SecretsStorageUnitName, null, documentVersion, createdAt, updatedAt, data);
    }

    private static Secret ToSecret(DocumentEnvelope envelope)
    {
        var document = JsonSerializer.Deserialize<SecretDocument>(envelope.Data, JsonOptions) ?? throw new InvalidOperationException($"Secret document '{envelope.Id}' could not be deserialized.");
        return document.ToSecret();
    }

    private static DocumentKey CreateKey(string id) => new(id, SecretsStorageManifest.SecretsStorageUnitName);

    private sealed class SecretDocument
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string? Description { get; set; }
        public string TypeName { get; set; } = default!;
        public string StoreName { get; set; } = default!;
        public string? Scope { get; set; }
        public SecretStatus Status { get; set; }
        public ICollection<string> Tags { get; set; } = [];
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public int? CurrentVersion { get; set; }
        public DateTimeOffset? CurrentVersionExpiresAt { get; set; }
        public IList<SecretVersion> Versions { get; set; } = [];

        public static SecretDocument FromSecret(Secret secret)
        {
            var latestActiveVersion = secret.LatestActiveVersion;
            return new SecretDocument
            {
                Id = secret.Id,
                Name = secret.Name,
                DisplayName = secret.DisplayName,
                Description = secret.Description,
                TypeName = secret.TypeName,
                StoreName = secret.StoreName,
                Scope = secret.Scope,
                Status = secret.Status,
                Tags = secret.Tags.ToArray(),
                CreatedAt = secret.CreatedAt,
                UpdatedAt = secret.UpdatedAt,
                CurrentVersion = latestActiveVersion?.Version,
                CurrentVersionExpiresAt = latestActiveVersion?.ExpiresAt,
                Versions = secret.Versions.ToList()
            };
        }

        public Secret ToSecret() =>
            new()
            {
                Id = Id,
                Name = Name,
                DisplayName = DisplayName,
                Description = Description,
                TypeName = TypeName,
                StoreName = StoreName,
                Scope = Scope,
                Status = Status,
                Tags = Tags.ToHashSet(StringComparer.OrdinalIgnoreCase),
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                Versions = Versions.ToList()
            };
    }
}
