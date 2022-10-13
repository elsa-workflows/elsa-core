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
    Task<StartWorkflowHostResult> StartWorkflowAsync(IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<StartWorkflowHostResult> StartWorkflowAsync(string instanceId, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
    Task<ResumeWorkflowHostResult> ResumeWorkflowAsync(string bookmarkId, IDictionary<string, object>? input = default, CancellationToken cancellationToken = default);
}

public record StartWorkflowHostResult(Diff<Bookmark> BookmarksDiff);

public record ResumeWorkflowHostResult(Diff<Bookmark> BookmarksDiff);