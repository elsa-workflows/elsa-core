using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Registers one execution-cycle handle with the explicitly owning <see cref="WorkflowExecutionScope"/>.
/// The owner releases it after all awaited execution and persistence unwind, including on failure.
/// </summary>
/// <remarks>
/// Direct pipeline callers without an explicit scope are tracked only until the pipeline returns.
/// They must create a scope around the pipeline and any subsequent awaited writes to extend that boundary.
/// </remarks>
public class ExecutionCycleTrackingMiddleware(WorkflowMiddlewareDelegate next, IExecutionCycleRegistry cycleRegistry) : WorkflowExecutionMiddleware(next)
{
    /// <summary>
    /// Transient property conveying the originating <see cref="IIngressSource.Name"/> from the dispatcher.
    /// </summary>
    public const string IngressSourceNameKey = "Elsa.Workflows.Runtime.IngressSourceName";

    /// <summary>
    /// Transient property holding the attempt's handle. Checkpoints never dispose it. The owning scope
    /// retires this entry and releases the handle after the last awaited write, so force drain can await
    /// <see cref="ExecutionCycleHandle.Disposed"/> before its forensic write.
    /// </summary>
    public const string ExecutionCycleHandleKey = "Elsa.Workflows.Runtime.ExecutionCycleHandle";

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        using var executionScope = WorkflowExecutionScope.Begin(context);
        executionScope.GetOrAddResource(ExecutionCycleHandleKey, () =>
        {
            var ingressSourceName = context.TransientProperties.TryGetValue(IngressSourceNameKey, out var raw) ? raw as string : null;
            // Keep the existing cancellation bridge. Cancellation requests do not release ownership;
            // drain still waits for the owner to unwind before writing Interrupted.
            var handle = cycleRegistry.BeginCycle(context.Id, ingressSourceName, context.CancellationToken, cancelCallback: context.Cancel);

            if (context.GetService<IOptions<GracefulShutdownOptions>>()?.Value.InterruptOnHostStop == true)
                CaptureRestartPoint(handle, context);

            return handle;
        });
        await Next(context);
    }

    /// <summary>
    /// Records what a host-stop drain needs to persist a cycle that never reached a commit, so the next start continues
    /// it. Captured when the cycle begins, before the workflow changes the context on another thread.
    /// </summary>
    private static void CaptureRestartPoint(ExecutionCycleHandle handle, WorkflowExecutionContext context)
    {
        if (context.ResumedBookmarkContext is { } resumed)
        {
            handle.ResumedBookmark = resumed.Bookmark;
            handle.ResumeInput = new Dictionary<string, object>(context.Input);
            return;
        }

        // Only a fresh run has no activity execution context yet. A trigger-started run is left to its trigger.
        if (context.ActivityExecutionContexts.Count > 0 || context.TriggerActivityId != null)
            return;

        var workflow = context.Workflow;
        var options = new WorkflowInstanceOptions
        {
            WorkflowInstanceId = context.Id,
            CorrelationId = context.CorrelationId,
            Name = context.Name,
            ParentWorkflowInstanceId = context.ParentWorkflowInstanceId,
            Input = new Dictionary<string, object>(context.Input),
            Properties = new Dictionary<string, object>(context.Properties)
        };
        handle.CreatePendingInstance = manager => manager.CreateWorkflowInstance(workflow, options);
    }
}
