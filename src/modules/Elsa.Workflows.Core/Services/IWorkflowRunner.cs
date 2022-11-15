using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Runs a given workflow by scheduling its root activity.
/// </summary>
public interface IWorkflowRunner
{
    Task<RunWorkflowResult> RunAsync(IActivity activity, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(IWorkflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync<T>(RunWorkflowOptions? options = default, CancellationToken cancellationToken = default) where T : IWorkflow;
    Task<RunWorkflowResult> RunAsync(Workflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext);
}

public record RunWorkflowOptions(string? InstanceId = default, string? CorrelationId = default, string? BookmarkId = default, IDictionary<string, object>? Input = default);