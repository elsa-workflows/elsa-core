using Elsa.Common.Models;
using Elsa.Workflows.Models;
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
    /// <param name="workflowGraph">The workflow.</param>
    /// <param name="workflowState">The workflow state to initialize the workflow host with.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(WorkflowGraph workflowGraph, WorkflowState workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="workflowGraph">The workflow.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(WorkflowGraph workflowGraph, CancellationToken cancellationToken = default);
}