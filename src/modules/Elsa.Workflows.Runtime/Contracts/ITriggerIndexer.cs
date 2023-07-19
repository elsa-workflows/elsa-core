using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Extracts triggers from workflow definitions.
/// </summary>
public interface ITriggerIndexer
{
    /// <summary>
    /// Indexes triggers of the deleted workflow definitions.
    /// </summary>
    Task<IndexedWorkflowTriggers> IndexTriggersDeleteWorkflowAsync(ICollection<string> workflowDefinitionIds, CancellationToken cancellationToken = default);

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
    /// <param name="definition">The workflow definition.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A collection of triggers.</returns>
    Task<IEnumerable<StoredTrigger>> GetTriggersAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns triggers for the specified workflow definition.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A collection of triggers.</returns>
    Task<IEnumerable<StoredTrigger>> GetTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default);
}