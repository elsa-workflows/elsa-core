using Elsa.Dapper.Contracts;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// <summary>
/// Implements the <see cref="IActivityExecutionStore"/> using Dapper.
/// </summary>
public class DapperActivityExecutionRecordStore : IActivityExecutionStore
{
    private const string TableName = "ActivityExecutionRecords";
    private const string PrimaryKeyName = "Id";
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly ISafeSerializer _safeSerializer;
    private readonly Store<ActivityExecutionRecordRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperActivityExecutionRecordStore"/> class.
    /// </summary>
    public DapperActivityExecutionRecordStore(IDbConnectionProvider dbConnectionProvider, IPayloadSerializer payloadSerializer, ISafeSerializer safeSerializer)
    {
        _payloadSerializer = payloadSerializer;
        _safeSerializer = safeSerializer;
        _store = new Store<ActivityExecutionRecordRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = await Map(record, cancellationToken);
        await _store.SaveAsync(mappedRecord, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default)
    {
        var mappedRecords = await Task.WhenAll(records.Select(async x => await Map(x, cancellationToken)));
        await _store.SaveManyAsync(mappedRecords, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ActivityExecutionRecord?> FindAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : await Map(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return await Task.WhenAll(records.Select(async x => await Map(x, cancellationToken)));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return await Task.WhenAll(records.Select(async x => await Map(x, cancellationToken)));
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private static void ApplyFilter(ParameterizedQuery query, ActivityExecutionRecordFilter filter)
    {
        query
            .Is(nameof(ActivityExecutionRecordRecord.Id), filter.Id)
            .In(nameof(ActivityExecutionRecordRecord.Id), filter.Ids)
            .Is(nameof(ActivityExecutionRecordRecord.ActivityId), filter.ActivityId)
            .In(nameof(ActivityExecutionRecordRecord.ActivityId), filter.ActivityIds)
            .Is(nameof(ActivityExecutionRecordRecord.WorkflowInstanceId), filter.WorkflowInstanceId)
            .In(nameof(ActivityExecutionRecordRecord.WorkflowInstanceId), filter.WorkflowInstanceIds);

        if (filter.Completed != null)
        {
            if (filter.Completed == true)
                query.IsNotNull(nameof(ActivityExecutionRecordRecord.CompletedAt));
            else
                query.IsNull(nameof(ActivityExecutionRecordRecord.CompletedAt));
        }
    }

    private async ValueTask<ActivityExecutionRecordRecord> Map(ActivityExecutionRecord source, CancellationToken cancellationToken)
    {
        return new ActivityExecutionRecordRecord
        {
            Id = source.Id,
            ActivityId = source.ActivityId,
            ActivityType = source.ActivityType,
            ActivityName = source.ActivityName,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CompletedAt = source.CompletedAt,
            StartedAt = source.StartedAt,
            HasBookmarks = source.HasBookmarks,
            Status = source.Status.ToString(),
            ActivityTypeVersion = source.ActivityTypeVersion,
            SerializedActivityState = source.ActivityState != null ? await _safeSerializer.SerializeAsync(source.ActivityState, cancellationToken) : default,
            SerializedPayload = source.Payload != null ? await _safeSerializer.SerializeAsync(source.Payload, cancellationToken) : default,
            SerializedOutputs = source.Outputs != null ? await _safeSerializer.SerializeAsync(source.Outputs, cancellationToken) : default,
            SerializedException = source.Exception != null ? _payloadSerializer.Serialize(source.Exception) : default
        };
    }

    private async ValueTask<ActivityExecutionRecord> Map(ActivityExecutionRecordRecord source, CancellationToken cancellationToken)
    {
        return new ActivityExecutionRecord
        {
            Id = source.Id,
            ActivityId = source.ActivityId,
            ActivityType = source.ActivityType,
            ActivityName = source.ActivityName,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CompletedAt = source.CompletedAt,
            StartedAt = source.StartedAt,
            HasBookmarks = source.HasBookmarks,
            Status = Enum.Parse<ActivityStatus>(source.Status),
            ActivityTypeVersion = source.ActivityTypeVersion,
            ActivityState = source.SerializedActivityState != null ? _payloadSerializer.Deserialize<IDictionary<string, object>>(source.SerializedActivityState) : default,
            Payload = source.SerializedPayload != null ? await _safeSerializer.DeserializeAsync<IDictionary<string, object>>(source.SerializedPayload, cancellationToken) : default,
            Outputs = source.SerializedOutputs != null ? await _safeSerializer.DeserializeAsync<IDictionary<string, object?>>(source.SerializedOutputs, cancellationToken) : default,
            Exception = source.SerializedException != null ? _payloadSerializer.Deserialize<ExceptionState>(source.SerializedException) : default
        };
    }
}