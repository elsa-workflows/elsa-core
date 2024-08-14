using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// <summary>
/// Provides a Dapper implementation of <see cref="ITriggerStore"/>.
/// </summary>
[UsedImplicitly]
internal class DapperTriggerStore(Store<StoredTriggerRecord> store, IPayloadSerializer payloadSerializer) : ITriggerStore
{
    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await store.SaveAsync(mappedRecord, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        var mappedRecords = records.Select(Map);
        await store.SaveManyAsync(mappedRecords, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<StoredTrigger?> FindAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record != null ? Map(record) : default;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        var filter = new TriggerFilter
        {
            Ids = removed.Select(r => r.Id).ToList()
        };
        await DeleteManyAsync(filter, cancellationToken);
        await SaveManyAsync(added, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private void ApplyFilter(ParameterizedQuery query, TriggerFilter filter)
    {
        query
            .Is(nameof(StoredTriggerRecord.Id), filter.Id)
            .In(nameof(StoredTriggerRecord.Id), filter.Ids)
            .Is(nameof(StoredTriggerRecord.WorkflowDefinitionId), filter.WorkflowDefinitionId)
            .In(nameof(StoredTriggerRecord.WorkflowDefinitionId), filter.WorkflowDefinitionIds)
            .Is(nameof(StoredTriggerRecord.WorkflowDefinitionVersionId), filter.WorkflowDefinitionVersionId)
            .In(nameof(StoredTriggerRecord.WorkflowDefinitionVersionId), filter.WorkflowDefinitionVersionIds)
            .Is(nameof(StoredTriggerRecord.Name), filter.Name)
            .In(nameof(StoredTriggerRecord.Name), filter.Names)
            .Is(nameof(StoredTriggerRecord.Hash), filter.Hash)
            ;
    }

    private IEnumerable<StoredTrigger> Map(IEnumerable<StoredTriggerRecord> source) => source.Select(Map);

    private StoredTrigger Map(StoredTriggerRecord source)
    {
        return new StoredTrigger
        {
            Id = source.Id,
            ActivityId = source.ActivityId,
            Hash = source.Hash,
            Name = source.Name,
            WorkflowDefinitionId = source.WorkflowDefinitionId,
            WorkflowDefinitionVersionId = source.WorkflowDefinitionVersionId,
            Payload = source.SerializedPayload != null ? payloadSerializer.Deserialize(source.SerializedPayload) : default,
            TenantId = source.TenantId
        };
    }

    private StoredTriggerRecord Map(StoredTrigger source)
    {
        return new StoredTriggerRecord
        {
            Id = source.Id,
            ActivityId = source.ActivityId,
            Hash = source.Hash,
            Name = source.Name,
            WorkflowDefinitionId = source.WorkflowDefinitionId,
            WorkflowDefinitionVersionId = source.WorkflowDefinitionVersionId,
            SerializedPayload = source.Payload != null ? payloadSerializer.Serialize(source.Payload) : default,
            TenantId = source.TenantId
        };
    }
}