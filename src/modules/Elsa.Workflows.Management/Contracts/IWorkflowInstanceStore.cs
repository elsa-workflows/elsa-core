using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Contracts;

/// <summary>
/// Represents a store of workflow instances.
/// </summary>
public interface IWorkflowInstanceStore
{
    Task<WorkflowInstance?> FindAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
    Task<Page<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);
    Task<Page<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowInstance>> FindManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowInstance>> FindManyAsync<TOrderBy>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default);
    Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);
    Task<Page<WorkflowInstanceSummary>> SummarizeManyAsync<TOrderBy>(WorkflowInstanceFilter filter, PageArgs pageArgs, WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowInstanceSummary>> SummarizeManyAsync<TOrder>(WorkflowInstanceFilter filter, WorkflowInstanceOrder<TOrder> order, CancellationToken cancellationToken = default);
    Task SaveAsync(WorkflowInstance record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowInstance> records, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
    Task<int> DeleteManyAsync(WorkflowInstanceFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>
/// A filter for querying workflow instances.
/// </summary>
public class WorkflowInstanceFilter
{
    public string? Id { get; set; }
    public ICollection<string>? Ids { get; set; }
    public string? SearchTerm { get; set; }
    public string? DefinitionId { get; set; }
    public string? DefinitionVersionId { get; set; }
    public ICollection<string>? DefinitionIds { get; set; }
    public ICollection<string>? DefinitionVersionIds { get; set; }
    public int? Version { get; set; }
    public string? CorrelationId { get; set; }
    public ICollection<string>? CorrelationIds { get; set; }
    public WorkflowStatus? WorkflowStatus { get; set; }
    public WorkflowSubStatus? WorkflowSubStatus { get; set; }

    public IQueryable<WorkflowInstance> Apply(IQueryable<WorkflowInstance> query)
    {
        var filter = this;
        if (!string.IsNullOrWhiteSpace(filter.Id)) query = query.Where(x => x.Id == filter.Id);
        if (filter.Ids != null) query = query.Where(x => filter.Ids.Contains(x.Id));
        if (!string.IsNullOrWhiteSpace(filter.DefinitionId)) query = query.Where(x => x.DefinitionId == filter.DefinitionId);
        if (!string.IsNullOrWhiteSpace(filter.DefinitionVersionId)) query = query.Where(x => x.DefinitionVersionId == filter.DefinitionVersionId);
        if (filter.DefinitionIds != null) query = query.Where(x => filter.DefinitionIds.Contains(x.DefinitionId));
        if (filter.DefinitionVersionIds != null) query = query.Where(x => filter.DefinitionVersionIds.Contains(x.DefinitionVersionId));
        if (filter.Version != null) query = query.Where(x => x.Version == filter.Version);
        if (!string.IsNullOrWhiteSpace(filter.CorrelationId)) query = query.Where(x => x.CorrelationId == filter.CorrelationId);
        if (filter.CorrelationIds != null) query = query.Where(x => filter.CorrelationIds.Contains(x.CorrelationId!));
        if (filter.WorkflowStatus != null) query = query.Where(x => x.Status == filter.WorkflowStatus);
        if (filter.WorkflowSubStatus != null) query = query.Where(x => x.SubStatus == filter.WorkflowSubStatus);

        var searchTerm = filter.SearchTerm;
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query =
                from instance in query
                where instance.Name!.Contains(searchTerm)
                      || instance.Id.Contains(searchTerm)
                      || instance.DefinitionId.Contains(searchTerm)
                      || instance.CorrelationId!.Contains(searchTerm)
                select instance;
        }

        return query;
    }
}

/// <summary>
/// Represents the order by which to order the results of a query.
/// </summary>
public class WorkflowInstanceOrder<TProp> : OrderDefinition<WorkflowInstance, TProp>
{
}