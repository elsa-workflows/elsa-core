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

public class RunWorkflowOptions
{
    public RunWorkflowOptions(
        string? workflowInstanceId = default, 
        string? correlationId = default, 
        string? bookmarkId = default, 
        string? activityId = default,
        string? activityNodeId = default,
        string? activityInstanceId = default,
        string? activityHash = default, 
        IDictionary<string, object>? input = default, 
        string? triggerActivityId = default)
    {
        WorkflowInstanceId = workflowInstanceId;
        CorrelationId = correlationId;
        BookmarkId = bookmarkId;
        ActivityId = activityId;
        ActivityNodeId = activityNodeId;
        ActivityInstanceId = activityInstanceId;
        ActivityHash = activityHash;
        Input = input;
        TriggerActivityId = triggerActivityId;
    }

    public string? WorkflowInstanceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? BookmarkId { get; set; }
    public string? ActivityId { get; set; }
    public string? ActivityNodeId { get; set; }
    public string? ActivityInstanceId { get; set; }
    public string? ActivityHash { get; set; }
    public IDictionary<string, object>? Input { get; set; }
    public string? TriggerActivityId { get; set; }
}