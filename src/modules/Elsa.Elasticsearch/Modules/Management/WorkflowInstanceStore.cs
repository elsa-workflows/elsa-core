using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Elasticsearch.Common;
using Elsa.Extensions;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Elasticsearch.Modules.Management;

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
    public async Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var result = await _store.SearchAsync(d => Filter(d, filter), new PageArgs(0, 1), cancellationToken);
        return result.Items.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default) =>
        await _store.SearchAsync(d => Filter(d, filter), pageArgs, cancellationToken);

    /// <inheritdoc />
    public async Task<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default) =>
        await _store.SearchAsync(d => Sort(Filter(d, filter), order), pageArgs, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default) =>
        await _store.SearchAsync(d => Filter(d, filter), cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.SearchAsync(d => Sort(Filter(d, filter), order), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => Summarize(Filter(d, filter)), pageArgs, cancellationToken);
        var summaries = results.Items.Select(WorkflowInstanceSummary.FromInstance).ToList();
        return new(summaries, results.TotalCount);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => Summarize(Sort(Filter(d, filter), order)), pageArgs, cancellationToken);
        var summaries = results.Items.Select(WorkflowInstanceSummary.FromInstance).ToList();
        return new(summaries, results.TotalCount);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => Summarize(Filter(d, filter)), cancellationToken);
        return results.Select(WorkflowInstanceSummary.FromInstance).ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrder>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrder> order, CancellationToken cancellationToken = default)
    {
        var results = await _store.SearchAsync(d => Summarize(Sort(Filter(d, filter), order)), cancellationToken);
        return results.Select(WorkflowInstanceSummary.FromInstance).ToList();
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default) =>
        await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default) =>
        await _store.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var result = await _store.DeleteByQueryAsync(d => Filter(d, filter), cancellationToken);
        return result > 0;
    }

    /// <inheritdoc />
    public async Task<int> DeleteManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default)
    {
        var result = await _store.DeleteByQueryAsync(d => Filter(d, filter), cancellationToken);
        return (int)result;
    }
    
    private static SearchRequestDescriptor<WorkflowInstance> Sort<TProp>(SearchRequestDescriptor<WorkflowInstance> descriptor, WorkflowInstanceOrder<TProp> order)
    {
        var sortDescriptor = new SortOptionsDescriptor<WorkflowInstance>();
        var propName = order.KeySelector.GetProperty()!.Name;
        var sortOrder = order.Direction == OrderDirection.Ascending ? SortOrder.Asc : SortOrder.Desc;
        sortDescriptor.Field(propName, f => f.Order(sortOrder));

        descriptor.Sort(sortDescriptor);
        return descriptor;
    }

    private static SearchRequestDescriptor<WorkflowInstance> Filter(SearchRequestDescriptor<WorkflowInstance> descriptor, WorkflowInstanceFilter filter) => descriptor.Query(query => Filter(query, filter));
    private static DeleteByQueryRequestDescriptor<WorkflowInstance> Filter(DeleteByQueryRequestDescriptor<WorkflowInstance> descriptor, WorkflowInstanceFilter filter) => descriptor.Query(query => Filter(query, filter));

    private static QueryDescriptor<WorkflowInstance> Filter(QueryDescriptor<WorkflowInstance> descriptor, WorkflowInstanceFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Id)) descriptor = descriptor.Match(m => m.Field(f => f.Id).Query(filter.Id));
        if (!string.IsNullOrWhiteSpace(filter.DefinitionId)) descriptor = descriptor.Match(m => m.Field(f => f.DefinitionId).Query(filter.DefinitionId));
        if (!string.IsNullOrWhiteSpace(filter.DefinitionVersionId)) descriptor = descriptor.Match(m => m.Field(f => f.DefinitionVersionId).Query(filter.DefinitionVersionId));
        
        // TODO: filter by IDs
        // TODO: filter by DefinitionIDs
        // TODO: filter by DefinitionVersionIDs
        // TODO: filter by CorrelationIDs
        
        if (filter.Version != null) descriptor = descriptor.Match(m => m.Field(f => f.Version).Query(filter.Version.ToString()!));
        if (!string.IsNullOrWhiteSpace(filter.CorrelationId)) descriptor = descriptor.Match(m => m.Field(f => f.CorrelationId).Query(filter.CorrelationId));
        if (filter.WorkflowStatus != null) descriptor = descriptor.Match(m => m.Field(f => f.Status).Query(filter.WorkflowStatus.ToString()!));
        if (filter.WorkflowSubStatus != null) descriptor = descriptor.Match(m => m.Field(f => f.SubStatus).Query(filter.WorkflowSubStatus.ToString()!));

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            descriptor = descriptor
                .QueryString(c => c
                    .Query(filter.SearchTerm));

        return descriptor;
    }

    private static SearchRequestDescriptor<WorkflowInstance> Summarize(SearchRequestDescriptor<WorkflowInstance> descriptor) =>
        descriptor.Fields(
            field => field.Field(f => f.Id),
            field => field.Field(f => f.DefinitionId),
            field => field.Field(f => f.Status),
            field => field.Field(f => f.SubStatus),
            field => field.Field(f => f.Version),
            field => field.Field(f => f.CorrelationId),
            field => field.Field(f => f.Version),
            field => field.Field(f => f.Name),
            field => field.Field(f => f.CreatedAt),
            field => field.Field(f => f.CancelledAt),
            field => field.Field(f => f.FaultedAt),
            field => field.Field(f => f.FinishedAt),
            field => field.Field(f => f.DefinitionVersionId),
            field => field.Field(f => f.LastExecutedAt)
        );
}