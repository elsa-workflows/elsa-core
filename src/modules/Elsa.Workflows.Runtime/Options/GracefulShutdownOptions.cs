using System.ComponentModel.DataAnnotations;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Configures the graceful-shutdown machinery of the workflow runtime: drain deadline, per-ingress-source pause timeout,
/// stimulus-queue back-pressure, and pause-persistence policy.
/// </summary>
public class GracefulShutdownOptions
{
    /// <summary>
    /// Maximum wall time a drain is allowed to take before outstanding bursts are force-cancelled.
    /// The effective deadline is clamped to the host's own shutdown timeout minus a small safety epsilon.
    /// </summary>
    public TimeSpan DrainDeadline { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Default per-ingress-source pause timeout. A source that does not complete its pause within this window is marked
    /// <c>PauseFailed</c> and — if it implements force-stop — escalated.
    /// </summary>
    public TimeSpan IngressPauseTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Maximum stimulus-queue depth while the runtime is paused. Beyond this threshold, readiness degrades and
    /// <see cref="OverflowPolicy"/> determines whether new writes are rejected.
    /// </summary>
    public int StimulusQueueMaxDepthWhilePaused { get; set; } = 10_000;

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

    /// <summary>
    /// Validates that every configured duration is strictly positive and that integer caps are greater than zero.
    /// </summary>
    internal void Validate()
    {
        if (DrainDeadline <= TimeSpan.Zero) throw new ValidationException($"{nameof(DrainDeadline)} must be greater than zero.");
        if (IngressPauseTimeout <= TimeSpan.Zero) throw new ValidationException($"{nameof(IngressPauseTimeout)} must be greater than zero.");
        if (StimulusQueueMaxDepthWhilePaused <= 0) throw new ValidationException($"{nameof(StimulusQueueMaxDepthWhilePaused)} must be greater than zero.");
        if (MaxForceCancelledInstanceIdsReported <= 0) throw new ValidationException($"{nameof(MaxForceCancelledInstanceIdsReported)} must be greater than zero.");
    }
}
