using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents a client that can interact with a workflow instance.
/// </summary>
public interface IWorkflowClient
{
    /// <summary>
    /// Called by the factory to initialize the client.
    /// </summary>
    ValueTask InitializeAsync(string workflowDefinitionVersionId, string workflowInstanceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes the workflow instance and waits for it to complete or reach a suspend point.
    /// </summary>
    Task<ExecuteWorkflowResult> ExecuteAndWaitAsync(IExecuteWorkflowRequest? request = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes the workflow instance and returns immediately.
    /// </summary>
    Task ExecuteAndForgetAsync(IExecuteWorkflowRequest? request = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancels the execution of a workflow instance.
    /// </summary>
    Task CancelAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports the <see cref="WorkflowState"/> of the specified workflow instance.
    /// </summary>
    Task<WorkflowState> ExportStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports the specified <see cref="WorkflowState"/>.
    /// </summary>
    Task ImportStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);
}