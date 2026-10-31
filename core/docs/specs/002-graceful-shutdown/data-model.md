# Data Model: Graceful Shutdown for the Workflow Runtime

**Feature**: `002-graceful-shutdown`
**Date**: 2026-04-24
**Sources**: spec.md Key Entities section, research.md decisions R1–R10.

This document fixes the shape of every new or modified type at the **contract / invariant** level. Exact method signatures live in the `contracts/` files.

---

## 1. `QuiescenceReason` (flags enum, new)

```csharp
[Flags]
public enum QuiescenceReason
{
    None              = 0,
    AdministrativePause = 1 << 0,
    Drain               = 1 << 1,
}
```

**Invariants**
- `None` means the runtime is in normal operation.
- `Drain` is forward-only within a generation (FR-002). Once set, never cleared.
- `AdministrativePause` is reversible (FR-003).
- The two values are composable (FR-004).

---

## 2. `QuiescenceState` (record, new)

Fields:
- `QuiescenceReason Reason` — current composite reason.
- `bool IsAcceptingNewWork` — derived convenience: `Reason == QuiescenceReason.None`.
- `DateTimeOffset? PausedAt` — set when `AdministrativePause` is first applied, cleared on resume.
- `DateTimeOffset? DrainStartedAt` — set when `Drain` is first applied; never cleared within the generation.
- `string? PauseReasonText` — optional caller-supplied reason (FR-030).
- `string? PauseRequestedBy` — identifier of the principal who requested pause (audit, FR-034).
- `string GenerationId` — opaque identifier of the current runtime generation (host process ID or shell activation ID). Opaque to callers; compared for equality only.

**Invariants**
- `(Reason HAS Drain)` ⇒ `DrainStartedAt.HasValue`.
- `(Reason HAS AdministrativePause)` ⇒ `PausedAt.HasValue`.
- `GenerationId` never changes within a single `IQuiescenceSignal` instance.

---

## 3. `IngressSourceState` (enum, new)

```csharp
public enum IngressSourceState
{
    Running,
    Pausing,
    Paused,
    PauseFailed,
    Resuming,
    ResumeFailed,
}
```

**Transitions** (observable via status query, FR-011):

```
                         pause()
    Running  ───────────────────────────────>  Pausing
       ▲                                         │
       │                              success    │    timeout/throw
       │                                         │
       │                                         ▼
       │                                       Paused     ───────────┐
       │                                         │                   │
       │  resume()                   resume()    │                   │
       └──────────────── Resuming <──────────────┘                   │
                          │                                          │
                          │  success                                 │
                          │                                          │
                          └─────────> Running                        │
                                                                     ▼
                                                              PauseFailed
                                                                     │
                                                                     │  resume()
                                                                     │
                                                                     ▼
                                                              Resuming
                                                                     │
                                                                     ▼
                                                              Running or ResumeFailed
```

**Invariants**
- `Pausing → Paused` ONLY via the source's own acknowledgement.
- `Pausing → PauseFailed` after the per-source pause timeout elapses OR an exception is captured (FR-012).
- `Resuming → ResumeFailed` after an exception. A `ResumeFailed` source stays in that state until the next successful resume, but does NOT block normal operation — it simply will not receive any work-start attempts until manually retried.
- `PauseFailed` is reachable from `Running` without going through `Pausing` only when the execution cycle-attribution check fires (FR-018) — a source that claims `Paused` but delivers is flipped directly.

---

## 4. `IIngressSource` registration record (internal, new)

Fields captured at `services.AddIngressSource(...)` time:
- `string Name` — stable, dot-separated identifier (e.g., `http.trigger`, `scheduling.cron`, `internal.bookmark-queue-worker`).
- `TimeSpan PauseTimeout` — overrides `GracefulShutdownOptions.IngressPauseTimeout` for this source only.
- `bool SupportsForceStop` — true when the registering type also implements `IForceStoppable`.
- `IngressSourceState State` — live, owned by `IIngressSourceRegistry`.
- `Exception? LastError` — last captured pause/resume failure.
- `DateTimeOffset? LastTransitionAt`.

**Invariants**
- `Name` is unique within a single `IIngressSourceRegistry`. Duplicate registration throws at startup.
- `PauseTimeout` > `TimeSpan.Zero`.

---

## 5. `ExecutionCycleHandle` (record, new)

Fields:
- `Guid Id` — unique per execution cycle within a generation.
- `string WorkflowInstanceId`.
- `string? IngressSourceName` — null when the execution cycle originated from direct API call (e.g., `IWorkflowRunner.RunAsync` from application code).
- `DateTimeOffset StartedAt`.
- `CancellationTokenSource CycleCts` — used to force-cancel on deadline breach (not exposed on the contract surface, kept internal to `ExecutionCycleRegistry`).

**Invariants**
- A `ExecutionCycleHandle` is created exactly once per execution cycle and disposed when the execution cycle completes or is force-cancelled — no resurrection.
- The active-execution cycle count reported by `IQuiescenceSignal` equals the number of live `ExecutionCycleHandle`s.

---

## 6. `WorkflowSubStatus` — add `Interrupted`

Existing enum at `src/modules/Elsa.Workflows.Core/Enums/WorkflowSubStatus.cs` (mirrored in `Elsa.Api.Client`):

```csharp
public enum WorkflowSubStatus
{
    Pending,
    Executing,
    Suspended,
    Finished,
    Cancelled,
    Faulted,
    Interrupted,  // NEW
}
```

**Semantics**
- `Interrupted` means "the last execution cycle was force-cancelled by the runtime during graceful drain; the instance is resumable."
- An `Interrupted` instance MUST have `IsExecuting = false` (that's what guarantees disjointness with `RestartInterruptedWorkflowsTask`'s filter).
- When the instance is requeued by `RecoverInterruptedWorkflowsStartupTask`, the sub-status transitions back to `Pending` (or directly to `Executing` if immediate dispatch happens) — the forensic log entry persists (FR-023).

---

## 7. `WorkflowInterruptedPayload` (record, new)

Typed payload stored in `WorkflowExecutionLogRecord.Payload` whenever `EventName == "WorkflowInterrupted"`:

- `DateTimeOffset InterruptedAt`.
- `string Reason` — enum discriminator: `"DeadlineBreach"` | `"OperatorForce"` | `"PersistenceFailure"`.
- `string GenerationId` — the runtime generation that was draining.
- `string? LastActivityId` — last activity observed executing when cancellation was requested.
- `string? LastActivityNodeId`.
- `string? IngressSourceName` — source that started the interrupted execution cycle, if known.
- `TimeSpan ExecutionCycleDuration` — elapsed time from execution cycle start to interruption. Persisted under the JSON wire key `"BurstDuration"` for backwards compatibility with pre-rename log records.

**Invariants**
- Written exactly once per force-cancelled execution cycle, synchronously with the persistence of the `Interrupted` sub-status.
- Payload is immutable after write.

---

## 8. `DrainOutcome` (record, new)

Fields:
- `DrainResult OverallResult` — enum `{ CompletedWithinDeadline, DeadlineExceeded, Forced, AbortedByUnhandledException }`.
- `DateTimeOffset StartedAt`, `DateTimeOffset CompletedAt`.
- `TimeSpan PausePhaseDuration` — wall time for the parallel pause step.
- `TimeSpan WaitPhaseDuration` — wall time waiting for active-execution cycle count to reach zero.
- `IReadOnlyList<IngressSourceFinalState> Sources` — per-source final `State`, `LastError`, `WasForceStopped`.
- `int ExecutionCyclesForceCancelledCount`.
- `IReadOnlyList<string> ForceCancelledInstanceIds` — capped at a configurable maximum (default 100) to avoid unbounded payloads in logs; count still reflects the true total.

**Invariants**
- `PausePhaseDuration + WaitPhaseDuration ≤ configured drain deadline + small epsilon` (allows for persistence latency on the terminal flush).
- `ExecutionCyclesForceCancelledCount == 0` ⇒ `OverallResult ∈ { CompletedWithinDeadline, AbortedByUnhandledException }`.

---

## 9. `GracefulShutdownOptions` (options class, new)

Registered via `IOptions<GracefulShutdownOptions>`; bound from `Runtime:GracefulShutdown` configuration section by convention.

- `TimeSpan DrainDeadline = TimeSpan.FromSeconds(30)` — R2.
- `TimeSpan IngressPauseTimeout = TimeSpan.FromSeconds(5)` — R1.
- `int StimulusQueueMaxDepthWhilePaused = 10_000` — FR-025.
- `StimulusQueueOverflowPolicy OverflowPolicy = Buffer` — R6.
- `PausePersistencePolicy PausePersistence = SessionScoped` — R8.
- `int MaxForceCancelledInstanceIdsReported = 100`.

Validation:
- All `TimeSpan` fields must be > `TimeSpan.Zero`.
- `StimulusQueueMaxDepthWhilePaused` must be > 0.

---

## 10. Storage touchpoints (summary)

| Concern | Storage | Change |
|---------|---------|--------|
| `WorkflowInstance.SubStatus` | existing EF entity | No schema change (int column); adding an enum value extends the value domain only. Compatibility verified per provider during QA. |
| `WorkflowExecutionLogRecord` for `WorkflowInterrupted` events | existing `IWorkflowExecutionLogStore` | No schema change — uses existing `EventName` + JSON `Payload` columns. |
| Persisted pause state (when enabled) | existing `Elsa.KeyValues` store | No schema change — one key per shell name. |
| Active-execution cycle registry | in-memory only | Never persisted. Execution cycles that were force-cancelled are recovered via the `Interrupted` sub-status, not via a registry replay. |
| `IngressSourceRegistry` state | in-memory only | Rebuilt on each generation from DI registrations. |

No migrations are required. Provider-specific EF projects are unaffected unless QA on any given provider reveals an integer-width or enum-conversion anomaly, in which case a migration would be scoped to that provider only.

---

## Cross-references

- FR-001..FR-005 map to `IQuiescenceSignal` + `QuiescenceState` + `QuiescenceReason`.
- FR-006..FR-013, FR-018 map to `IIngressSource` + `IngressSourceState` + registration record.
- FR-014..FR-017 map to `ExecutionCycleHandle` + `IExecutionCycleRegistry` + `DrainOutcome`.
- FR-019..FR-023 map to `WorkflowSubStatus.Interrupted` + `WorkflowInterruptedPayload`.
- FR-024..FR-026 map to `StimulusQueueOverflowPolicy` + the decorator in R6.
- FR-027..FR-029 map to the hosted-service registration order in R5 and the scoped `IQuiescenceSignal` in R3.
- FR-030..FR-034 map to admin endpoints (see `contracts/admin-endpoints.md`).
- FR-035 maps to `GracefulShutdownOptions`.
