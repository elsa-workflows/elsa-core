using Elsa.Workflows.Runtime.Middleware.Workflows;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Helpers for attaching ingress-source attribution to a workflow execution. The attribution is consumed by
/// <see cref="BurstTrackingMiddleware"/> when the burst is registered with <see cref="IBurstRegistry"/>.
/// </summary>
public static class IngressAttributionExtensions
{
    /// <summary>
    /// Stamps the supplied ingress source name into the context's transient properties. Call this before invoking
    /// the workflow execution pipeline (typically from an <see cref="IIngressSource"/> adapter or a dispatcher
    /// command handler that knows the originating source).
    /// </summary>
    public static void SetIngressSourceName(this WorkflowExecutionContext context, string? ingressSourceName)
    {
        if (string.IsNullOrEmpty(ingressSourceName)) return;
        context.TransientProperties[BurstTrackingMiddleware.IngressSourceNameKey] = ingressSourceName!;
    }

    /// <summary>
    /// Reads the ingress source name previously stamped by <see cref="SetIngressSourceName"/>, or null when no
    /// attribution is available.
    /// </summary>
    public static string? GetIngressSourceName(this WorkflowExecutionContext context) =>
        context.TransientProperties.TryGetValue(BurstTrackingMiddleware.IngressSourceNameKey, out var raw) ? raw as string : null;
}
