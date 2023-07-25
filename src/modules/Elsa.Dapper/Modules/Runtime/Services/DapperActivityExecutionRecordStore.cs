using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;

namespace Elsa.Dapper.Modules.Runtime.Services;

/// <summary>
/// Implements the <see cref="IActivityExecutionStore"/> using Dapper.
/// </summary>
public class DapperActivityExecutionRecordStore : IActivityExecutionStore
{
    private const string TableName = "WorkflowExecutionLogRecords";
    private const string PrimaryKeyName = "Id";
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly Store<ActivityExecutionRecordRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperActivityExecutionRecordStore"/> class.
    /// </summary>
    public DapperActivityExecutionRecordStore(IPayloadSerializer payloadSerializer, Store<ActivityExecutionRecordRecord> store)
    {
        _payloadSerializer = payloadSerializer;
        _store = store;
    }

    /// <inheritdoc />
    public async Task SaveAsync(ActivityExecutionRecord record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await _store.SaveAsync(mappedRecord, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<ActivityExecutionRecord> records, CancellationToken cancellationToken = default)
    {
        var mappedRecords = records.Select(Map);
        await _store.SaveManyAsync(mappedRecords, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync<TOrderBy>(ActivityExecutionRecordFilter filter, ActivityExecutionRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return records.Select(Map);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityExecutionRecord>> FindManyAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return records.Select(Map);
    }

    /// <inheritdoc />
    public Task<long> CountAsync(ActivityExecutionRecordFilter filter, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
    private static void ApplyFilter(ParameterizedQuery query, ActivityExecutionRecordFilter filter)
    {
        query
            .Is(nameof(ActivityExecutionRecordRecord.ActivityId), filter.ActivityId)
            .In(nameof(ActivityExecutionRecordRecord.ActivityId), filter.ActivityIds)
            .Is(nameof(ActivityExecutionRecordRecord.WorkflowInstanceId), filter.WorkflowInstanceId);

        if (filter.Completed != null)
        {
            if(filter.Completed == true)
                query.IsNotNull(nameof(ActivityExecutionRecordRecord.CompletedAt));
            else
                query.IsNull(nameof(ActivityExecutionRecordRecord.CompletedAt));
        }
    }

    private ActivityExecutionRecordRecord Map(ActivityExecutionRecord source)
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
            ActivityTypeVersion = source.ActivityTypeVersion,
            ActivityState = source.ActivityState != null ? _payloadSerializer.Serialize(source.ActivityState) : default
        };
    }

    private ActivityExecutionRecord Map(ActivityExecutionRecordRecord source)
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
            ActivityTypeVersion = source.ActivityTypeVersion,
            ActivityState = source.ActivityState != null ? _payloadSerializer.Deserialize<IDictionary<string, object>>(source.ActivityState) : default
        };
    }
}