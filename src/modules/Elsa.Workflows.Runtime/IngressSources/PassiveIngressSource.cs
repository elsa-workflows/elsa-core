namespace Elsa.Workflows.Runtime.IngressSources;

/// <summary>
/// Base class for <see cref="IIngressSource"/> implementations whose actual pause/resume
/// enforcement lives in another layer — request middleware, queue processor, scheduling
/// dispatcher — and which only need to surface their state in the runtime's diagnostic
/// registry. Subclasses provide a stable <see cref="Name"/> and may override
/// <see cref="PauseTimeout"/>; everything else is derived from the shared
/// <see cref="IQuiescenceSignal"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use this base when your component already cooperates with <see cref="IQuiescenceSignal"/>
/// at its hot path (e.g. an ASP.NET Core middleware that returns <c>503 Service Unavailable</c>
/// while the runtime is not accepting new work, or a queue processor that consults the signal
/// at the top of each invocation). The adapter is then a passive observer: <c>PauseAsync</c>
/// / <c>ResumeAsync</c> are no-ops because there is nothing locally to start or stop, and
/// <see cref="CurrentState"/> reflects the signal directly so the orchestrator's
/// <c>DrainOutcome.Sources</c> and the admin status endpoint can show the source's state
/// without each adapter re-implementing the same five-line snippet.
/// </para>
/// <para>
/// Implement <see cref="IIngressSource"/> directly — not via this base — when the source owns
/// concrete pause/resume behaviour. For example, a message-queue consumer that needs to call
/// <c>Pause()</c> on its underlying client, or an HTTP poller that must stop a background
/// loop, has work to do at pause time and should encode that work in its own
/// <c>PauseAsync</c> / <c>ResumeAsync</c> implementations.
/// </para>
/// </remarks>
public abstract class PassiveIngressSource(IQuiescenceSignal signal) : IIngressSource
{
    private readonly IQuiescenceSignal _signal = signal;

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Default is 50 ms. Passive sources do no work at pause time, so the timeout only bounds
    /// the orchestrator's parallel pause-await; sub-second is correct. Override if a subclass
    /// has a reason to differ.
    /// </remarks>
    public virtual TimeSpan PauseTimeout => TimeSpan.FromMilliseconds(50);

    /// <inheritdoc />
    public IngressSourceState CurrentState =>
        _signal.IsAcceptingNewWork ? IngressSourceState.Running : IngressSourceState.Paused;

    /// <inheritdoc />
    public ValueTask PauseAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <inheritdoc />
    public ValueTask ResumeAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
