using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents a single workflow instance that can be executed and takes care of publishing various lifecycle events.
/// </summary>
public interface IWorkflowHost
{
    /// <summary>
    /// Gets or sets the workflow definition.
    /// </summary>
    Workflow Workflow { get; set; }

    /// <summary>
    /// Gets or sets the workflow state.
    /// </summary>
    WorkflowState WorkflowState { get; set; }

    /// <summary>
    /// Returns a value indicating whether the specified workflow can start a new instance or not.
    /// </summary>
    Task<bool> CanStartWorkflowAsync(RunWorkflowParams? @params = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Starts a new workflow instance.
    /// </summary>
    Task<RunWorkflowResult> RunWorkflowAsync(RunWorkflowParams? @params = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels the workflow instance.
    /// </summary>
    Task CancelWorkflowAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Persists the workflow state.
    /// </summary>
    Task PersistStateAsync(CancellationToken cancellationToken = default);
}