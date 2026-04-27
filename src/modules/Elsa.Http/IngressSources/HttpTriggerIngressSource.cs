using Elsa.Workflows.Runtime;

namespace Elsa.Http.IngressSources;

/// <summary>
/// Surfaces HTTP-triggered workflow ingress as an <see cref="IIngressSource"/> in the runtime's diagnostic registry.
/// Pause/resume are observed automatically via <see cref="IQuiescenceSignal"/> — the <c>HttpWorkflowsMiddleware</c>
/// short-circuits to 503 Service Unavailable while the runtime is paused or draining (FR-006). This adapter therefore
/// has no behavior of its own beyond reporting state to the registry.
/// </summary>
public sealed class HttpTriggerIngressSource(IQuiescenceSignal signal) : IIngressSource
{
    /// <inheritdoc />
    public string Name => "http.trigger";

    /// <inheritdoc />
    public TimeSpan PauseTimeout => TimeSpan.FromMilliseconds(50);

    /// <inheritdoc />
    public IngressSourceState CurrentState =>
        signal.IsAcceptingNewWork ? IngressSourceState.Running : IngressSourceState.Paused;

    /// <inheritdoc />
    public ValueTask PauseAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask ResumeAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
