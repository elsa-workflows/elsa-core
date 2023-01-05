using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Elasticsearch.Common;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Nest;

namespace Elsa.Elasticsearch.Modules.Management;

public class ElasticWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly ElasticStore<WorkflowInstance> _store;
    
    public ElasticWorkflowInstanceStore(ElasticStore<WorkflowInstance> store)
    {
        _store = store;
    }

    public async Task<WorkflowInstance?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => 
        await _store.GetByIdAsync(id, cancellationToken);

    public async Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default) => 
        await _store.SaveAsync(record, cancellationToken);

    public async Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default) => 
        await _store.SaveManyAsync(records, cancellationToken);

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => 
        await _store.DeleteByIdAsync(id, cancellationToken);

    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => 
        await _store.DeleteManyAsync(ids.Select(id => new WorkflowInstance { Id = id}), cancellationToken);

    public async Task DeleteManyByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.DeleteByQueryAsync(q => q
                .Term(t => t
                    .Field(f => f.DefinitionId)
                    .Value(definitionId)), 
            cancellationToken);

    public async Task<Page<WorkflowInstanceSummary>> FindManyAsync(FindWorkflowInstancesArgs args,
        CancellationToken cancellationToken = default)
    {
        var queryDescriptor = new QueryContainerDescriptor<WorkflowInstance>();
        var searchDescriptor = new SearchDescriptor<WorkflowInstance>();
        var query = new QueryContainer();

        var (searchTerm, definitionId, version, correlationId, workflowStatus, workflowSubStatus, pageArgs, orderBy,
            orderDirection) = args;

        if (!string.IsNullOrWhiteSpace(definitionId))
            query = queryDescriptor.Match(m => m.Field(f => f.DefinitionId).Query(args.DefinitionId));

        if (version != null)
            query &= queryDescriptor.Match(m => m.Field(f => f.Version).Query(args.Version.ToString()));

        if (!string.IsNullOrWhiteSpace(correlationId))
            query &= queryDescriptor.Match(m => m.Field(f => f.CorrelationId).Query(args.CorrelationId));

        if (workflowStatus != null)
            query &= queryDescriptor.Match(m => m.Field(f => f.Status).Query(args.WorkflowStatus?.ToString()));

        if (workflowSubStatus != null)
            query &= queryDescriptor.Match(m => m.Field(f => f.SubStatus).Query(args.WorkflowSubStatus?.ToString()));

        if(!string.IsNullOrWhiteSpace(searchTerm))
        {
            query &= queryDescriptor
                .MultiMatch(c => c
                    .Type(TextQueryType.Phrase)
                    .Fields(f => f
                        .Field(p => p.Name)
                        .Field(p => p.Id)
                        .Field(p => p.DefinitionId)
                        .Field(p => p.CorrelationId))
                    .Lenient()
                    .Query(searchTerm));
        }
        
        searchDescriptor = orderBy switch
        {
            OrderBy.Finished => orderDirection == OrderDirection.Ascending
                ? searchDescriptor.Sort(x => x.Ascending(f => f.FinishedAt))
                : searchDescriptor.Sort(x => x.Descending(f => f.FinishedAt)),
            OrderBy.LastExecuted => orderDirection == OrderDirection.Ascending
                ? searchDescriptor.Sort(x => x.Ascending(f => f.LastExecutedAt))
                : searchDescriptor.Sort(x => x.Descending(f => f.LastExecutedAt)),
            OrderBy.Created => orderDirection == OrderDirection.Ascending
                ? searchDescriptor.Sort(x => x.Ascending(f => f.CreatedAt))
                : searchDescriptor.Sort(x => x.Descending(f => f.CreatedAt)),
            _ => searchDescriptor
        };

        var result = await _store.SearchAsync(searchDescriptor.Query(_ => query), args.PageArgs, cancellationToken);
        return new Page<WorkflowInstanceSummary>(result.Items.Select(WorkflowInstanceSummary.FromInstance).ToList(),
            result.TotalCount);
    }
}