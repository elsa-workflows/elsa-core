using Elsa.Common.Models;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Services;

public interface IWorkflowRuntime
{
    Task<StartWorkflowResult> StartWorkflowAsync(string definitionId, StartWorkflowOptions options, CancellationToken cancellationToken = default);
    Task<ResumeWorkflowResult> ResumeWorkflowAsync(string instanceId, string bookmarkId, ResumeWorkflowOptions options, CancellationToken cancellationToken = default);
    Task<TriggerWorkflowsResult> TriggerWorkflowsAsync(string activityTypeName, object bookmarkPayload, TriggerWorkflowsOptions options, CancellationToken cancellationToken = default);
    Task<WorkflowState?> ExportWorkflowStateAsync(string instanceId, CancellationToken cancellationToken = default);
    Task ImportWorkflowStateAsync(WorkflowState workflowState, CancellationToken cancellationToken = default);
}

public record StartWorkflowOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default, VersionOptions VersionOptions = default);
public record ResumeWorkflowOptions(IDictionary<string, object>? Input = default);
public record StartWorkflowResult(string InstanceId, ICollection<Bookmark> Bookmarks);
public record ResumeWorkflowResult(ICollection<Bookmark> Bookmarks);
public record TriggerWorkflowsOptions(string? CorrelationId = default, IDictionary<string, object>? Input = default);
public record TriggerWorkflowsResult(ICollection<TriggeredWorkflow> TriggeredWorkflows);
public record TriggeredWorkflow(string InstanceId, ICollection<Bookmark> Bookmarks);