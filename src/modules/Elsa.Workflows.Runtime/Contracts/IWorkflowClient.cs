using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a client that can interact with a workflow instance.
/// </summary>
public interface IWorkflowClient
{
    /// <summary>
    /// Gets the ID of the workflow instance.
    /// </summary>
    string WorkflowInstanceId { get; }
    
    /// <summary>
    /// Creates a new workflow instance for the specified workflow definition version.
    /// </summary>
    Task<CreateWorkflowInstanceResponse> CreateInstanceAsync(CreateWorkflowInstanceRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes the workflow instance and waits for it to complete or reach a suspend point.
    /// </summary>
    Task<RunWorkflowInstanceResponse> RunInstanceAsync(RunWorkflowInstanceRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new workflow instance and executes it.
    /// </summary>
    Task<RunWorkflowInstanceResponse> CreateAndRunInstanceAsync(CreateAndRunWorkflowInstanceRequest request, CancellationToken cancellationToken = default);
    
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
    
    Task<bool> InstanceExistsAsync(CancellationToken cancellationToken = default);
}