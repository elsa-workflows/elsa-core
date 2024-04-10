using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents a single workflow instance that can be executed and takes care of publishing various lifecycle events.
/// </summary>
public interface IWorkflowHost
{
    /// <summary>
    /// The workflow definition.
    /// </summary>
    Workflow Workflow { get; set; }

    /// <summary>
    /// The workflow state.
    /// </summary>
    WorkflowState WorkflowState { get; set; }

    /// <summary>
    /// Returns a value indicating whether the specified workflow can start a new instance or not.
    /// </summary>
    Task<bool> CanStartWorkflowAsync(StartWorkflowHostParams? @params = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a new workflow instance and execute it.
    /// </summary>
    Task<StartWorkflowHostResult> StartWorkflowAsync(StartWorkflowHostParams? @params = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume an existing workflow instance.
    /// </summary>
    Task<ResumeWorkflowHostResult> ResumeWorkflowAsync(ResumeWorkflowHostParams? @params = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Persist the workflow state.
    /// </summary>
    Task PersistStateAsync(CancellationToken cancellationToken = default);
}