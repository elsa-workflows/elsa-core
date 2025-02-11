using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Matches;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Params;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Represents a workflow runtime that can create <see cref="IWorkflowClient"/> instances connected to a workflow instance.
/// </summary>
public interface IWorkflowRuntime
{
    /// <summary>
    /// Creates a new <see cref="IWorkflowClient"/> instance.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A new <see cref="IWorkflowClient"/> instance.</returns>
    /// <remarks>The workflow instance doesn't exist yet, and a new workflow instance ID will be generated.</remarks>
    ValueTask<IWorkflowClient> CreateClientAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new <see cref="IWorkflowClient"/> instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance for which to create a client. If <c>null</c>, a new workflow instance ID will be generated.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A new <see cref="IWorkflowClient"/> instance.</returns>
    /// <remarks>The workflow instance itself doesn't have to exist yet.</remarks>
    ValueTask<IWorkflowClient> CreateClientAsync(string? workflowInstanceId, CancellationToken cancellationToken = default);
    
    
    /// <summary>
    /// Returns a value whether the specified workflow definition can create a new instance.
    /// </summary>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<CanStartWorkflowResult> CanStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = default);

    /// <summary>
    /// Creates a new workflow instance of the specified definition ID and executes it.
    /// </summary>
    /// <param name="definitionId">The workflow definition ID to run.</param>
    /// <param name="options">Options for starting the workflow.</param>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<WorkflowExecutionResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = default);

    /// <summary>
    /// Starts all workflows with triggers matching the specified activity type and bookmark payload.
    /// </summary>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<ICollection<WorkflowExecutionResult>> StartWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = default);

    /// <summary>
    /// Tries to start a workflow and returns the result if successful.
    /// </summary>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<WorkflowExecutionResult?> TryStartWorkflowAsync(string definitionId, StartWorkflowRuntimeParams? options = default);

    /// <summary>
    /// Resumes an existing workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance to resume.</param>
    /// <param name="options">Options for resuming the workflow.</param>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<WorkflowExecutionResult?> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeParams? options = default);

    /// <summary>
    /// Resumes all workflows that are bookmarked on the specified activity type. 
    /// </summary>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = default);

    /// <summary>
    /// Starts all workflows and resumes existing workflow instances based on the specified activity type and bookmark payload.
    /// </summary>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions? options = default);

    /// <summary>
    /// Executes a pending workflow.
    /// </summary>
    /// <param name="match">A workflow match to execute.</param>
    /// <param name="options">Options for executing the workflow.</param>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowMatch match, ExecuteWorkflowParams? options = default);

    /// <summary>
    /// Cancels the execution of a workflow.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance to cancel.</param>
    /// <param name="cancellationToken"></param>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<CancellationResult> CancelWorkflowAsync(string workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all the workflows that can be started or resumed based on a query model.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="cancellationToken"></param>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<IEnumerable<WorkflowMatch>> FindWorkflowsAsync(WorkflowsFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the <see cref="WorkflowState"/> of the specified workflow instance.
    /// </summary>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<WorkflowState?> ExportWorkflowStateAsync(string workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports the specified <see cref="WorkflowState"/>.
    /// </summary>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the specified bookmark.
    /// </summary>
    /// <param name="bookmark">The bookmark to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task UpdateBookmarkAsync(StoredBookmark bookmark, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of workflow instances based on the provided query args.
    /// </summary>
    [Obsolete("Use the client API instead, retrieved from CreateClientAsync")]
    Task<long> CountRunningWorkflowsAsync(CountRunningWorkflowsRequest request, CancellationToken cancellationToken = default);
}