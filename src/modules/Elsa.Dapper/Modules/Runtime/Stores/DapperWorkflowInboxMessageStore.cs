using Elsa.Common.Contracts;
using Elsa.Common.Entities;
using Elsa.Common.Models;
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
public class DapperWorkflowInboxMessageStore : IWorkflowInboxMessageStore
{
    private readonly IPayloadSerializer _payloadSerializer;
    private readonly ISystemClock _systemClock;
    private const string TableName = "WorkflowInboxMessages";
    private const string PrimaryKeyName = "Id";
    private readonly Store<WorkflowInboxMessageRecord> _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperWorkflowInboxMessageStore"/> class.
    /// </summary>
    public DapperWorkflowInboxMessageStore(IDbConnectionProvider dbConnectionProvider, IPayloadSerializer payloadSerializer, ISystemClock systemClock)
    {
        _payloadSerializer = payloadSerializer;
        _systemClock = systemClock;
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
    public async ValueTask<long> DeleteManyAsync(WorkflowInboxMessageFilter filter, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        if (pageArgs == null)
            return await _store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);

        return await _store.DeleteAsync(q => ApplyFilter(q, filter), pageArgs, new[] { new OrderField(nameof(WorkflowInboxMessage.CreatedAt), OrderDirection.Ascending) }, cancellationToken);
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
                ;

            clauses.Add(clause);
        }

        var clausesSql = string.Join(" OR ", clauses.Select(x => x.Sql.ToString()));
        query.Sql.AppendLine(clausesSql);
        
        // Copy all parameters from the clauses to the main query.
        foreach (var clause in clauses)
        {
            foreach (var parameterName in clause.Parameters.ParameterNames)
            {
                var parameterValue = clause.Parameters.Get<object>(parameterName);
                query.Parameters.Add(parameterName, parameterValue);
            }
        }
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
            SerializedBookmarkPayload = _payloadSerializer.Serialize(source.BookmarkPayload),
            SerializedInput = source.Input != null ? _payloadSerializer.Serialize(source.Input) : default,
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
            BookmarkPayload = _payloadSerializer.Deserialize(source.SerializedBookmarkPayload),
            Input = source.SerializedInput != null ? _payloadSerializer.Deserialize<Dictionary<string, object>>(source.SerializedInput) : default,
            CreatedAt = source.CreatedAt,
            ExpiresAt = source.ExpiresAt,
        };
    }
}