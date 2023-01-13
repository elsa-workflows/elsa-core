using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Elasticsearch.Common;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

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
    public async Task<WorkflowInstance?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var instances = await _store
            .SearchAsync(desc => desc
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.Id)
                        .Query(id))), default, cancellationToken);

        return instances.Items.MaxBy(x => x.LastExecutedAt);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default) => 
        await _store.SaveAsync(record, cancellationToken);

    /// <inheritdoc />
    public async Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default) => 
        await _store.SaveManyAsync(records, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _store.DeleteByQueryAsync(desc => desc
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.Id)
                        .Value(id))), 
            cancellationToken);
        return result > 0;
    }

    /// <inheritdoc />
    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var result = await _store.DeleteByQueryAsync(desc => desc
                .Query(q => q
                    .Terms(t => t
                        .Field(f => f.Id)
                        .Terms(new TermsQueryField(ids.Select(FieldValue.String).ToList()))
                    )),
            cancellationToken);
        return (int)result;
    }

    /// <inheritdoc />
    public async Task DeleteManyByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.DeleteByQueryAsync(desc => desc
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.DefinitionId)
                        .Value(definitionId))), 
            cancellationToken);

    /// <inheritdoc />
    public async Task<Page<WorkflowInstanceSummary>> FindManyAsync(FindWorkflowInstancesArgs args, CancellationToken cancellationToken = default)
    {
        var sortDescriptor = new SortOptionsDescriptor<WorkflowInstance>();

        var (searchTerm, definitionId, version, correlationId, workflowStatus, workflowSubStatus, pageArgs, orderBy,
            orderDirection) = args;

        sortDescriptor = orderBy switch
        {
            OrderBy.Finished => orderDirection == OrderDirection.Ascending
                ? sortDescriptor.Field(f => f.FinishedAt!, cfg => cfg.Order(SortOrder.Asc))
                : sortDescriptor.Field(f => f.FinishedAt!, cfg => cfg.Order(SortOrder.Desc)),
            OrderBy.LastExecuted => orderDirection == OrderDirection.Ascending
                ? sortDescriptor.Field(f => f.LastExecutedAt!, cfg => cfg.Order(SortOrder.Asc))
                : sortDescriptor.Field(f => f.LastExecutedAt!, cfg => cfg.Order(SortOrder.Desc)),
            OrderBy.Created => orderDirection == OrderDirection.Ascending
                ? sortDescriptor.Field(f => f.CreatedAt, cfg => cfg.Order(SortOrder.Asc))
                : sortDescriptor.Field(f => f.CreatedAt, cfg => cfg.Order(SortOrder.Desc)),
            _ => sortDescriptor
        };

        var result = await _store.SearchAsync(s =>
        {
            if (!string.IsNullOrWhiteSpace(definitionId))
                s.Query(q => q.Match(m => m.Field(f => f.DefinitionId).Query(definitionId)));

            if (version != null)
                s.Query(q => q.Match(m => m.Field(f => f.Version).Query(version.ToString()!)));

            if (!string.IsNullOrWhiteSpace(correlationId))
                s.Query(q => q.Match(m => m.Field(f => f.CorrelationId).Query(correlationId)));

            if (workflowStatus != null)
                s.Query(q => q.Match(m => m.Field(f => f.Status).Query(workflowStatus.ToString()!)));

            if (workflowSubStatus != null)
                s.Query(q => q.Match(m => m.Field(f => f.SubStatus).Query(workflowSubStatus.ToString()!)));
            
            if(!string.IsNullOrWhiteSpace(searchTerm))
                s.Query(q => q
                    .QueryString(c => c
                        .Query(searchTerm)));

            s.Sort(sortDescriptor);
        }, args.PageArgs, cancellationToken);
        return new Page<WorkflowInstanceSummary>(result.Items.Select(WorkflowInstanceSummary.FromInstance).ToList(),
            result.TotalCount);
    }
}