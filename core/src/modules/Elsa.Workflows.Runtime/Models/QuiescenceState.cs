namespace Elsa.Workflows.Runtime;

/// <summary>
/// Composite signal describing whether the runtime is accepting new work and, if not, why not.
/// Immutable snapshot — the active state is always read through <c>IQuiescenceSignal.CurrentState</c>.
/// </summary>
public sealed record QuiescenceState(
    QuiescenceReason Reason,
    DateTimeOffset? PausedAt,
    DateTimeOffset? DrainStartedAt,
    string? PauseReasonText,
    string? PauseRequestedBy,
    string GenerationId)
{
    /// <summary>
    /// Convenience derived flag: <c>true</c> iff <see cref="Reason"/> is <see cref="QuiescenceReason.None"/>.
    /// </summary>
    public bool IsAcceptingNewWork => Reason == QuiescenceReason.None;

    /// <summary>
    /// Creates the initial state for a fresh runtime generation.
    /// </summary>
    public static QuiescenceState Initial(string generationId) =>
        new(QuiescenceReason.None, null, null, null, null, generationId);
}
