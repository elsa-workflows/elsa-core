namespace Elsa.Workflows.Runtime.IngressSources;

/// <summary>
/// Surfaces the in-process bookmark-queue processor as an <see cref="IIngressSource"/> in the runtime's diagnostic
/// registry. Pause/resume are observed automatically via <see cref="IQuiescenceSignal"/> — the
/// <c>BookmarkQueueProcessor</c> consults the signal at the top of each invocation and skips dispatch when the
/// runtime is paused or draining (FR-024). This adapter therefore has no behaviour of its own beyond reporting state.
/// </summary>
public sealed class InternalBookmarkQueueIngressSource(IQuiescenceSignal signal) : IIngressSource
{
    /// <inheritdoc />
    public string Name => "internal.bookmark-queue-worker";

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
