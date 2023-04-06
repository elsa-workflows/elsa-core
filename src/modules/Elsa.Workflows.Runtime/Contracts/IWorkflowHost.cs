using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

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
    /// Returns a value indicating whether or not the specified workflow can start a new instance or not.
    /// </summary>
    Task<bool> CanStartWorkflowAsync(StartWorkflowHostOptions? options = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start a new workflow instance and execute it.
    /// </summary>
    Task<StartWorkflowHostResult> StartWorkflowAsync(StartWorkflowHostOptions? options = default, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume an existing workflow instance.
    /// </summary>
    Task<ResumeWorkflowHostResult> ResumeWorkflowAsync(ResumeWorkflowHostOptions? options = default, CancellationToken cancellationToken = default);
}

public record StartWorkflowHostOptions(string? InstanceId = default, string? CorrelationId = default, IDictionary<string, object>? Input = default, string? TriggerActivityId = default);

public record ResumeWorkflowHostOptions(
    string? CorrelationId = default,
    string? BookmarkId = default,
    string? ActivityId = default,
    string? ActivityNodeId = default,
    string? ActivityInstanceId = default,
    string? ActivityHash = default,
    IDictionary<string, object>? Input = default);

public record StartWorkflowHostResult(Diff<Bookmark> BookmarksDiff);

public record ResumeWorkflowHostResult(Diff<Bookmark> BookmarksDiff);