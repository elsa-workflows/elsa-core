using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Elasticsearch.Common;
using Elsa.Extensions;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Elasticsearch.Modules.Runtime;

/// <summary>
/// Store and retrieves <see cref="WorkflowExecutionLogRecord"/> objects from an Elasticsearch.
/// </summary>
public class ElasticWorkflowExecutionLogStore : IWorkflowExecutionLogStore
{
    private readonly ElasticStore<WorkflowExecutionLogRecord> _store;
    
    /// <summary>
    /// Constructor.
    /// </summary>
    public ElasticWorkflowExecutionLogStore(ElasticStore<WorkflowExecutionLogRecord> store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await _store.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var result = await _store.SearchAsync(d => Filter(d, filter), new PageArgs(0, 1), cancellationToken);
        return result.Items.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = await _store.SearchAsync(d => Sort(Filter(d, filter), order), new PageArgs(0, 1), cancellationToken);
        return result.Items.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await _store.SearchAsync(d => Filter(d, filter), pageArgs, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await _store.SearchAsync(d => Sort(Filter(d, filter), order), pageArgs, cancellationToken);
    }

    private static SearchRequestDescriptor<WorkflowExecutionLogRecord> Sort<TProp>(SearchRequestDescriptor<WorkflowExecutionLogRecord> descriptor, WorkflowExecutionLogRecordOrder<TProp> order)
    {
        var sortDescriptor = new SortOptionsDescriptor<WorkflowExecutionLogRecord>();
        var propName = order.KeySelector.GetProperty()!.Name;
        var sortOrder = order.Direction == OrderDirection.Ascending ? SortOrder.Asc : SortOrder.Desc;
        sortDescriptor.Field(propName, f => f.Order(sortOrder));

        descriptor.Sort(sortDescriptor);
        return descriptor;
    }

    private static SearchRequestDescriptor<WorkflowExecutionLogRecord> Filter(SearchRequestDescriptor<WorkflowExecutionLogRecord> descriptor, WorkflowExecutionLogRecordFilter filter) => descriptor.Query(query => Filter(query, filter));
    private static DeleteByQueryRequestDescriptor<WorkflowExecutionLogRecord> Filter(DeleteByQueryRequestDescriptor<WorkflowExecutionLogRecord> descriptor, WorkflowExecutionLogRecordFilter filter) => descriptor.Query(query => Filter(query, filter));

    private static QueryDescriptor<WorkflowExecutionLogRecord> Filter(QueryDescriptor<WorkflowExecutionLogRecord> descriptor, WorkflowExecutionLogRecordFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.WorkflowInstanceId)) descriptor = descriptor.Match(m => m.Field(f => f.WorkflowInstanceId).Query(filter.WorkflowInstanceId));
        if (!string.IsNullOrWhiteSpace(filter.ActivityId)) descriptor = descriptor.Match(m => m.Field(f => f.ActivityId).Query(filter.ActivityId));
        if (!string.IsNullOrWhiteSpace(filter.EventName)) descriptor = descriptor.Match(m => m.Field(f => f.EventName).Query(filter.EventName));
        
        return descriptor;
    }
}