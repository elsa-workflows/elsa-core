namespace Elsa.Workflows.Runtime.IngressSources;

/// <summary>
/// Surfaces the in-process bookmark-queue processor as an <see cref="IIngressSource"/> so it appears in the admin
/// status response alongside external sources. Pause/resume are observed automatically via <see cref="IQuiescenceSignal"/>
/// — the <c>BookmarkQueueProcessor</c> consults the signal at the top of each invocation and skips dispatch when the
/// runtime is paused or draining (FR-024). This adapter therefore has no behaviour of its own beyond reporting state.
/// </summary>
public sealed class InternalBookmarkQueueIngressSource : IIngressSource
{
    // Lazy resolution: this source is collected by IIngressSourceRegistry's IEnumerable<IIngressSource> ctor parameter.
    // QuiescenceSignal also needs the registry transitively (via IBurstRegistry), creating a DI cycle if we eagerly
    // inject IQuiescenceSignal here. The Func<T> indirection breaks that cycle.
    private readonly Func<IQuiescenceSignal> _signalFactory;

    public InternalBookmarkQueueIngressSource(Func<IQuiescenceSignal> signalFactory) => _signalFactory = signalFactory;

    /// <inheritdoc />
    public string Name => "internal.bookmark-queue-worker";

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
