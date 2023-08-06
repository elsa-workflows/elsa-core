using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// <summary>
/// Provides a Dapper implementation of <see cref="ITriggerStore"/>.
/// </summary>
public class DapperTriggerStore : ITriggerStore
{
    private const string TableName = "Triggers";
    private const string PrimaryKeyName = "Id";
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly Store<StoredTriggerRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperTriggerStore"/> class.
    /// </summary>
    public DapperTriggerStore(IDbConnectionProvider dbConnectionProvider, IPayloadSerializer payloadSerializer)
    {
        _payloadSerializer = payloadSerializer;
        _store = new Store<StoredTriggerRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(StoredTrigger record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await _store.SaveAsync(mappedRecord, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<StoredTrigger> records, CancellationToken cancellationToken = default)
    {
        var mappedRecords = records.Select(Map);
        await _store.SaveManyAsync(mappedRecords, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<StoredTrigger>> FindManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async ValueTask ReplaceAsync(IEnumerable<StoredTrigger> removed, IEnumerable<StoredTrigger> added, CancellationToken cancellationToken = default)
    {
        var filter = new TriggerFilter { Ids = removed.Select(r => r.Id).ToList() };
        await DeleteManyAsync(filter, cancellationToken);
        await SaveManyAsync(added, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(TriggerFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
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
            Payload = source.SerializedPayload != null ? _payloadSerializer.Deserialize(source.SerializedPayload) : default
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
            SerializedPayload = source.Payload != null ? _payloadSerializer.Serialize(source.Payload) : default
        };
    }
}