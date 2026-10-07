using Elsa.Workflows.Pipelines.WorkflowExecution;

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
            return cycleRegistry.BeginCycle(context.Id, ingressSourceName, context.CancellationToken, cancelCallback: context.Cancel);
        });
        await Next(context);
    }
}
