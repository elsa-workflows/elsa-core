namespace Elsa.Workflows.Runtime;

/// <summary>
/// Domain service that backs the runtime's administrative pause / resume / force-drain / status surface. Endpoints
/// (HTTP, MCP, CLI) compose this single service rather than re-implementing the orchestration + audit logic per
/// transport.
/// </summary>
public interface IWorkflowRuntimeAdminService
{
    /// <summary>
    /// Places the runtime into <see cref="QuiescenceReason.AdministrativePause"/>. Idempotent — a second call with
    /// the runtime already paused returns the current state without publishing additional audit notifications
    /// (SC-007).
    /// </summary>
    ValueTask<QuiescenceState> PauseAsync(string? reason, string? requestedBy, CancellationToken cancellationToken);

    /// <summary>
    /// Clears <see cref="QuiescenceReason.AdministrativePause"/>. Returns the post-request state regardless of the
    /// outcome — callers detect "resume rejected because drain is active" by inspecting
    /// <see cref="QuiescenceState.Reason"/> against the original state.
    /// </summary>
    ValueTask<QuiescenceState> ResumeAsync(string? requestedBy, CancellationToken cancellationToken);

    /// <summary>
    /// Operator-escalation drain with zero deadline. Cancels every active execution cycle, persists their instances as
    /// <see cref="WorkflowSubStatus.Interrupted"/>, writes a <c>WorkflowInterrupted</c> log entry per affected
    /// instance, and returns the structured outcome. Throws <see cref="InvalidOperationException"/> when a
    /// non-force drain is already in progress in the same generation.
    /// </summary>
    ValueTask<DrainOutcome> ForceDrainAsync(string? reason, string? requestedBy, CancellationToken cancellationToken);

    /// <summary>Composite status for the admin status endpoint and any other diagnostic consumer.</summary>
    RuntimeAdminStatus GetStatus();
}

/// <summary>Snapshot returned by <see cref="IWorkflowRuntimeAdminService.GetStatus"/>.</summary>
public sealed record RuntimeAdminStatus(
    QuiescenceState State,
    IReadOnlyCollection<IngressSourceSnapshot> Sources,
    int ActiveExecutionCycleCount);
