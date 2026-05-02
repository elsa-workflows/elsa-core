using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.IngressSources;

namespace Elsa.Scheduling.IngressSources;

/// <summary>
/// Surfaces scheduled-trigger ingress (Cron, Timer, StartAt, Delay) as an <see cref="IIngressSource"/> in the runtime's
/// diagnostic registry. Pause/resume is observed transitively via <see cref="IQuiescenceSignal"/> — scheduled triggers
/// dispatch through the bookmark queue, whose processor consults the signal at the top of each invocation (FR-024).
/// This adapter therefore has no behavior of its own beyond reporting state, which is what
/// <see cref="PassiveIngressSource"/> is for.
/// </summary>
public sealed class ScheduledTriggerIngressSource(IQuiescenceSignal signal) : PassiveIngressSource(signal)
{
    /// <inheritdoc />
    /// <remarks>
    /// Covers all scheduling-driven trigger types (Cron, Timer, StartAt, Delay) — not just Cron. Operators reading
    /// <c>GET /admin/workflow-runtime/status</c> or <c>DrainOutcome.Sources</c> see this name verbatim; the generic
    /// <c>scheduling.trigger</c> avoids implying Cron-only coverage. Singular to match the rest of the suite
    /// (<c>http.trigger</c>, <c>internal.bookmark-queue-worker</c>) — the name identifies the source, not the
    /// number of trigger types it dispatches.
    /// </remarks>
    public override string Name => "scheduling.trigger";
}
