using Elsa.Common.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Creates <see cref="IWorkflowHost"/> objects.
/// </summary>
public interface IWorkflowHostFactory
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="versionOptions">The version options.</param>
    /// <param name="definitionId">The workflow definition ID.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost?> CreateAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);

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