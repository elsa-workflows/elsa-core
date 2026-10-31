namespace Elsa.Workflows.Runtime;

/// <summary>
/// Structured result of a completed drain, returned by <c>IDrainOrchestrator.DrainAsync</c>.
/// </summary>
public sealed record DrainOutcome(
    DrainResult OverallResult,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    TimeSpan PausePhaseDuration,
    TimeSpan WaitPhaseDuration,
    IReadOnlyList<IngressSourceFinalState> Sources,
    int ExecutionCyclesForceCancelledCount,
    IReadOnlyList<string> ForceCancelledInstanceIds)
{
    /// <summary>
    /// <c>true</c> when this outcome was returned from the orchestrator's cache (i.e., a previous drain already
    /// ran in this generation and the caller's request was answered with the cached result rather than triggering
    /// a fresh drain). Audit consumers MUST skip publishing notifications when this flag is set, otherwise
    /// repeated calls would emit spurious audit events and break the SC-007 idempotency guarantee.
    /// </summary>
    public bool WasCached { get; init; }
}
