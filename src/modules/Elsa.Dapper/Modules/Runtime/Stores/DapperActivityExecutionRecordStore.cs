using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using Elsa.Workflows.State;
using JetBrains.Annotations;

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// <summary>
/// Implements the <see cref="IActivityExecutionStore"/> using Dapper.
/// </summary>
[UsedImplicitly]
internal class DapperActivityExecutionRecordStore(Store<ActivityExecutionRecordRecord> store, IPayloadSerializer payloadSerializer, ISafeSerializer safeSerializer)
    : IActivityExecutionStore
{
    /// <inheritdoc />
    public async Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = await Map(record, cancellationToken);
        await store.SaveAsync(mappedRecord, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default)
    {
        var mappedRecords = await Task.WhenAll(records.Select(async x => await Map(x, cancellationToken)));
        await store.SaveManyAsync(mappedRecords, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ActivityExecutionRecord?> FindAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record == null ? null : await MapAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return await Task.WhenAll(records.Select(async x => await MapAsync(x, cancellationToken)));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return await Task.WhenAll(records.Select(async x => await MapAsync(x, cancellationToken)));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync<ActivityExecutionSummaryRecord>(q => ApplyFilter(q, filter), cancellationToken);
        return records.Select(MapSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecordSummary>> FindManySummariesAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync<ActivityExecutionSummaryRecord>(q => ApplyFilter(q, filter), cancellationToken);
        return records.Select(MapSummary).ToList();
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.CountAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
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

    private ActivityExecutionRecordRecord Map(ActivityExecutionRecord source, CancellationToken cancellationToken)
    {
        return new ActivityExecutionRecordRecord
        {
            Id = source.Id,
            ActivityId = source.ActivityId,
            ActivityNodeId = source.ActivityNodeId,
            ActivityType = source.ActivityType,
            ActivityName = source.ActivityName,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CompletedAt = source.CompletedAt,
            StartedAt = source.StartedAt,
            HasBookmarks = source.HasBookmarks,
            Status = source.Status.ToString(),
            ActivityTypeVersion = source.ActivityTypeVersion,
            SerializedActivityState = source.ActivityState != null ? await safeSerializer.SerializeAsync(source.ActivityState, cancellationToken) : null,
            SerializedPayload = source.Payload != null ? await safeSerializer.SerializeAsync(source.Payload, cancellationToken) : null,
            SerializedOutputs = source.Outputs != null ? await safeSerializer.SerializeAsync(source.Outputs, cancellationToken) : null,
            SerializedException = source.Exception != null ? payloadSerializer.Serialize(source.Exception) : null,
            SerializedProperties = source.Properties.Any() ? await safeSerializer.SerializeAsync(source.Properties, cancellationToken) : null,
            TenantId = source.TenantId
        };
    }

    private ActivityExecutionRecord Map(ActivityExecutionRecordRecord source, CancellationToken cancellationToken)
    {
        return new ActivityExecutionRecord
        {
            Id = source.Id,
            ActivityId = source.ActivityId,
            ActivityNodeId = source.ActivityNodeId,
            ActivityType = source.ActivityType,
            ActivityName = source.ActivityName,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CompletedAt = source.CompletedAt,
            StartedAt = source.StartedAt,
            HasBookmarks = source.HasBookmarks,
            Status = Enum.Parse<ActivityStatus>(source.Status),
            ActivityTypeVersion = source.ActivityTypeVersion,
            ActivityState = source.SerializedActivityState != null ? payloadSerializer.Deserialize<IDictionary<string, object>>(source.SerializedActivityState) : default,
            Payload = source.SerializedPayload != null ? await safeSerializer.DeserializeAsync<IDictionary<string, object>>(source.SerializedPayload, cancellationToken) : default,
            Outputs = source.SerializedOutputs != null ? await safeSerializer.DeserializeAsync<IDictionary<string, object?>>(source.SerializedOutputs, cancellationToken) : default,
            Exception = source.SerializedException != null ? payloadSerializer.Deserialize<ExceptionState>(source.SerializedException) : default,
            Properties = source.SerializedProperties != null ? await safeSerializer.DeserializeAsync<IDictionary<string, object>>(source.SerializedProperties, cancellationToken) : new Dictionary<string, object>(),
            TenantId = source.TenantId
        };
    }

    private ActivityExecutionRecordSummary MapSummary(ActivityExecutionSummaryRecord source)
    {
        return new ActivityExecutionRecordSummary
        {
            Id = source.Id,
            ActivityId = source.ActivityId,
            ActivityNodeId = source.ActivityNodeId,
            ActivityType = source.ActivityType,
            ActivityName = source.ActivityName,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CompletedAt = source.CompletedAt,
            StartedAt = source.StartedAt,
            HasBookmarks = source.HasBookmarks,
            Status = Enum.Parse<ActivityStatus>(source.Status),
            ActivityTypeVersion = source.ActivityTypeVersion,
            TenantId = source.TenantId
        };
    }
}