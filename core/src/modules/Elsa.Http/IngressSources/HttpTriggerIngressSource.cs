using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.IngressSources;

namespace Elsa.Http.IngressSources;

/// <summary>
/// Surfaces HTTP-triggered workflow ingress as an <see cref="IIngressSource"/> in the runtime's diagnostic registry.
/// Pause/resume are observed automatically via <see cref="IQuiescenceSignal"/> — the <c>HttpWorkflowsMiddleware</c>
/// short-circuits to <c>503 Service Unavailable</c> while the runtime is paused or draining (FR-006). This adapter
/// therefore has no behavior of its own beyond reporting state, which is what <see cref="PassiveIngressSource"/>
/// is for.
/// </summary>
public sealed class HttpTriggerIngressSource(IQuiescenceSignal signal) : PassiveIngressSource(signal)
{
    /// <inheritdoc />
    public override string Name => "http.trigger";
}
