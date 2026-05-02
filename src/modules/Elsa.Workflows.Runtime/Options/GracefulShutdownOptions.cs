namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Configures the graceful-shutdown machinery of the workflow runtime: drain deadline, per-ingress-source pause timeout,
/// stimulus-queue back-pressure, and pause-persistence policy.
/// </summary>
public class GracefulShutdownOptions
{
    /// <summary>
    /// Maximum wall time a drain is allowed to take before outstanding execution cycles are force-cancelled.
    /// The effective deadline is clamped to the host's own shutdown timeout minus a small safety epsilon.
    /// </summary>
    public TimeSpan DrainDeadline { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Default per-ingress-source pause timeout. Used by the drain orchestrator whenever a source's
    /// own <see cref="IIngressSource.PauseTimeout"/> is <see cref="TimeSpan.Zero"/> (or negative).
    /// A source that does not complete its pause within the resolved window is marked
    /// <c>PauseFailed</c> and — if it implements force-stop — escalated.
    /// </summary>
    /// <remarks>
    /// Sources may opt out of this default by exposing their own positive <see cref="IIngressSource.PauseTimeout"/>;
    /// the per-source value wins. The orchestrator additionally caps every per-source deadline at the overall
    /// drain deadline so a single misbehaving source cannot exceed the host's shutdown budget.
    /// </remarks>
    public TimeSpan IngressPauseTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum stimulus-queue depth while the runtime is paused. Beyond this threshold, readiness degrades and
    /// <see cref="OverflowPolicy"/> determines whether new writes are rejected. <c>null</c> retains the unlimited
    /// queue behavior from before graceful shutdown shipped — useful when upstream transports already implement
    /// their own back-pressure and the operator does not want a runtime-side cap.
    /// </summary>
    public int? StimulusQueueMaxDepthWhilePaused { get; set; } = 10_000;

    /// <summary>
    /// Policy applied when the paused stimulus queue exceeds <see cref="StimulusQueueMaxDepthWhilePaused"/>.
    /// </summary>
    public StimulusQueueOverflowPolicy OverflowPolicy { get; set; } = StimulusQueueOverflowPolicy.Buffer;

    /// <summary>
    /// Whether an administrative pause survives a runtime generation boundary.
    /// </summary>
    public PausePersistencePolicy PausePersistence { get; set; } = PausePersistencePolicy.SessionScoped;

    /// <summary>
    /// Cap on how many force-cancelled workflow instance IDs are reported in a <c>DrainOutcome</c>. The true count is always
    /// reported regardless of this cap.
    /// </summary>
    public int MaxForceCancelledInstanceIdsReported { get; set; } = 100;

}
