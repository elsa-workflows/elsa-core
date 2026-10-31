namespace Elsa.Workflows.Runtime.IngressSources;

/// <summary>
/// Surfaces the in-process bookmark-queue processor as an <see cref="IIngressSource"/> in the runtime's diagnostic
/// registry. Pause/resume are observed automatically via <see cref="IQuiescenceSignal"/> — the
/// <c>BookmarkQueueProcessor</c> consults the signal at the top of each invocation and skips dispatch when the
/// runtime is paused or draining (FR-024). This adapter therefore has no behavior of its own beyond reporting
/// state, which is what <see cref="PassiveIngressSource"/> is for.
/// </summary>
public sealed class InternalBookmarkQueueIngressSource(IQuiescenceSignal signal) : PassiveIngressSource(signal)
{
    /// <inheritdoc />
    public override string Name => "internal.bookmark-queue-worker";
}
