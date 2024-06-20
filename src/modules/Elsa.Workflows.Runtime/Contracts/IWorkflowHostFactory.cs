using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Creates <see cref="IWorkflowHost"/> objects.
/// </summary>
[Obsolete("Use IWorkflowRuntime, IWorkflowRunner and IWorkflowInvoker services instead.")]
public interface IWorkflowHostFactory
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="workflowGraph">The workflow.</param>
    /// <param name="options">The options to use when creating the workflow host.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(WorkflowGraph workflowGraph, WorkflowHostOptions? options = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="workflowGraph">The workflow.</param>
    /// <param name="workflowState">The workflow state to initialize the workflow host with.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(WorkflowGraph workflowGraph, WorkflowState workflowState, CancellationToken cancellationToken = default);
}