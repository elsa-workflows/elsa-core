using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Workflows.Runtime.Models;

namespace Elsa.Workflows.Runtime.Services;

public interface ITriggerIndexer
{
    /// <summary>
    /// Indexes triggers of the specified workflow.
    /// </summary>
    Task<IndexedWorkflowTriggers> IndexTriggersAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Indexes triggers of the specified workflow.
    /// </summary>
    Task<IndexedWorkflowTriggers> IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default);
}