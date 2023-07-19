using Elsa.Workflows.Core.State;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents a store of <see cref="WorkflowState"/>, used by the <see cref="DefaultWorkflowRuntime"/>.
/// </summary>
public interface IWorkflowStateStore
{
    /// <summary>
    /// Save the specified <see cref="WorkflowState"/> to te persistence store.
    /// </summary>
    ValueTask SaveAsync(string id, WorkflowState state, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Load the <see cref="WorkflowState"/> by the specified ID. 
    /// </summary>
    ValueTask<WorkflowState?> FindAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Count the number of workflow states based on the provided <see cref="CountRunningWorkflowsArgs"/>. 
    /// </summary>
    ValueTask<long> CountAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all workflow states matching the specified filter.
    /// </summary>
    /// <param name="filter">The filter to apply.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The number of deleted workflow states.</returns>
    Task<long> DeleteManyAsync(WorkflowStateFilter filter, CancellationToken cancellationToken = default);
}