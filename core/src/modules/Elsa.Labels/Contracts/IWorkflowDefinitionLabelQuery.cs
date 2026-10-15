using Elsa.Labels.Entities;

namespace Elsa.Labels.Contracts;

/// <summary>
/// Queries workflow definition label relationships for workflow-definition filter providers.
/// </summary>
public interface IWorkflowDefinitionLabelQuery
{
    /// <summary>
    /// Returns workflow definition versions associated with any of the specified label IDs. Matching is based on the workflow definition version ID; an empty or unknown set returns no associations.
    /// </summary>
    Task<IEnumerable<WorkflowDefinitionLabel>> FindByLabelIdsAsync(IEnumerable<string> labelIds, CancellationToken cancellationToken = default);
}
