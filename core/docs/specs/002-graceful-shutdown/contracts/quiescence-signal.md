# Contract: `IQuiescenceSignal`

**Module**: `Elsa.Workflows.Runtime/Contracts`
**Lifetime**: Singleton within the workflow-runtime container (per R3 — container-scoped means one per shell in multi-shell hosts, one per host in single-shell hosts).

## Purpose

The single source of truth that any collaborator (ingress sources, internal workers, health checks, admin endpoints) consults to determine whether the runtime is currently accepting new work, and for what reason it is not.

## Surface

```csharp
public interface IQuiescenceSignal
{
    QuiescenceState CurrentState { get; }

    bool IsAcceptingNewWork { get; }                       // derived: CurrentState.Reason == None

    // Drain is forward-only (FR-002). Returns the composite post-call state.
    ValueTask<QuiescenceState> BeginDrainAsync(CancellationToken ct = default);

    // Administrative pause is reversible (FR-003).
    ValueTask<QuiescenceState> PauseAsync(string? reasonText, string? requestedBy, CancellationToken ct);
    ValueTask<QuiescenceState> ResumeAsync(string? requestedBy, CancellationToken ct);

    int ActiveExecutionCycleCount { get; }                          // FR-032
}
```

## Invariants

1. **Forward-only drain** — `BeginDrainAsync` sets the `Drain` flag and never clears it (FR-002).
2. **Composability** — calling `PauseAsync` during an active drain adds the `AdministrativePause` flag; `ResumeAsync` clears only that flag, never `Drain` (FR-004).
3. **Idempotent pause** — a second `PauseAsync` while `Reason HAS AdministrativePause` is a no-op with no new audit event (SC-007).
4. **Idempotent resume** — a `ResumeAsync` while `Reason !HAS AdministrativePause` is a no-op.
5. **Resume rejected during drain** — per Edge Cases: `ResumeAsync` MUST NOT transition out of pause if `Drain` is still set. Implementation returns the unchanged state; callers can detect via `CurrentState.Reason`.

## Failure modes

- `PauseAsync`/`ResumeAsync` do NOT throw on source-level failures. Failures are isolated inside the drain orchestrator and surfaced via `IIngressSourceRegistry` and `DrainOutcome`.
- `BeginDrainAsync` is never called by user code; only by `DrainOrchestratorHostedService`.

## Persistence

When `GracefulShutdownOptions.PausePersistence == AcrossReactivations`, the implementation re-applies any persisted pause state via `InitializePersistedStateAsync`, invoked at runtime startup by `InitializePauseStateStartupTask` (IModule path) or `InitializePauseStateShellInitializer` (CShells path). On a hit, `CurrentState.Reason` is initialized to `AdministrativePause`. `PauseAsync` writes the key (under a per-shell discriminator: `elsa.quiescence.pause.{shellId}`); `ResumeAsync` clears it. `BeginDrainAsync` does NOT persist — drain is a per-generation event.

## Concrete implementation

`QuiescenceSignal` — thread-safe state-machine backed by a single lock for transition atomicity. Reads are lock-free (volatile read of `CurrentState` reference).
