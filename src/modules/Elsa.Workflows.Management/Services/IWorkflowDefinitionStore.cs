using Elsa.Common.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.Services;

/// <summary>
/// A specification to use when finding workflow definitions. Only non-null fields will be included in the conditional expression.
/// </summary>
public class WorkflowDefinitionFilter
{
    public string? Id { get; set; }
    public ICollection<string>? Ids { get; set; }
    public string? DefinitionId { get; set; }
    public ICollection<string>? DefinitionIds { get; set; }
    public VersionOptions? VersionOptions { get; set; }
    public string? Name { get; set; }
    public ICollection<string>? Names { get; set; }
    public string? MaterializerName { get; set; }
    public bool? UsableAsActivity { get; set; }
}

/// <summary>
/// Represents a store of <see cref="WorkflowDefinition"/>s.
/// </summary>
public interface IWorkflowDefinitionStore
{
    Task<WorkflowDefinition?> FindAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
    Task<Page<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowDefinition>> FindManyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
    Task<Page<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, PageArgs pageArgs, CancellationToken cancellationToken = default);
    Task<IEnumerable<WorkflowDefinitionSummary>> FindSummariesAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> FindLastVersionAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken);
    Task SaveAsync(WorkflowDefinition record, CancellationToken cancellationToken = default);
    Task SaveManyAsync(IEnumerable<WorkflowDefinition> records, CancellationToken cancellationToken = default);
    Task<int> DeleteAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(WorkflowDefinitionFilter filter, CancellationToken cancellationToken = default);
}