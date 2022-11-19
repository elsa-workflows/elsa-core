using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Represents a single workflow instance that can be executed and takes care of publishing various lifecycle events.
/// </summary>
public interface IWorkflowHost
{
    Workflow Workflow { get; set; }
    WorkflowState WorkflowState { get; set; }
    Task<StartWorkflowHostResult> StartWorkflowAsync(StartWorkflowHostOptions? options = default, CancellationToken cancellationToken = default);
    Task<ResumeWorkflowHostResult> ResumeWorkflowAsync(ResumeWorkflowHostOptions? options = default, CancellationToken cancellationToken = default);
}

public record StartWorkflowHostOptions(string? InstanceId = default, string? CorrelationId = default, IDictionary<string, object>? Input = default, string? TriggerActivityId = default);

public record ResumeWorkflowHostOptions(string? CorrelationId = default, string? BookmarkId = default, string? ActivityId = default, IDictionary<string, object>? Input = default);

public record StartWorkflowHostResult(Diff<Bookmark> BookmarksDiff);

public record ResumeWorkflowHostResult;