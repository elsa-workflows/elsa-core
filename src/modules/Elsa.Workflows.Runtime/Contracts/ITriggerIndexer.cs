using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Contracts;

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
    /// Send message of all triggers in the trigger store to populate the routetable on startup
    /// </summary>
    Task<IndexedWorkflowTriggers> IndexAllTriggersAsync(CancellationToken cancellationToken = default);
}