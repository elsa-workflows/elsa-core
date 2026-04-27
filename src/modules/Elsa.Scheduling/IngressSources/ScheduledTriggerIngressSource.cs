using Elsa.Workflows.Runtime;

namespace Elsa.Scheduling.IngressSources;

/// <summary>
/// Surfaces scheduled-trigger ingress (Cron, Timer, StartAt, Delay) as an <see cref="IIngressSource"/> in the runtime's
/// diagnostic registry. Pause/resume is observed transitively via <see cref="IQuiescenceSignal"/> — scheduled triggers
/// dispatch through the bookmark queue, whose processor consults the signal at the top of each invocation (FR-024).
/// This adapter therefore has no behaviour of its own beyond reporting state.
/// </summary>
public sealed class ScheduledTriggerIngressSource : IIngressSource
{
    private readonly Func<IQuiescenceSignal> _signalFactory;

    public ScheduledTriggerIngressSource(Func<IQuiescenceSignal> signalFactory) => _signalFactory = signalFactory;

    /// <inheritdoc />
    /// <remarks>
    /// Covers all scheduling-driven trigger types (Cron, Timer, StartAt, Delay) — not just Cron. Operators reading
    /// <c>GET /admin/workflow-runtime/status</c> or <c>DrainOutcome.Sources</c> see this name verbatim, so the
    /// generic <c>scheduling.triggers</c> avoids implying Cron-only coverage.
    /// </remarks>
    public string Name => "scheduling.triggers";

    /// <inheritdoc />
    public TimeSpan PauseTimeout => TimeSpan.FromMilliseconds(50);

    /// <inheritdoc />
    public IngressSourceState CurrentState =>
        _signalFactory().IsAcceptingNewWork ? IngressSourceState.Running : IngressSourceState.Paused;

    /// <inheritdoc />
    public ValueTask PauseAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask ResumeAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
