using Elsa.ModularPersistence.Descriptors;
using Elsa.ModularPersistence.Documents;
using Elsa.ModularPersistence.Queries;

namespace Elsa.ModularPersistence.Runtime;

public sealed class RuntimeEntityDataService(
    IRuntimeStorageDefinitionStore definitionStore,
    IRuntimeEntityDocumentStoreFactoryRegistry storeFactoryRegistry) : IRuntimeEntityDataService
{
    public async ValueTask<RuntimeEntityRecord> CreateAsync(string definitionId, RuntimeEntitySaveRequest request, RuntimeStorageOperationContext context, string? providerName = null, CancellationToken cancellationToken = default)
    {
        var (definition, manifest) = await GetPublishedDefinitionAsync(definitionId, context, cancellationToken);
        await using var session = await storeFactoryRegistry.CreateStore(manifest, providerName).OpenSessionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var envelope = new DocumentEnvelope(request.Id, definition.StorageUnitName, request.TenantId, 1, now, now, request.Data, request.Metadata);
        var result = await session.SaveAsync(envelope, ExpectedDocumentVersion.New, cancellationToken);
        return ToRecord(envelope with { }, result.Version);
    }

    public async ValueTask<RuntimeEntityRecord?> GetAsync(string definitionId, string id, string? tenantId, RuntimeStorageOperationContext context, string? providerName = null, CancellationToken cancellationToken = default)
    {
        var (definition, manifest) = await GetPublishedDefinitionAsync(definitionId, context, cancellationToken);
        await using var session = await storeFactoryRegistry.CreateStore(manifest, providerName).OpenSessionAsync(cancellationToken);
        var envelope = await session.LoadAsync(new DocumentKey(id, definition.StorageUnitName, tenantId), cancellationToken);
        return envelope == null ? null : ToRecord(envelope);
    }

    public async ValueTask<IReadOnlyCollection<RuntimeEntityRecord>> QueryAsync(string definitionId, RuntimeEntityQueryRequest request, RuntimeStorageOperationContext context, string? providerName = null, CancellationToken cancellationToken = default)
    {
        var (definition, manifest) = await GetPublishedDefinitionAsync(definitionId, context, cancellationToken);
        var storageUnit = manifest.StorageUnits.Single();
        var filters = request.Filters.Select(filter => ToDocumentFilter(definition, storageUnit, filter)).ToArray();
        if (filters.Length == 0)
            throw new InvalidOperationException("Runtime entity queries must include at least one indexed field filter.");

        var page = request.Limit is null ? null : new DocumentQueryPage(request.Limit.Value, request.Offset);
        var query = new DocumentQuery(definition.StorageUnitName, filters, request.Sorts, page, request.TenantId);
        await using var session = await storeFactoryRegistry.CreateStore(manifest, providerName).OpenSessionAsync(cancellationToken);
        var results = await session.QueryAsync(query, cancellationToken);
        return results.Select(x => ToRecord(x)).ToArray();
    }

    public async ValueTask<RuntimeEntityRecord> UpdateAsync(string definitionId, RuntimeEntitySaveRequest request, RuntimeStorageOperationContext context, string? providerName = null, CancellationToken cancellationToken = default)
    {
        var (definition, manifest) = await GetPublishedDefinitionAsync(definitionId, context, cancellationToken);
        await using var session = await storeFactoryRegistry.CreateStore(manifest, providerName).OpenSessionAsync(cancellationToken);
        var key = new DocumentKey(request.Id, definition.StorageUnitName, request.TenantId);
        var existing = await session.LoadAsync(key, cancellationToken) ?? throw new KeyNotFoundException($"Runtime entity '{request.Id}' was not found.");
        var expectedVersion = request.ExpectedVersion is null ? ExpectedDocumentVersion.Exact(existing.Version) : ExpectedDocumentVersion.Exact(request.ExpectedVersion.Value);
        var envelope = new DocumentEnvelope(request.Id, definition.StorageUnitName, request.TenantId, existing.Version + 1, existing.CreatedAt, DateTimeOffset.UtcNow, request.Data, request.Metadata ?? existing.Metadata);
        var result = await session.SaveAsync(envelope, expectedVersion, cancellationToken);
        return ToRecord(envelope, result.Version);
    }

    public async ValueTask<bool> DeleteAsync(string definitionId, string id, string? tenantId, RuntimeStorageOperationContext context, string? providerName = null, long? expectedVersion = null, CancellationToken cancellationToken = default)
    {
        var (definition, manifest) = await GetPublishedDefinitionAsync(definitionId, context, cancellationToken);
        await using var session = await storeFactoryRegistry.CreateStore(manifest, providerName).OpenSessionAsync(cancellationToken);
        var key = new DocumentKey(id, definition.StorageUnitName, tenantId);
        var existing = await session.LoadAsync(key, cancellationToken);
        if (existing == null)
            return false;

        var expected = expectedVersion is null ? ExpectedDocumentVersion.Exact(existing.Version) : ExpectedDocumentVersion.Exact(expectedVersion.Value);
        await session.DeleteAsync(key, expected, cancellationToken);
        return true;
    }

    private async ValueTask<(RuntimeStorageDefinition Definition, StorageManifestDescriptor Manifest)> GetPublishedDefinitionAsync(string definitionId, RuntimeStorageOperationContext context, CancellationToken cancellationToken)
    {
        var definition = await definitionStore.GetAsync(definitionId, cancellationToken) ?? throw new KeyNotFoundException($"Runtime storage definition '{definitionId}' was not found.");
        if (definition.State != RuntimeStorageDefinitionState.Published)
            throw new InvalidOperationException($"Runtime storage definition '{definitionId}' is not published.");

        EnsureAllowed(definition, context);
        var result = RuntimeStorageDefinitionManifestFactory.CreateManifest(definition);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Runtime storage definition '{definitionId}' is invalid: {string.Join("; ", result.Errors.Select(x => x.Message))}");

        return (definition, result.Manifest!);
    }

    private static void EnsureAllowed(RuntimeStorageDefinition definition, RuntimeStorageOperationContext context)
    {
        if (context.Permissions.Contains(PermissionNames.All))
            return;

        var missingPermissions = definition.RequiredPermissions.Where(x => !context.Permissions.Contains(x)).ToArray();
        if (missingPermissions.Length > 0)
            throw new UnauthorizedAccessException($"Runtime entity access requires permission(s): {string.Join(", ", missingPermissions)}.");
    }

    private static DocumentQueryFilter ToDocumentFilter(RuntimeStorageDefinition definition, StorageUnitDescriptor storageUnit, RuntimeEntityQueryFilter filter)
    {
        var field = storageUnit.Fields.SingleOrDefault(x => x.Name == filter.FieldName)
                    ?? throw new InvalidOperationException($"Field '{filter.FieldName}' is not declared by runtime storage definition '{definition.Id}'.");
        var indexName = string.IsNullOrWhiteSpace(filter.IndexName)
            ? definition.Indexes.FirstOrDefault(x => x.FieldNames.Contains(filter.FieldName, StringComparer.Ordinal))?.Name
            : filter.IndexName;
        if (string.IsNullOrWhiteSpace(indexName))
            throw new InvalidOperationException($"Field '{filter.FieldName}' is not indexed by runtime storage definition '{definition.Id}'.");

        foreach (var value in filter.Values)
        {
            if (value.Type != field.Type)
                throw new InvalidOperationException($"Filter value for field '{filter.FieldName}' uses type '{value.Type}', but the field type is '{field.Type}'.");
        }

        return new DocumentQueryFilter(indexName, filter.FieldName, filter.Operator, filter.Values);
    }

    private static RuntimeEntityRecord ToRecord(DocumentEnvelope envelope, long? version = null) =>
        new(envelope.Id, envelope.TenantId, version ?? envelope.Version, envelope.CreatedAt, envelope.UpdatedAt, envelope.Data, envelope.Metadata);
}
