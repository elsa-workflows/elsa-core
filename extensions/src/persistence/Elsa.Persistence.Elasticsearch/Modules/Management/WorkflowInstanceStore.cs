using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.Elasticsearch.Common;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Humanizer;

namespace Elsa.Persistence.Elasticsearch.Modules.Management;

/// <summary>
/// Stores and retrieves workflow instances from Elasticsearch.
/// </summary>
public class ElasticWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly ElasticStore<WorkflowInstance> _store;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ElasticWorkflowInstanceStore(ElasticStore<WorkflowInstance> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async ValueTask<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var result = await _store.SearchAsync(d => Filter(d, filter), PageArgs.FromRange(0, 1), cancellationToken);
        return result.Items.FirstOrDefault();
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await _store.SearchAsync(d => Filter(d, filter), pageArgs, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.SearchAsync(d => Sort(Filter(d, filter), order), pageArgs, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.SearchAsync(d => Filter(d, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.SearchAsync(d => Sort(Filter(d, filter), order), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> CountAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.CountAsync(d => Filter(d, filter), cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => Summarize(Filter(d, filter)), pageArgs, cancellationToken);
        var summaries = results.Items.Select(WorkflowInstanceSummary.FromInstance).ToList();
        return new(summaries, results.TotalCount);
    }

    /// <inheritdoc />
    public async ValueTask<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => Summarize(Sort(Filter(d, filter), order)), pageArgs, cancellationToken);
        var summaries = results.Items.Select(WorkflowInstanceSummary.FromInstance).ToList();
        return new(summaries, results.TotalCount);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => SelectId(Filter(d, filter)), cancellationToken);
        return results.Select(x => x.Id).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<Page<string>> FindManyIdsAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => SelectId(Filter(d, filter)), pageArgs, cancellationToken);
        return new(results.Items.Select(x => x.Id).ToList(), results.TotalCount);
    }

    /// <inheritdoc />
    public async ValueTask<Page<string>> FindManyIdsAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => SelectId(Sort(Filter(d, filter), order)), pageArgs, cancellationToken);
        return new(results.Items.Select(x => x.Id).ToList(), results.TotalCount);
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => Summarize(Filter(d, filter)), cancellationToken);
        return results.Select(WorkflowInstanceSummary.FromInstance).ToList();
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrder>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrder> order, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => Summarize(Sort(Filter(d, filter), order)), cancellationToken);
        return results.Select(WorkflowInstanceSummary.FromInstance).ToList();
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(instance, cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="WorkflowInstanceAlreadyExistsException">An instance already uses the specified ID.</exception>
    public async ValueTask AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        if (!await _store.TryCreateAsync(instance, instance.Id, cancellationToken))
            throw new WorkflowInstanceAlreadyExistsException(instance.Id);
    }

    public async ValueTask UpdateAsync(WorkflowInstance instance, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(instance, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask SaveManyAsync(IEnumerable<WorkflowInstance> instances, CancellationToken cancellationToken = default)
    {
        await _store.SaveManyAsync(instances, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<long> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        return await _store.DeleteByQueryAsync(d => Filter(d, filter), cancellationToken);
    }

    public Task UpdateUpdatedTimestampAsync(string workflowInstanceId, DateTimeOffset value, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    /// <remarks>
    /// <paramref name="allowFinishedCancelled"/> is unused: drain no longer promotes Finished/Cancelled (#8419).
    /// The parameter remains so the 3.8.4 signature stays binary-compatible.
    /// </remarks>
    public async ValueTask<bool> TryMarkInterruptedAsync(string workflowInstanceId, CancellationToken cancellationToken = default, bool allowFinishedCancelled = false)
    {
        // Apply the status filter as part of the write so a stale snapshot cannot replace
        // a Finished instance (#8419). allowFinishedCancelled is ignored.
        var updated = await _store.UpdateByQueryAsync(d =>
        {
            d.Refresh(true);
            d.Query(q => q.Bool(b => b
                .Filter(new IdsQuery { Values = new[] { workflowInstanceId } })
                .MustNot(new TermQuery { Field = "status.keyword", Value = WorkflowStatus.Finished.ToString() })));

            d.Script(s => s
                .Source("ctx._source.status = params.status; ctx._source.subStatus = params.subStatus; ctx._source.isExecuting = params.isExecuting;")
                .AddParam("status", WorkflowStatus.Running.ToString())
                .AddParam("subStatus", WorkflowSubStatus.Interrupted.ToString())
                .AddParam("isExecuting", false));
        }, cancellationToken);

        return updated > 0;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryTransitionSubStatusAsync(
        string workflowInstanceId,
        WorkflowSubStatus expected,
        DateTimeOffset? expectedUpdatedAt,
        WorkflowSubStatus next,
        DateTimeOffset stampedAt,
        CancellationToken cancellationToken = default)
    {
        if (!expected.IsPermittedSubStatusTransition(next))
            return false;

        // Conditional write: do not SaveAsync a Find snapshot. The script no-ops unless the source still matches,
        // and a no-op does not increment the updated count. updatedAt is compared as a round-trip string.
        var updated = await _store.UpdateByQueryAsync(d =>
        {
            d.Refresh(true);
            // Default conflicts=abort throws on a version conflict. Proceed reports the document as not updated,
            // which is the compare-and-swap result for both the mark and the restore.
            d.Conflicts(Conflicts.Proceed);
            d.Query(q => q.Ids(new IdsQuery { Values = new[] { workflowInstanceId } }));
            d.Script(s => s
                .Source("""
                    if (ctx._source.id == params.workflowInstanceId
                        && ctx._source.status == params.running
                        && ctx._source.subStatus == params.expected
                        && (params.checkUpdatedAt == false || ctx._source.updatedAt == params.expectedUpdatedAt)
                        && (params.requireNotExecuting == false || (ctx._source.isExecuting != true && ctx._source.subStatus != params.executing))) {
                      ctx._source.subStatus = params.next;
                      ctx._source.updatedAt = params.stampedAt;
                    } else {
                      ctx.op = 'noop';
                    }
                    """)
                .AddParam("workflowInstanceId", workflowInstanceId)
                .AddParam("running", WorkflowStatus.Running.ToString())
                .AddParam("expected", expected.ToString())
                .AddParam("checkUpdatedAt", expectedUpdatedAt.HasValue)
                .AddParam("expectedUpdatedAt", expectedUpdatedAt?.ToString("O") ?? string.Empty)
                .AddParam("requireNotExecuting", next == WorkflowSubStatus.Cancelling)
                .AddParam("executing", WorkflowSubStatus.Executing.ToString())
                .AddParam("next", next.ToString())
                .AddParam("stampedAt", stampedAt.ToString("O")));
        }, cancellationToken);

        return updated > 0;
    }

    private static SearchRequestDescriptor<WorkflowInstance> Sort<TProp>(SearchRequestDescriptor<WorkflowInstance> descriptor, WorkflowInstanceOrder<TProp> order)
    {
        var sortDescriptor = new SortOptionsDescriptor<WorkflowInstance>();
        var propName = order.KeySelector.GetPropertyName().Camelize();
        var sortOrder = order.Direction == OrderDirection.Ascending ? SortOrder.Asc : SortOrder.Desc;
        sortDescriptor.Field(propName, f => f.Order(sortOrder));

        descriptor.Sort(sortDescriptor);
        return descriptor;
    }

    private static SearchRequestDescriptor<WorkflowInstance> Filter(SearchRequestDescriptor<WorkflowInstance> descriptor, WorkflowInstanceFilter filter)
    {
        return descriptor.Query(query => Filter(query, filter));
    }

    private static CountRequestDescriptor<WorkflowInstance> Filter(CountRequestDescriptor<WorkflowInstance> descriptor, WorkflowInstanceFilter filter)
    {
        return descriptor.Query(query => Filter(query, filter));
    }

    private static DeleteByQueryRequestDescriptor<WorkflowInstance> Filter(DeleteByQueryRequestDescriptor<WorkflowInstance> descriptor, WorkflowInstanceFilter filter)
    {
        return descriptor.Query(query => Filter(query, filter));
    }

    private static QueryDescriptor<WorkflowInstance> Filter(QueryDescriptor<WorkflowInstance> descriptor, WorkflowInstanceFilter filter)
    {
        // Summaries used by deletion and the delete itself must apply the same restrictions.
        if (filter.TimestampFilters?.Count > 0)
            throw new NotSupportedException("Elasticsearch does not support custom workflow instance timestamp filters.");

        var filters = new List<Query>();

        if (filter.Id != null)
            filters.AddDocumentIds([filter.Id]);

        filters.AddDocumentIds(filter.Ids);
        filters.AddKeyword("definitionId", filter.DefinitionId);
        filters.AddKeywords("definitionId", filter.DefinitionIds);
        filters.AddKeyword("definitionVersionId", filter.DefinitionVersionId);
        filters.AddKeywords("definitionVersionId", filter.DefinitionVersionIds);
        filters.AddKeywords("parentWorkflowInstanceId", filter.ParentWorkflowInstanceIds);
        filters.AddKeyword("correlationId", filter.CorrelationId);
        filters.AddKeywords("correlationId", filter.CorrelationIds);
        filters.AddKeyword("status", filter.WorkflowStatus?.ToString());
        filters.AddKeywords("status", filter.WorkflowStatuses?.Select(status => status.ToString()).ToArray());
        filters.AddKeyword("subStatus", filter.WorkflowSubStatus?.ToString());
        filters.AddKeywords("subStatus", filter.WorkflowSubStatuses?.Select(status => status.ToString()).ToArray());
        filters.AddKeywords("name", filter.Names);

        if (filter.Version is { } version)
            filters.Add(new TermQuery { Field = "version", Value = version });
        if (filter.IsExecuting is { } isExecuting)
            filters.Add(new TermQuery { Field = "isExecuting", Value = isExecuting });
        if (filter.IsSystem is { } isSystem)
            filters.Add(new TermQuery { Field = "isSystem", Value = isSystem });
        if (filter.BeforeLastUpdated is { } beforeLastUpdated)
            filters.Add(new DateRangeQuery { Field = "updatedAt", Lt = beforeLastUpdated.UtcDateTime });
        if (filter.HasIncidents is { } hasIncidents)
            filters.Add(hasIncidents
                ? new NumberRangeQuery { Field = "incidentCount", Gt = 0 }
                : new TermQuery { Field = "incidentCount", Value = 0 });
        if (filter.Name is { } name)
        {
            var escapedName = name.Replace("\\", "\\\\").Replace("*", "\\*").Replace("?", "\\?");
            filters.Add(new WildcardQuery { Field = "name.keyword", Value = $"*{escapedName}*", CaseInsensitive = true });
        }
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            filters.Add(new QueryStringQuery { Query = filter.SearchTerm });

        return filters.Count == 0 ? descriptor.MatchAll(new MatchAllQuery()) : descriptor.Bool(new BoolQuery { Filter = filters });
    }

    private static SearchRequestDescriptor<WorkflowInstance> Summarize(SearchRequestDescriptor<WorkflowInstance> descriptor)
    {
        return descriptor.Fields(
            field => field.Field(f => f.Id),
            field => field.Field(f => f.DefinitionId),
            field => field.Field(f => f.Status),
            field => field.Field(f => f.SubStatus),
            field => field.Field(f => f.Version),
            field => field.Field(f => f.CorrelationId),
            field => field.Field(f => f.Version),
            field => field.Field(f => f.Name),
            field => field.Field(f => f.CreatedAt),
            field => field.Field(f => f.FinishedAt),
            field => field.Field(f => f.DefinitionVersionId),
            field => field.Field(f => f.UpdatedAt)
        );
    }

    private static SearchRequestDescriptor<WorkflowInstance> SelectId(SearchRequestDescriptor<WorkflowInstance> descriptor)
    {
        return descriptor.Fields(field => field.Field(f => f.Id));
    }
}
