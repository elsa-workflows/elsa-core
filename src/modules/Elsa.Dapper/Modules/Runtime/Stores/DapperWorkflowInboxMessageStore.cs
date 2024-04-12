using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Dapper.Extensions;
using Elsa.Dapper.Models;
using Elsa.Dapper.Modules.Runtime.Records;
using Elsa.Dapper.Services;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;

namespace Elsa.Dapper.Modules.Runtime.Stores;

/// <summary>
/// A Dapper-based <see cref="IBookmarkStore"/> implementation.
/// </summary>
[UsedImplicitly]
internal class DapperWorkflowInboxMessageStore(Store<WorkflowInboxMessageRecord> store, IPayloadSerializer payloadSerializer)
    : IWorkflowInboxMessageStore
{
    /// <inheritdoc />
    public async ValueTask SaveAsync(WorkflowInboxMessage record, CancellationToken cancellationToken = default)
    {
        var mappedRecord = Map(record);
        await store.SaveAsync(mappedRecord, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(WorkflowInboxMessageFilter filter, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filter), cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInboxMessage>> FindManyAsync(IEnumerable<WorkflowInboxMessageFilter> filters, CancellationToken cancellationToken = default)
    {
        var records = await store.FindManyAsync(q => ApplyFilter(q, filters.ToArray()), cancellationToken);
        return Map(records);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteManyAsync(WorkflowInboxMessageFilter filter, PageArgs? pageArgs = default, CancellationToken cancellationToken = default)
    {
        if (pageArgs == null)
            return await store.DeleteAsync(q => ApplyFilter(q, filter), cancellationToken);

        return await store.DeleteAsync(q => ApplyFilter(q, filter), pageArgs, new[] { new OrderField(nameof(WorkflowInboxMessage.CreatedAt), OrderDirection.Ascending) }, cancellationToken: cancellationToken);
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
            SerializedBookmarkPayload = payloadSerializer.Serialize(source.BookmarkPayload),
            SerializedInput = source.Input != null ? payloadSerializer.Serialize(source.Input) : default,
            CreatedAt = source.CreatedAt,
            ExpiresAt = source.ExpiresAt,
            TenantId = source.TenantId
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
            BookmarkPayload = payloadSerializer.Deserialize(source.SerializedBookmarkPayload),
            Input = source.SerializedInput != null ? payloadSerializer.Deserialize<Dictionary<string, object>>(source.SerializedInput) : default,
            CreatedAt = source.CreatedAt,
            ExpiresAt = source.ExpiresAt,
            TenantId = source.TenantId
        };
    }
}