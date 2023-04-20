using Elsa.Common.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Contracts;

/// <summary>
/// Represents a workflow runtime that can start, resume and find workflows.
/// </summary>
public interface IWorkflowRuntime
{
    /// <summary>
    /// Returns a value whether or not the specified workflow definition can create a new instance.
    /// </summary>
    Task<CanStartWorkflowResult> CanStartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new workflow instance of the specified definition ID and executes it.
    /// </summary>
    /// <param name="definitionId">The workflow definition ID to run.</param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    Task<WorkflowExecutionResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts all workflows with triggers matching the specified activity type and bookmark payload.
    /// </summary>
    /// <returns></returns>
    Task<ICollection<WorkflowExecutionResult>> StartWorkflowsAsync(
        string activityTypeName,
        object bookmarkPayload,
        TriggerWorkflowsRuntimeOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to start a workflow and returns the result if successful.
    /// </summary>
    Task<WorkflowExecutionResult?> TryStartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes an existing workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance to resume.</param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    Task<WorkflowExecutionResult> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes all workflows that are bookmarked on the specified activity type. 
    /// </summary>
    Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsRuntimeOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts all workflows and resumes existing workflow instances based on the specified activity type and bookmark payload.
    /// </summary>
    Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsRuntimeOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a pending workflow.
    /// </summary>
    /// <param name="match">A workflow match to execute.</param>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<WorkflowExecutionResult> ExecuteWorkflowAsync(WorkflowMatch match, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all the workflows that can be started or resumed based on a query model.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<WorkflowMatch>> FindWorkflowsAsync(WorkflowsFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports the <see cref="WorkflowState"/> of the specified workflow instance.
    /// </summary>
    Task<WorkflowState?> ExportWorkflowStateAsync(string workflowInstanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports the specified <see cref="WorkflowState"/>.
    /// </summary>
    Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds and removes bookmarks based on the provided bookmarks diff.
    /// </summary>
    Task UpdateBookmarksAsync(UpdateBookmarksContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the specified bookmark.
    /// </summary>
    /// <param name="bookmark">The bookmark to update.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateBookmarkAsync(StoredBookmark bookmark, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of workflow instances based on the provided query args.
    /// </summary>
    Task<int> CountRunningWorkflowsAsync(CountRunningWorkflowsArgs args, CancellationToken cancellationToken = default);
}

public record StartWorkflowRuntimeOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default, VersionOptions VersionOptions = default, string? TriggerActivityId = default, string? InstanceId = default);

public record ResumeWorkflowRuntimeOptions(
    string? CorrelationId = default, 
    string? WorkflowInstanceId = default, 
    string? BookmarkId = default, 
    string? ActivityId = default,
    string? ActivityNodeId = default,
    string? ActivityInstanceId = default,
    string? ActivityHash = default,
    IDictionary<string, object>? Input = default);

public record CanStartWorkflowResult(string? InstanceId, bool CanStart);

public record TriggerWorkflowsRuntimeOptions(string? CorrelationId = default, string? WorkflowInstanceId = default, IDictionary<string, object>? Input = default);

public record TriggerWorkflowsResult(ICollection<WorkflowExecutionResult> TriggeredWorkflows);

public record WorkflowExecutionResult(string WorkflowInstanceId, WorkflowStatus Status, WorkflowSubStatus SubStatus, ICollection<Bookmark> Bookmarks, string? TriggeredActivityId = null, WorkflowFaultState? Fault = default);

public record UpdateBookmarksContext(string InstanceId, Diff<Bookmark> Diff, string? CorrelationId);

public record WorkflowsFilter(string ActivityTypeName, object BookmarkPayload, TriggerWorkflowsRuntimeOptions Options);

public record WorkflowMatch(string WorkflowInstanceId, WorkflowInstance? WorkflowInstance, string? CorrelationId);

public record StartableWorkflowMatch(string WorkflowInstanceId, WorkflowInstance? WorkflowInstance, string? CorrelationId, string? ActivityId, string? DefinitionId)
    : WorkflowMatch(WorkflowInstanceId, WorkflowInstance, CorrelationId);

public record ResumableWorkflowMatch(string WorkflowInstanceId, WorkflowInstance? WorkflowInstance, string? CorrelationId, string? BookmarkId)
    : WorkflowMatch(WorkflowInstanceId, WorkflowInstance, CorrelationId);

/// <summary>
/// Contains arguments to use for counting the number of workflow instances.
/// </summary>
public class CountRunningWorkflowsArgs
{
    /// <summary>
    /// The workflow definition ID to include in the query.
    /// </summary>
    public string? DefinitionId { get; set; }

    /// <summary>
    /// The workflow definition version to include in the query.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// The correlation ID to include in the query. 
    /// </summary>
    public string? CorrelationId { get; set; }
}