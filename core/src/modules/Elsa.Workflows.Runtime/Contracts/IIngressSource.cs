namespace Elsa.Workflows.Runtime;

/// <summary>
/// An adapter through which external events enter the workflow engine. Every component that initiates execution cycles
/// from an external event — message consumers, schedulers, HTTP trigger handlers, internal durable-queue processors,
/// internal recurring tasks that enqueue work, and third-party modules — implements this contract so the runtime
/// can pause them uniformly during drain or administrative pause. See FR-006..FR-013.
/// </summary>
public interface IIngressSource
{
    /// <summary>Stable, dot-separated identifier (e.g., <c>http.trigger</c>, <c>scheduling.cron</c>).</summary>
    string Name { get; }

    /// <summary>
    /// Per-source pause timeout. Return <see cref="TimeSpan.Zero"/> (or negative) to defer to the
    /// configured <c>GracefulShutdownOptions.IngressPauseTimeout</c> default; return a positive
    /// value to override the default for this specific source. The orchestrator additionally caps
    /// the resolved value at the overall drain deadline.
    /// </summary>
    TimeSpan PauseTimeout { get; }

    /// <summary>Observable state, backed by the source's internal state machine.</summary>
    IngressSourceState CurrentState { get; }

    /// <summary>
    /// Stops the source from delivering new work. Idempotent and safe under concurrent invocation.
    /// </summary>
    ValueTask PauseAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Resumes delivery. Idempotent and safe under concurrent invocation.
    /// </summary>
    ValueTask ResumeAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Optional escalation capability an <see cref="IIngressSource"/> may declare to be force-stopped when
/// <see cref="IIngressSource.PauseAsync"/> cannot complete within the configured timeout. See FR-008.
/// </summary>
public interface IForceStoppable
{
    /// <summary>
    /// Abandons any in-flight delivery and stops the source with extreme prejudice. Failures are logged
    /// by the caller but do not block the overall drain.
    /// </summary>
    ValueTask ForceStopAsync(CancellationToken cancellationToken);
}
