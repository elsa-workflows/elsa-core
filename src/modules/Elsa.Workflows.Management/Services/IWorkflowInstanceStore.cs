using Elsa.Common.Entities;
using Elsa.Common.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Services;

public class WorkflowInstanceFilter
{
    public string? Id { get; set; }
    public ICollection<string>? Ids { get; set; }
    public string? SearchTerm { get; set; }
    public string? DefinitionId { get; set; }
    public ICollection<string>? DefinitionIds { get; set; }
    public int? Version { get; set; }
    public string? CorrelationId { get; set; }
    public ICollection<string>? CorrelationIds { get; set; }
    public WorkflowStatus? WorkflowStatus { get; set; }
    public WorkflowSubStatus? WorkflowSubStatus { get; set; }
}

public class WorkflowInstanceOrder<TProp> : OrderDefinition<WorkflowInstance, TProp>
{
}

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