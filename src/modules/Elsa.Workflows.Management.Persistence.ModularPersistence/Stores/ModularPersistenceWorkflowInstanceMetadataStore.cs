using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Common.Models;
using Elsa.ModularPersistence.Documents;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Persistence.ModularPersistence.Storage;

namespace Elsa.Workflows.Management.Persistence.ModularPersistence.Stores;

public sealed class ModularPersistenceWorkflowInstanceMetadataStore(IDocumentStore documentStore) : IWorkflowInstanceMetadataStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async ValueTask SaveAsync(WorkflowInstanceMetadataRecord record, CancellationToken cancellationToken = default)
    {
        await using var session = await documentStore.OpenSessionAsync(cancellationToken);
        await SaveAsync(session, record, cancellationToken);
    }

    public async ValueTask SaveManyAsync(IEnumerable<WorkflowInstanceMetadataRecord> records, CancellationToken cancellationToken = default)
    {
        await using var session = await documentStore.OpenSessionAsync(cancellationToken);

        foreach (var record in records)
            await SaveAsync(session, record, cancellationToken);
    }

    public async ValueTask<WorkflowInstanceMetadataRecord?> FindAsync(string id, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        await using var session = await documentStore.OpenSessionAsync(cancellationToken);
        var envelope = await session.LoadAsync(new DocumentKey(id, WorkflowInstanceMetadataStorageManifest.StorageUnitName, tenantId), cancellationToken);
        return envelope == null ? null : ToRecord(envelope);
    }

    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var page = await QueryRecordsAsync(filter, pageArgs, tenantId, cancellationToken);
        return new Page<WorkflowInstanceSummary>(page.Items.Select(x => x.ToSummary()).ToList(), page.TotalCount);
    }

    public async ValueTask<Page<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        var page = await QueryRecordsAsync(filter, pageArgs, tenantId, cancellationToken);
        return new Page<string>(page.Items.Select(x => x.Id).ToList(), page.TotalCount);
    }

    public async ValueTask<long> CountAsync(WorkflowInstanceFilter filter, string? tenantId = null, CancellationToken cancellationToken = default)
    {
        await using var session = await documentStore.OpenSessionAsync(cancellationToken);
        var query = WorkflowInstanceMetadataQueryBuilder.Build(filter, tenantId: tenantId);
        var results = await session.QueryAsync(query, cancellationToken);
        return results.LongCount();
    }

    private static async ValueTask SaveAsync(IDocumentSession session, WorkflowInstanceMetadataRecord record, CancellationToken cancellationToken)
    {
        var key = new DocumentKey(record.Id, WorkflowInstanceMetadataStorageManifest.StorageUnitName, record.TenantId);
        var existingEnvelope = await session.LoadAsync(key, cancellationToken);
        var nextVersion = existingEnvelope?.Version + 1 ?? 1;
        var expectedVersion = existingEnvelope == null ? ExpectedDocumentVersion.New : ExpectedDocumentVersion.Exact(existingEnvelope.Version);
        await session.SaveAsync(ToEnvelope(record, nextVersion), expectedVersion, cancellationToken);
    }

    private async ValueTask<Page<WorkflowInstanceMetadataRecord>> QueryRecordsAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, string? tenantId, CancellationToken cancellationToken)
    {
        await using var session = await documentStore.OpenSessionAsync(cancellationToken);

        var countQuery = WorkflowInstanceMetadataQueryBuilder.Build(filter, tenantId: tenantId);
        var count = (await session.QueryAsync(countQuery, cancellationToken)).LongCount();
        var pageQuery = WorkflowInstanceMetadataQueryBuilder.Build(filter, pageArgs, tenantId);
        var records = (await session.QueryAsync(pageQuery, cancellationToken)).Select(ToRecord).ToList();

        return new Page<WorkflowInstanceMetadataRecord>(records, count);
    }

    private static DocumentEnvelope ToEnvelope(WorkflowInstanceMetadataRecord record, long documentVersion)
    {
        var data = JsonSerializer.Serialize(record, JsonOptions);
        return new DocumentEnvelope(record.Id, WorkflowInstanceMetadataStorageManifest.StorageUnitName, record.TenantId, documentVersion, record.CreatedAt, record.UpdatedAt, data);
    }

    private static WorkflowInstanceMetadataRecord ToRecord(DocumentEnvelope envelope) =>
        JsonSerializer.Deserialize<WorkflowInstanceMetadataRecord>(envelope.Data, JsonOptions)
        ?? throw new InvalidOperationException($"Workflow instance metadata document '{envelope.Id}' could not be deserialized.");
}
