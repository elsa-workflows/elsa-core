using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;

namespace Elsa.Workflows.ComponentTests.Services;

/// <summary>
/// Tracks actual in-flight workflow-runner calls across the session-shared test cluster.
/// Persisted workflow status cannot provide this signal: rejected inputs can leave an
/// Executing snapshot even after the request and runner call have unwound.
/// </summary>
public sealed class WorkflowExecutionTracker
{
    private readonly object _gate = new();
    private int _activeCount;
    private TaskCompletionSource<bool> _idle = CreateCompletedSignal();

    public int ActiveCount
    {
        get
        {
            lock (_gate)
                return _activeCount;
        }
    }

    public IDisposable Enter()
    {
        lock (_gate)
        {
            if (_activeCount++ == 0)
                _idle = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        return new Lease(this);
    }

    public async Task WaitForIdleAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Task idleTask;
            lock (_gate)
            {
                if (_activeCount == 0)
                    return;

                idleTask = _idle.Task;
            }

            await idleTask.WaitAsync(cancellationToken);
        }
    }

    private void Exit()
    {
        TaskCompletionSource<bool>? idle = null;
        lock (_gate)
        {
            if (_activeCount <= 0)
                throw new InvalidOperationException("Workflow execution tracking became unbalanced.");

            if (--_activeCount == 0)
                idle = _idle;
        }

        idle?.TrySetResult(true);
    }

    private static TaskCompletionSource<bool> CreateCompletedSignal()
    {
        var signal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        signal.SetResult(true);
        return signal;
    }

    private sealed class Lease(WorkflowExecutionTracker owner) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                owner.Exit();
        }
    }
}

/// <summary>
/// Ensures every workflow runner exit, including an exceptional exit, balances the host tracker.
/// </summary>
public sealed class TrackingWorkflowRunner(IWorkflowRunner inner, WorkflowExecutionTracker tracker) : IWorkflowRunner
{
    public Task<RunWorkflowResult> RunAsync(IActivity activity, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
        TrackAsync(() => inner.RunAsync(activity, options, cancellationToken));

    public Task<RunWorkflowResult> RunAsync(IWorkflow workflow, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
        TrackAsync(() => inner.RunAsync(workflow, options, cancellationToken));

    public Task<RunWorkflowResult<TResult>> RunAsync<TResult>(WorkflowBase<TResult> workflow, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
        TrackAsync(() => inner.RunAsync(workflow, options, cancellationToken));

    public Task<RunWorkflowResult> RunAsync<T>(RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) where T : IWorkflow, new() =>
        TrackAsync(() => inner.RunAsync<T>(options, cancellationToken));

    public Task<TResult> RunAsync<T, TResult>(RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) where T : WorkflowBase<TResult>, new() =>
        TrackAsync(() => inner.RunAsync<T, TResult>(options, cancellationToken));

    public Task<RunWorkflowResult> RunAsync(Workflow workflow, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
        TrackAsync(() => inner.RunAsync(workflow, options, cancellationToken));

    public Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
        TrackAsync(() => inner.RunAsync(workflow, workflowState, options, cancellationToken));

    public Task<RunWorkflowResult> RunAsync(WorkflowGraph workflowGraph, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
        TrackAsync(() => inner.RunAsync(workflowGraph, options, cancellationToken));

    public Task<RunWorkflowResult> RunAsync(WorkflowGraph workflowGraph, WorkflowState workflowState, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) =>
        TrackAsync(() => inner.RunAsync(workflowGraph, workflowState, options, cancellationToken));

    public Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext) =>
        TrackAsync(() => inner.RunAsync(workflowExecutionContext));

    private async Task<TResult> TrackAsync<TResult>(Func<Task<TResult>> operation)
    {
        using var lease = tracker.Enter();
        return await operation();
    }
}
