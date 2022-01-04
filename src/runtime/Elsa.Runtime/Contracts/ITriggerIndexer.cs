using Elsa.Models;

namespace Elsa.Runtime.Contracts;

public interface ITriggerIndexer
{
    /// <summary>
    /// Indexes triggers from all workflows.
    /// </summary>
    Task IndexTriggersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Indexes triggers of the specified workflow.
    /// </summary>
    Task IndexTriggersAsync(Workflow workflow, CancellationToken cancellationToken = default);
}