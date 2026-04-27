using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Registers a <see cref="BurstHandle"/> in <see cref="IBurstRegistry"/> for the duration of a single workflow burst —
/// the slice of execution between the pipeline entry and the next persistence boundary. The drain orchestrator counts
/// these handles and, on deadline breach, force-cancels each one and marks the underlying instance
/// <see cref="WorkflowSubStatus.Interrupted"/>.
/// </summary>
/// <remarks>
/// <para>
/// Ingress source attribution: when a dispatcher knows which <see cref="IIngressSource"/> initiated the call, it sets
/// <see cref="WorkflowExecutionContext.TransientProperties"/>[<see cref="IngressSourceNameKey"/>] before invoking the pipeline.
/// The middleware reads it and forwards the name to <see cref="IBurstRegistry.BeginBurst"/>, which uses it to detect the
/// FR-018 invariant violation (a source that reports <see cref="IngressSourceState.Paused"/> but initiates a burst anyway).
/// </para>
/// </remarks>
public class BurstTrackingMiddleware(WorkflowMiddlewareDelegate next, IBurstRegistry burstRegistry) : WorkflowExecutionMiddleware(next)
{
    /// <summary>
    /// <see cref="WorkflowExecutionContext.TransientProperties"/> key used to convey the originating
    /// <see cref="IIngressSource.Name"/> from the dispatcher into the pipeline.
    /// </summary>
    public const string IngressSourceNameKey = "Elsa.Workflows.Runtime.IngressSourceName";

    /// <summary>
    /// <see cref="WorkflowExecutionContext.TransientProperties"/> key under which the active <see cref="BurstHandle"/>
    /// is stored for the duration of the workflow execution. <see cref="Services.BurstAwareCommitStateHandler"/>
    /// retrieves and disposes the handle AFTER the runner's terminal commit has persisted the workflow state, so
    /// the drain orchestrator's force-cancel path (which awaits <c>handle.Disposed</c>) sees the runner's commit
    /// land BEFORE its own <see cref="WorkflowSubStatus.Interrupted"/> write — eliminating the runner-clobber race.
    /// </summary>
    public const string BurstHandleKey = "Elsa.Workflows.Runtime.BurstHandle";

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var ingressSourceName = context.TransientProperties.TryGetValue(IngressSourceNameKey, out var raw) ? raw as string : null;

        // The cancelCallback bridges the BurstHandle's cancellation to the workflow execution itself: when the drain
        // orchestrator calls handle.Cancel() on deadline breach, WorkflowExecutionContext.Cancel() runs synchronously,
        // which fires the registered cancellation callback inside the context (transitioning the workflow to Cancelled
        // and clearing its schedule). The runner stops scheduling new activities; the orchestrator awaits
        // handle.Disposed (set by the BurstAwareCommitStateHandler decorator AFTER commit) and then overwrites the
        // sub-status with Interrupted.
        var handle = burstRegistry.BeginBurst(
            context.Id,
            ingressSourceName,
            context.CancellationToken,
            cancelCallback: context.Cancel);
        context.TransientProperties[BurstHandleKey] = handle;

        try
        {
            await Next(context);
        }
        catch
        {
            // Exception path: the runner won't reach commitStateHandler.CommitAsync, so the decorator never disposes.
            // We dispose here to free the registry slot. Disposal is idempotent (BurstHandle._disposed flag).
            handle.Dispose();
            throw;
        }
        // Success path: BurstAwareCommitStateHandler disposes the handle after the runner's commit completes.
    }
}
