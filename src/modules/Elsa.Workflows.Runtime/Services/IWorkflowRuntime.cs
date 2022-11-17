using Elsa.Common.Models;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Services;

public interface IWorkflowRuntime
{
    Task<StartWorkflowResult> StartWorkflowAsync(string definitionId, StartWorkflowRuntimeOptions options, CancellationToken cancellationToken = default);
    Task<ResumeWorkflowResult> ResumeWorkflowAsync(string instanceId, string bookmarkId, ResumeWorkflowRuntimeOptions options, CancellationToken cancellationToken = default);
    Task<ICollection<ResumedWorkflow>> ResumeWorkflowsAsync(string activityTypeName, object bookmarkPayload, ResumeWorkflowRuntimeOptions options, CancellationToken cancellationToken = default);
    Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsRuntimeOptions options, CancellationToken cancellationToken = default);
    Task<WorkflowState?> ExportWorkflowStateAsync(string instanceId, CancellationToken cancellationToken = default);
    Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);
    Task UpdateBookmarksAsync(UpdateBookmarksContext context, CancellationToken cancellationToken = default);
}

public record StartWorkflowRuntimeOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default, VersionOptions VersionOptions = default, string? TriggerActivityId = default);
public record ResumeWorkflowRuntimeOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default);
public record StartWorkflowResult(string InstanceId, ICollection<Bookmark> Bookmarks);
public record ResumeWorkflowResult(ICollection<Bookmark> Bookmarks);
public record TriggerWorkflowsRuntimeOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default);
public record TriggerWorkflowsResult(ICollection<TriggeredWorkflow> TriggeredWorkflows);
public record ResumedWorkflow(string InstanceId, ICollection<Bookmark> Bookmarks);
public record TriggeredWorkflow(string InstanceId, ICollection<Bookmark> Bookmarks);
public record UpdateBookmarksContext(string InstanceId, Diff<Bookmark> Diff, string? CorrelationId);