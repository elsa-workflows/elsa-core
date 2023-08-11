using System.Text.Json;
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
/// A Dapper-based <see cref="IBookmarkStore"/> implementation.
/// </summary>
public class DapperWorkflowInboxStore : IWorkflowInboxStore
{
    private readonly IPayloadSerializer _payloadSerializer;
    private const string TableName = "WorkflowInboxMessages";
    private const string PrimaryKeyName = "Id";
    private readonly Store<WorkflowInboxMessageRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperWorkflowInboxStore"/> class.
    /// </summary>
    public DapperWorkflowInboxStore(IDbConnectionProvider dbConnectionProvider, IPayloadSerializer payloadSerializer)
    {
        _payloadSerializer = payloadSerializer;
        _store = new Store<WorkflowInboxMessageRecord>(dbConnectionProvider, TableName, PrimaryKeyName);
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(WorkflowInboxMessage record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await _store.SaveAsync(mappedRecord, PrimaryKeyName, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(IEnumerable<WorkflowInboxMessageFilter> filters, CancellationToken cancellationToken = default)
    {
        var records = await _store.FindManyAsync(q => ApplyFilter(q, filters.ToArray()), cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);
    }

    private void ApplyFilter(ParameterizedQuery query, params WorkflowInboxMessageFilter[] filters)
    {
        var clauses = new List<ParameterizedQuery>();

        foreach (var filter in filters)
        {
            var clause = new ParameterizedQuery(query.Dialect);

            clause
                .Is(nameof(WorkflowInboxMessageRecord.Hash), filter.Hash)
                .Is(nameof(WorkflowInboxMessageRecord.WorkflowInstanceId), filter.WorkflowInstanceId)
                .Is(nameof(WorkflowInboxMessageRecord.CorrelationId), filter.CorrelationId)
                .Is(nameof(WorkflowInboxMessageRecord.ActivityTypeName), filter.ActivityTypeName)
                .Is(nameof(WorkflowInboxMessageRecord.ActivityInstanceId), filter.ActivityInstanceId)
                .Is(nameof(WorkflowInboxMessageRecord.IsHandled), filter.IsHandled)
                ;

            clauses.Add(clause);
        }

        var clausesSql = string.Join(" OR ", $"({clauses.Select(x => x.Sql)})");
        query.Sql.AppendLine(clausesSql);
    }

    private IEnumerable<WorkflowInboxMessage> Map(IEnumerable<WorkflowInboxMessageRecord> source) => source.Select(Map);

    private WorkflowInboxMessageRecord Map(WorkflowInboxMessage source)
    {
        return new WorkflowInboxMessageRecord
        {
            Id = source.Id,
            ActivityTypeName = source.ActivityTypeName,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CorrelationId = source.CorrelationId,
            ActivityInstanceId = source.ActivityInstanceId,
            Hash = source.Hash,
            BookmarkPayload = _payloadSerializer.Serialize(source.BookmarkPayload),
            Input = source.Input != null ? _payloadSerializer.Serialize(source.Input) : default,
            HandledAt = source.HandledAt,
            AffectedWorkflowInstancesIds = source.AffectedWorkflowInstancesIds != null ? JsonSerializer.Serialize(source.AffectedWorkflowInstancesIds) : default,
            IsHandled = source.IsHandled,
            CreatedAt = source.CreatedAt,
            ExpiresAt = source.ExpiresAt,
        };
    }

    private WorkflowInboxMessage Map(WorkflowInboxMessageRecord source)
    {
        return new WorkflowInboxMessage
        {
            Id = source.Id,
            ActivityTypeName = source.ActivityTypeName,
            WorkflowInstanceId = source.WorkflowInstanceId,
            CorrelationId = source.CorrelationId,
            ActivityInstanceId = source.ActivityInstanceId,
            Hash = source.Hash,
            BookmarkPayload = _payloadSerializer.Deserialize(source.BookmarkPayload),
            AffectedWorkflowInstancesIds = source.AffectedWorkflowInstancesIds != null ? JsonSerializer.Deserialize<List<string>>(source.AffectedWorkflowInstancesIds) : default,
            HandledAt = source.HandledAt,
            IsHandled = source.IsHandled,
            Input = source.Input != null ? _payloadSerializer.Deserialize<Dictionary<string, object>>(source.Input) : default,
            CreatedAt = source.CreatedAt,
            ExpiresAt = source.ExpiresAt,
        };
    }
}