namespace Elsa.Workflows.Runtime;

/// <summary>
/// Single source of truth for whether the workflow runtime is currently accepting new work.
/// Scoped to the container in which the workflow runtime is registered: global in single-runtime hosts,
/// per-shell in multi-shell hosts. See FR-001..FR-005.
/// </summary>
public interface IQuiescenceSignal
{
    /// <summary>Current composite state. Reads are lock-free.</summary>
    QuiescenceState CurrentState { get; }

    /// <summary>Convenience: <c>true</c> iff the runtime is accepting new work.</summary>
    bool IsAcceptingNewWork { get; }

    /// <summary>Number of active bursts currently running under this runtime.</summary>
    int ActiveBurstCount { get; }

    /// <summary>Fires exactly once per effective state transition. Never fires on idempotent no-ops.</summary>
    event EventHandler<QuiescenceState>? StateChanged;

    /// <summary>
    /// Enters <see cref="QuiescenceReason.Drain"/>. Forward-only within a runtime generation.
    /// Called by the drain orchestrator; not intended for direct invocation by user code.
    /// </summary>
    ValueTask<QuiescenceState> BeginDrainAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enters <see cref="QuiescenceReason.AdministrativePause"/>. Idempotent — a repeat call returns the current state
    /// without publishing a new transition. When pause persistence is enabled, writes the persisted key.
    /// </summary>
    ValueTask<QuiescenceState> PauseAsync(string? reasonText, string? requestedBy, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the <see cref="QuiescenceReason.AdministrativePause"/> flag. Has no effect while the <see cref="QuiescenceReason.Drain"/>
    /// flag is also set — resume during drain is a no-op; callers inspect the returned state to detect this.
    /// Idempotent.
    /// </summary>
    ValueTask<QuiescenceState> ResumeAsync(string? requestedBy, CancellationToken cancellationToken);
}
