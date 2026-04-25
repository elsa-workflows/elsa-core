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
    int BurstsForceCancelledCount,
    IReadOnlyList<string> ForceCancelledInstanceIds);
