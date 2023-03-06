using Elsa.Common.Models;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Workflows.Runtime.Services;

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
    /// Resumes an existing workflow instance.
    /// </summary>
    /// <param name="workflowInstanceId">The ID of the workflow instance to resume.</param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    Task<ResumeWorkflowResult> ResumeWorkflowAsync(string workflowInstanceId, ResumeWorkflowRuntimeOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes all workflows that are bookmarked on the specified activity type. 
    /// </summary>
    Task<ICollection<WorkflowExecutionResult>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsRuntimeOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts all workflows and resumes existing workflow instances based on the specified activity type and bookmark payload.
    /// </summary>
    Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsRuntimeOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all the workflows that can be started or resumed based on a query model.
    /// </summary>
    /// <param name="workflowsQuery"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<CollectedWorkflow>> FindWorkflowsAsync(WorkflowsQuery workflowsQuery, CancellationToken cancellationToken = default);

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
    /// Counts the number of workflow instances based on the provided query args.
    /// </summary>
    Task<int> CountRunningWorkflowsAsync(CountRunningWorkflowsArgs args, CancellationToken cancellationToken = default);
}

public record StartWorkflowRuntimeOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default, VersionOptions VersionOptions = default, string? TriggerActivityId = default, string? InstanceId = default);
public record ResumeWorkflowRuntimeOptions(string? CorrelationId = default, string? BookmarkId = default, string? ActivityId = default, IDictionary<string, object>? Input = default);
public record CanStartWorkflowResult(string? InstanceId, bool CanStart);
public record ResumeWorkflowResult(ICollection<Bookmark> Bookmarks);
public record TriggerWorkflowsRuntimeOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default);
public record TriggerWorkflowsResult(ICollection<WorkflowExecutionResult> TriggeredWorkflows);
public record WorkflowExecutionResult(string InstanceId, ICollection<Bookmark> Bookmarks, string? ActivityId = null);
public record UpdateBookmarksContext(string InstanceId, Diff<Bookmark> Diff, string? CorrelationId);
public record WorkflowsQuery(string ActivityTypeName, object BookmarkPayload, TriggerWorkflowsRuntimeOptions Options);
public record CollectedWorkflow(string WorkflowInstanceId, WorkflowInstance? WorkflowInstance, string? ActivityId);

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