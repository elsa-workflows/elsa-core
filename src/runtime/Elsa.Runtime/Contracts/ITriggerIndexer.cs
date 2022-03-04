using Elsa.Models;
using Elsa.Persistence.Entities;
using Elsa.Runtime.Models;
using Elsa.Runtime.Notifications;

namespace Elsa.Runtime.Contracts;

public interface ITriggerIndexer
{
    /// <summary>
    /// Indexes triggers from all workflows.
    /// </summary>
    Task<ICollection<IndexedWorkflowTriggers>> IndexTriggersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Indexes triggers of the specified workflow.
    /// </summary>
    Task<IndexedWorkflowTriggers> IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default);
}