using Elsa.Common.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Creates <see cref="IWorkflowHost"/> objects.
/// </summary>
public interface IWorkflowHostFactory
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="versionOptions">The version options.</param>
    /// <param name="definitionId">The workflow definition ID.</param>
    /// /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost?> CreateAsync(string definitionId, VersionOptions versionOptions, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="workflowDefinition">The workflow definition.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(WorkflowDefinition workflowDefinition, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="workflowState">The workflow state to initialize the workflow host with.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(Workflow workflow, WorkflowState workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new <see cref="IWorkflowHost"/> object.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task<IWorkflowHost> CreateAsync(Workflow workflow, CancellationToken cancellationToken = default);
}