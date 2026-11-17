using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Persistence.Elasticsearch.Common;
using Elsa.Persistence.Elasticsearch.Shared.Models;
using Elsa.Extensions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.OrderDefinitions;
using JetBrains.Annotations;

namespace Elsa.Persistence.Elasticsearch.Modules.Runtime;

/// <summary>
/// Store and retrieves <see cref="WorkflowExecutionLogRecord"/> objects from an Elasticsearch.
/// </summary>
[UsedImplicitly]
public class ElasticWorkflowExecutionLogStore(ElasticStore<WorkflowExecutionLogRecord> store) : IWorkflowExecutionLogStore
{
    /// <inheritdoc />
    public async Task AddAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await store.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowExecutionLogRecord record, CancellationToken cancellationToken = default)
    {
        await store.SaveAsync(record, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowExecutionLogRecord> records, CancellationToken cancellationToken = default)
    {
        await store.SaveManyAsync(records, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        var result = await store.SearchAsync(d => Filter(d, filter), PageArgs.FromRange(0, 1), cancellationToken);
        return result.Items.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<WorkflowExecutionLogRecord?> FindAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        var result = await store.SearchAsync(d => Sort(Filter(d, filter), order), PageArgs.FromRange(0, 1), cancellationToken);
        return result.Items.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default)
    {
        return await store.SearchAsync(d => Filter(d, filter), pageArgs, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Page<WorkflowExecutionLogRecord>> FindManyAsync<TOrderBy>(WorkflowExecutionLogRecordFilter filter, PageArgs pageArgs, WorkflowExecutionLogRecordOrder<TOrderBy> order, CancellationToken cancellationToken = default)
    {
        return await store.SearchAsync(d => Sort(Filter(d, filter), order), pageArgs, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> DeleteManyAsync(WorkflowExecutionLogRecordFilter filter, CancellationToken cancellationToken = default)
    {
        return await store.DeleteByQueryAsync(d => Filter(d, filter), cancellationToken);
    }

    private static SearchRequestDescriptor<WorkflowExecutionLogRecord> Sort<TProp>(SearchRequestDescriptor<WorkflowExecutionLogRecord> descriptor, WorkflowExecutionLogRecordOrder<TProp> order)
    {
        var propName = order.KeySelector.GetProperty()!.Name;
        var orderField = new OrderField(propName, order.Direction);
        return Sort(descriptor, orderField);
    }
    
    private static SearchRequestDescriptor<WorkflowExecutionLogRecord> Sort(SearchRequestDescriptor<WorkflowExecutionLogRecord> descriptor, params OrderField[] orderFields)
    {
        var sortDescriptor = new SortOptionsDescriptor<WorkflowExecutionLogRecord>();
        
        foreach (var orderField in orderFields)
        {
            var sortOrder = orderField.Direction == OrderDirection.Ascending ? SortOrder.Asc : SortOrder.Desc;
            sortDescriptor.Field(orderField.Field, f => f.Order(sortOrder));    
        }
        
        descriptor.Sort(sortDescriptor);
        return descriptor;
    }

    private static SearchRequestDescriptor<WorkflowExecutionLogRecord> Filter(SearchRequestDescriptor<WorkflowExecutionLogRecord> descriptor, WorkflowExecutionLogRecordFilter filter) => descriptor.Query(query => Filter(query, filter));
    private static DeleteByQueryRequestDescriptor<WorkflowExecutionLogRecord> Filter(DeleteByQueryRequestDescriptor<WorkflowExecutionLogRecord> descriptor, WorkflowExecutionLogRecordFilter filter) => descriptor.Query(query => Filter(query, filter));

    private static QueryDescriptor<WorkflowExecutionLogRecord> Filter(QueryDescriptor<WorkflowExecutionLogRecord> descriptor, WorkflowExecutionLogRecordFilter filter)
    {
        var filters = new List<Query>();
        var exclusions = new List<Query>();

        if (filter.Id != null)
            filters.AddDocumentIds([filter.Id]);

        filters.AddDocumentIds(filter.Ids);
        filters.AddKeyword("workflowInstanceId", filter.WorkflowInstanceId);
        filters.AddKeywords("workflowInstanceId", filter.WorkflowInstanceIds);
        filters.AddKeyword("parentActivityInstanceId", filter.ParentActivityInstanceId);
        filters.AddKeyword("activityId", filter.ActivityId);
        filters.AddKeywords("activityId", filter.ActivityIds);
        filters.AddKeyword("activityNodeId", filter.ActivityNodeId);
        filters.AddKeywords("activityNodeId", filter.ActivityNodeIds);
        filters.AddKeyword("eventName", filter.EventName);
        filters.AddKeywords("eventName", filter.EventNames);
        exclusions.AddKeyword("activityType", filter.ExcludeActivityType);
        exclusions.AddKeywords("activityType", filter.ExcludeActivityTypes);

        return filters.Count == 0 && exclusions.Count == 0
            ? descriptor.MatchAll(new MatchAllQuery())
            : descriptor.Bool(new BoolQuery { Filter = filters, MustNot = exclusions });
    }
}
