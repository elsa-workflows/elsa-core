using Elsa.Workflows.Core.State;
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
    ValueTask<WorkflowState?> LoadAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Count the number of workflow states based on the provided <see cref="CountRunningWorkflowsArgs"/>. 
    /// </summary>
    ValueTask<long> CountAsync(CountRunningWorkflowsArgs args, CancellationToken cancellationToken = default);
}