using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;
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