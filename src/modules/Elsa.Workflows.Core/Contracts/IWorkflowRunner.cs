using Elsa.Workflows.Core.Abstractions;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Runs a given workflow by scheduling its root activity.
/// </summary>
public interface IWorkflowRunner
{
    Task<RunWorkflowResult> RunAsync(IActivity activity, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(IWorkflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult<TResult>> RunAsync<TResult>(WorkflowBase<TResult> workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync<T>(RunWorkflowOptions? options = default, CancellationToken cancellationToken = default) where T : IWorkflow;
    Task<TResult> RunAsync<T, TResult>(RunWorkflowOptions? options = default, CancellationToken cancellationToken = default) where T : WorkflowBase<TResult>;
    Task<RunWorkflowResult> RunAsync(Workflow workflow, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, RunWorkflowOptions? options = default, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext);
}

public record RunWorkflowOptions(
    string? InstanceId = default, 
    string? CorrelationId = default, 
    string? BookmarkId = default, 
    string? ActivityId = default,
    string? ActivityNodeId = default,
    string? ActivityInstanceId = default,
    string? ActivityHash = default, 
    IDictionary<string, object>? Input = default, 
    string? TriggerActivityId = default);