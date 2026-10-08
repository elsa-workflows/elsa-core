# Contract: `IIngressSource` (+ `IForceStoppable`)

**Module**: `Elsa.Workflows.Runtime/Contracts`
**Lifetime**: Scoped to the runtime container; instances are discovered via DI and registered with `IIngressSourceRegistry` at feature activation time.

## Purpose

Every component that injects external events into the workflow engine — first-party message consumers, schedulers, HTTP trigger handlers, internal durable-queue processors, internal recurring tasks that enqueue work, and third-party modules — implements this contract so that the runtime can pause them uniformly during drain or administrative pause (FR-006).

## Surface

```csharp
public interface IIngressSource
{
    string Name { get; }

    TimeSpan PauseTimeout { get; }                         // R1 — default 5 s unless overridden

    IngressSourceState CurrentState { get; }               // FR-011

    ValueTask PauseAsync(CancellationToken ct);

    ValueTask ResumeAsync(CancellationToken ct);
}

public interface IForceStoppable                           // FR-008, optional
{
    ValueTask ForceStopAsync(CancellationToken ct);
}
```

`IIngressSourceRegistry` — exposed to admin endpoints and the drain orchestrator; not part of the public ingress-adapter contract:

```csharp
public interface IIngressSourceRegistry
{
    IReadOnlyCollection<IngressSourceSnapshot> Snapshot();

    // Atomic per-source transition tracking; used internally by DrainOrchestrator.
    ValueTask MarkPauseFailedAsync(string name, string reason, Exception? error = null);
}
```

## Invariants

1. **Idempotence** — `PauseAsync` on an already-paused source returns immediately (no-op); `ResumeAsync` on an already-running source returns immediately (FR-010).
2. **Concurrency safety** — concurrent `PauseAsync`/`ResumeAsync` calls MUST converge to a single state; implementations typically guard with a semaphore or CAS on their internal state field.
3. **Timeout model** — the orchestrator enforces `PauseTimeout` by racing the source's `PauseAsync` against a linked `CancellationTokenSource`. The source itself SHOULD observe the token but is not required to — a source that hangs past its timeout is marked `PauseFailed` regardless.
4. **State-query truthfulness** — `CurrentState` MUST be consistent with whether the source is actively delivering. A source that reports `Paused` but continues to start execution cycles is detected via the execution cycle-attribution mechanism (FR-018, R7) and its registry entry is flipped to `PauseFailed`.
5. **No resume during drain** — implementations MAY refuse `ResumeAsync` if they can see the ambient quiescence signal indicates `Drain`. In practice the orchestrator never calls `ResumeAsync` during drain.

## Error semantics

- A `PauseAsync` call that throws leaves the source in `PauseFailed` with the exception captured in the registry snapshot. The drain does NOT propagate the exception; it records and continues (FR-012, FR-013).
- A `PauseAsync` call that hangs past `PauseTimeout` is abandoned (`CancellationToken` fires, registry records "timeout"). If the source also implements `IForceStoppable`, `ForceStopAsync` is invoked with a fresh token bounded by the remaining overall drain deadline.
- `IForceStoppable.ForceStopAsync` failures are logged but do not block drain.

## Registration

```csharp
public static IServiceCollection AddIngressSource<TSource>(
    this IServiceCollection services,
    Action<IngressSourceRegistrationOptions>? configure = null)
    where TSource : class, IIngressSource;
```

`IngressSourceRegistrationOptions` exposes:
- `TimeSpan? PauseTimeoutOverride`

If the source type also implements `IForceStoppable`, the registrar automatically registers the force-stop capability — adapters do not need a separate registration call.

## First-party adapters covered in this feature

| Name | Implementation | Wraps |
|------|----------------|-------|
| `http.trigger` | `HttpTriggerIngressSource` | `HttpWorkflowsMiddleware` dispatch |
| `scheduling.cron` | `ScheduledTriggerIngressSource` | scheduler tick loop |
| `internal.bookmark-queue-worker` | `BookmarkQueueWorker` itself (becomes an `IIngressSource`) | existing dequeue loop |

Adapters for other ingress modules (message brokers, etc.) are out of scope for this feature — once the contract is in place, each transport module adds its own adapter in a follow-up PR (per Constitution VI — one PR, one concern).
