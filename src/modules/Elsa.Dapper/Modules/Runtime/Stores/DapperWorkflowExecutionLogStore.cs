using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// <summary>
/// Implements the <see cref="IWorkflowExecutionLogStore"/> using Dapper.
/// </summary>
[UsedImplicitly]
internal class DapperWorkflowExecutionLogStore(Store<WorkflowExecutionLogRecordRecord> store, IPayloadSerializer payloadSerializer) : IWorkflowExecutionLogStore
{
    /// <inheritdoc />
    public async Task AddAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await store.AddAsync(mappedRecord, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        var mappedRecords = records.Select(Map);
        await store.AddManyAsync(mappedRecords, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await store.SaveAsync(mappedRecord, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        var mappedRecords = records.Select(Map);
        await store.SaveManyAsync(mappedRecords, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), cancellationToken);
        return record != null ? Map(record) : default;
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var record = await store.FindAsync(q => ApplyFilter(q, filter), order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
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
        var page = await store.FindManyAsync(q => ApplyFilter(q, filter), pageArgs, order.KeySelector.GetPropertyName(), order.Direction, cancellationToken);
        return Map(page);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
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
            .IsNot(nameof(WorkflowExecutionLogRecordRecord.ActivityType), filter.ExcludeActivityType)
            .NotIn(nameof(WorkflowExecutionLogRecordRecord.ActivityType), filter.ExcludeActivityTypes)
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
            ActivityNodeId = source.ActivityNodeId,
            Timestamp = source.Timestamp,
            Sequence = source.Sequence,
            EventName = source.EventName,
            Message = source.Message,
            Source = source.Source,
            SerializedActivityState = source.ActivityState != null ? payloadSerializer.Serialize(source.ActivityState) : null,
            SerializedPayload = source.Payload != null ? payloadSerializer.Serialize(source.Payload) : null,
            TenantId = source.TenantId
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
            ActivityNodeId = source.ActivityNodeId,
            Timestamp = source.Timestamp,
            Sequence = source.Sequence,
            EventName = source.EventName,
            Message = source.Message,
            Source = source.Source,
            ActivityState = source.SerializedActivityState != null ? payloadSerializer.Deserialize<IDictionary<string, object>>(source.SerializedActivityState) : null,
            Payload = source.SerializedPayload != null ? payloadSerializer.Deserialize(source.SerializedPayload) : null,
            TenantId = source.TenantId
        };
    }
}