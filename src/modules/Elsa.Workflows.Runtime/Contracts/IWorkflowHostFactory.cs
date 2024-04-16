using Elsa.Workflows.Activities;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Creates <see cref="IWorkflowHost"/> objects.
/// </summary>
public interface IWorkflowHostFactory
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="instanceId">The instance ID to initialize the workflow host with. If not provided, a new instance ID will be generated.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(Workflow workflow, string? instanceId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="workflowState">The workflow state to initialize the workflow host with.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken = default);
}