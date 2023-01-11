using System.DirectoryServices.Protocols;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Elasticsearch.Common;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Elasticsearch.Modules.Management;

public class ElasticWorkflowInstanceStore : IWorkflowInstanceStore
{
    private readonly ElasticStore<WorkflowInstance> _store;
    
    public ElasticWorkflowInstanceStore(ElasticStore<WorkflowInstance> store)
    {
        _store = store;
    }

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

    public async Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default) => 
        await _store.SaveAsync(record, cancellationToken);

    public async Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default) => 
        await _store.SaveManyAsync(records, cancellationToken);

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default) => 
        await _store.DeleteByIdAsync(id, cancellationToken);

    public async Task<int> DeleteManyAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => 
        (int)await _store.DeleteManyAsync(ids.Select(id => new WorkflowInstance {Id = id}), cancellationToken);

    public async Task DeleteManyByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default) =>
        await _store.DeleteByQueryAsync(desc => desc
                .Query(q => q
                    .Term(t => t
                        .Field(f => f.DefinitionId)
                        .Value(definitionId))), 
            cancellationToken);

    public async Task<Page<WorkflowInstanceSummary>> FindManyAsync(FindWorkflowInstancesArgs args, CancellationToken cancellationToken = default)
    {
        // var queryDescriptor = new QueryContainerDescriptor<WorkflowInstance>();
        var searchDescriptor = new SearchRequestDescriptor<WorkflowInstance>();
        var query = new QueryDescriptor<WorkflowInstance>();

        var (searchTerm, definitionId, version, correlationId, workflowStatus, workflowSubStatus, pageArgs, orderBy,
            orderDirection) = args;

        if (!string.IsNullOrWhiteSpace(definitionId))
            query = query.Match(m => m.Field(f => f.DefinitionId).Query(definitionId));

        if (version != null)
            query = query.Match(m => m.Field(f => f.Version).Query(args.Version.ToString()));

        if (!string.IsNullOrWhiteSpace(correlationId))
            query = query.Match(m => m.Field(f => f.CorrelationId).Query(args.CorrelationId));

        if (workflowStatus != null)
            query = query.Match(m => m.Field(f => f.Status).Query(args.WorkflowStatus?.ToString()));

        if (workflowSubStatus != null)
            query = query.Match(m => m.Field(f => f.SubStatus).Query(args.WorkflowSubStatus?.ToString()));

        if(!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query
                .MultiMatch(c => c
                    .Type(TextQueryType.Phrase)
                    .Fields(new []
                    {
                        nameof(WorkflowInstance.Id), 
                        nameof(WorkflowInstance.Name), 
                        nameof(WorkflowInstance.DefinitionId), 
                        nameof(WorkflowInstance.CorrelationId)
                    })
                    .Lenient()
                    .Query(searchTerm));
        }
        
        searchDescriptor = orderBy switch
        {
            OrderBy.Finished => orderDirection == OrderDirection.Ascending
                ? searchDescriptor.Sort(x => x.Field(f => f.FinishedAt, cfg => cfg.Order(SortOrder.Asc)))
                : searchDescriptor.Sort(x => x.Field(f => f.FinishedAt, cfg => cfg.Order(SortOrder.Desc))),
            OrderBy.LastExecuted => orderDirection == OrderDirection.Ascending
                ? searchDescriptor.Sort(x => x.Field(f => f.LastExecutedAt, cfg => cfg.Order(SortOrder.Asc)))
                : searchDescriptor.Sort(x => x.Field(f => f.LastExecutedAt, cfg => cfg.Order(SortOrder.Desc))),
            OrderBy.Created => orderDirection == OrderDirection.Ascending
                ? searchDescriptor.Sort(x => x.Field(f => f.CreatedAt, cfg => cfg.Order(SortOrder.Asc)))
                : searchDescriptor.Sort(x => x.Field(f => f.CreatedAt, cfg => cfg.Order(SortOrder.Desc))),
            _ => searchDescriptor
        };

        var result = await _store.SearchAsync(s => searchDescriptor.Query(query), args.PageArgs, cancellationToken);
        return new Page<WorkflowInstanceSummary>(result.Items.Select(WorkflowInstanceSummary.FromInstance).ToList(),
            result.TotalCount);
    }
}