using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;

namespace Elsa.Workflows;

/// <summary>
/// Runs a given workflow by scheduling its root activity.
/// </summary>
public interface IWorkflowRunner
{
    Task<RunWorkflowResult> RunAsync(IActivity activity, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(IWorkflow workflow, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult<TResult>> RunAsync<TResult>(WorkflowBase<TResult> workflow, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync<T>(RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) where T : IWorkflow, new();
    Task<TResult> RunAsync<T, TResult>(RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) where T : WorkflowBase<TResult>, new();
    Task<RunWorkflowResult> RunAsync(Workflow workflow, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(WorkflowGraph workflowGraph, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(WorkflowGraph workflowGraph, WorkflowState workflowState, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default);
    Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext);
    Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext, IWorkflowExecutionPipeline workflowExecutionPipeline, IActivityExecutionPipeline activityExecutionPipeline);
}