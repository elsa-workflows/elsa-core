using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dapper.Contracts;
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

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// <summary>
/// Implements the <see cref="IWorkflowExecutionLogStore"/> using Dapper.
/// </summary>
public class DapperWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private const string TableName = "WorkflowExecutionLogRecords";
    private const string PrimaryKeyName = "Id";
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly Store<WorkflowExecutionLogRecordRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperWorkflowExecutionLogStore"/> class.
    /// </summary>
    public DapperWorkflowExecutionLogStore(IDbConnectionProvider dbConnectionProvider, IPayloadSerializer payloadSerializer)
    {
        _payloadSerializer = payloadSerializer;
        _store = new Store<WorkflowExecutionLogRecordRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await _store.SaveAsync(mappedRecord, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        var mappedRecords = records.Select(Map);
        await _store.SaveManyAsync(mappedRecords, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record != null ? Map(record) : default;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var record = await _store.FindAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return record != null ? Map(record) : default;
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await FindManyAsync(
            filter,
            pageArgs,
            new WorkflowExecutionLogRecordOrder<DateTimeOffset>(x => x.Timestamp, OrderDirection.Descending),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var page = await _store.FindManyAsync(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return Map(page);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private static void ApplyFilter(ParameterizedQuery query, WorkflowExecutionLogRecordFilter filter)
    {
        query
            .Is(nameof(WorkflowExecutionLogRecordRecord.Id), filter.Id)
            .In(nameof(WorkflowExecutionLogRecordRecord.Id), filter.Ids)
            .Is(nameof(WorkflowExecutionLogRecordRecord.ParentActivityInstanceId), filter.ParentActivityInstanceId)
            .Is(nameof(WorkflowExecutionLogRecordRecord.ActivityId), filter.ActivityId)
            .In(nameof(WorkflowExecutionLogRecordRecord.ActivityId), filter.ActivityIds)
            .Is(nameof(WorkflowExecutionLogRecordRecord.WorkflowInstanceId), filter.WorkflowInstanceId)
            .In(nameof(WorkflowExecutionLogRecordRecord.WorkflowInstanceId), filter.WorkflowInstanceIds)
            .Is(nameof(WorkflowExecutionLogRecordRecord.EventName), filter.EventName)
            .In(nameof(WorkflowExecutionLogRecordRecord.ActivityId), filter.EventNames)
            ;
    }

    private Page<WorkflowExecutionLogRecord> Map(Page<WorkflowExecutionLogRecordRecord> source)
    {
        return new Page<WorkflowExecutionLogRecord>(source.Items.Select(Map).ToList(), source.TotalCount);
    }

    private WorkflowExecutionLogRecordRecord Map(WorkflowExecutionLogRecord source)
    {
        return new WorkflowExecutionLogRecordRecord
        {
            Id = source.Id,
            WorkflowDefinitionId = source.WorkflowDefinitionId,
            WorkflowDefinitionVersionId = source.WorkflowDefinitionVersionId,
            WorkflowInstanceId = source.WorkflowInstanceId,
            WorkflowVersion = source.WorkflowVersion,
            ActivityInstanceId = source.ActivityInstanceId,
            ParentActivityInstanceId = source.ParentActivityInstanceId,
            ActivityId = source.ActivityId,
            ActivityType = source.ActivityType,
            ActivityTypeVersion = source.ActivityTypeVersion,
            ActivityName = source.ActivityName,
            NodeId = source.NodeId,
            Timestamp = source.Timestamp,
            Sequence = source.Sequence,
            EventName = source.EventName,
            Message = source.Message,
            Source = source.Source,
            SerializedActivityState = source.ActivityState != null ? _payloadSerializer.Serialize(source.ActivityState) : null,
            SerializedPayload = source.Payload != null ? _payloadSerializer.Serialize(source.Payload) : null,
        };
    }

    private WorkflowExecutionLogRecord Map(WorkflowExecutionLogRecordRecord source)
    {
        return new WorkflowExecutionLogRecord
        {
            Id = source.Id,
            WorkflowDefinitionId = source.WorkflowDefinitionId,
            WorkflowDefinitionVersionId = source.WorkflowDefinitionVersionId,
            WorkflowInstanceId = source.WorkflowInstanceId,
            WorkflowVersion = source.WorkflowVersion,
            ActivityInstanceId = source.ActivityInstanceId,
            ParentActivityInstanceId = source.ParentActivityInstanceId,
            ActivityId = source.ActivityId,
            ActivityType = source.ActivityType,
            ActivityTypeVersion = source.ActivityTypeVersion,
            ActivityName = source.ActivityName,
            NodeId = source.NodeId,
            Timestamp = source.Timestamp,
            Sequence = source.Sequence,
            EventName = source.EventName,
            Message = source.Message,
            Source = source.Source,
            ActivityState = source.SerializedActivityState != null ? _payloadSerializer.Deserialize<IDictionary<string, object>>(source.SerializedActivityState) : null,
            Payload = source.SerializedPayload != null ? _payloadSerializer.Deserialize(source.SerializedPayload) : null,
        };
    }
}