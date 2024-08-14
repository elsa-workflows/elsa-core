using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Extracts triggers from workflow definitions.
/// </summary>
public interface ITriggerIndexer
{
    /// <summary>
    /// Removes triggers matching the specified filter.
    /// </summary>
    Task DeleteTriggersAsync(TriggerFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes triggers of the specified workflow.
    /// </summary>
    Task<IndexedWorkflowTriggers> IndexTriggersAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Indexes triggers of the specified workflow.
    /// </summary>
    Task<IndexedWorkflowTriggers> IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns triggers for the specified workflow definition.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A collection of triggers.</returns>
    Task<IEnumerable<StoredTrigger>> GetTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default);
}