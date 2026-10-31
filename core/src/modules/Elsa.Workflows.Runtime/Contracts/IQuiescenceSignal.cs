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

    /// <summary>Convenience: <c>true</c> if the runtime is accepting new work.</summary>
    bool IsAcceptingNewWork { get; }

    /// <summary>Number of active execution cycles currently running under this runtime.</summary>
    int ActiveExecutionCycleCount { get; }

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

    /// <summary>
    /// Loads any persisted administrative pause state and re-applies it to the in-memory state. Called once per
    /// runtime generation by the runtime's startup lifecycle (an <c>IStartupTask</c> in IModule deployments and
    /// an <c>IShellInitializer</c> in shell-aware deployments) when the configured pause-persistence policy
    /// requires across-reactivation persistence. Implementations that do not persist state can return a
    /// no-op task — the contract is intentionally lifecycle-aware so consumers don't need to type-check
    /// against a concrete implementation.
    /// </summary>
    ValueTask InitializePersistedStateAsync(CancellationToken cancellationToken);
}
