# Contract: `IDrainOrchestrator`

**Module**: `Elsa.Workflows.Runtime/Contracts`
**Lifetime**: Singleton; invoked by `DrainOrchestratorHostedService` during `IHostedService.StopAsync`, and by the force-stop admin endpoint during runtime.

## Purpose

Owns the drain protocol end-to-end: parallel pause of every `IIngressSource`, wait for `ExecutionCycleRegistry.ActiveCount` to reach zero within the drain deadline, force-cancel + mark-`Interrupted` on breach, emit the forensic `WorkflowInterrupted` event per affected instance, and return a `DrainOutcome`.

## Surface

```csharp
public interface IDrainOrchestrator
{
    ValueTask<DrainOutcome> DrainAsync(DrainTrigger trigger, CancellationToken ct);
}

public enum DrainTrigger
{
    HostStopSignal,
    ShellDeactivation,
    OperatorForce,        // from the admin force endpoint
}
```

## Protocol (invariants + ordering)

1. **Enter drain** — `IQuiescenceSignal.BeginDrainAsync()` is called first. This sets the signal before any pause is requested, so that any collaborator polling the signal sees drain immediately.
2. **Parallel pause** — for every registered ingress source, start `IIngressSource.PauseAsync(ct)` with a linked token that expires at `min(now + source.PauseTimeout, deadline)`. The orchestrator tracks per-source completion:
   - Success → `IngressSourceState.Paused`.
   - Timeout or exception → `IngressSourceState.PauseFailed`. If the source implements `IForceStoppable`, `ForceStopAsync` is invoked with a fresh token bounded by the remaining deadline.
   - None of these propagate — they are collected into the `DrainOutcome.Sources` list (FR-013).
3. **Wait for execution cycles** — poll `IExecutionCycleRegistry.ActiveCount` on a short interval (10 ms) until it reaches zero OR the drain deadline elapses.
4. **On deadline breach** — iterate live `ExecutionCycleHandle`s; for each:
   - Cancel `CycleCts`.
   - Await persistence of the workflow instance in `Interrupted` sub-status.
   - Emit `WorkflowInterrupted` log entry with typed payload.
   - If persistence itself fails, still mark the execution cycle as interrupted in memory and record `Reason = "PersistenceFailure"` in the payload (the instance will likely be recovered later by the timeout-based `RestartInterruptedWorkflowsTask`).
5. **Exit** — return `DrainOutcome`. The caller (typically `DrainOrchestratorHostedService`) logs the outcome and returns from `StopAsync`. The host then continues with its own shutdown.

## Invariants

1. **Single invocation per generation** — `DrainAsync` is called at most once for `HostStopSignal` or `ShellDeactivation`. A second call during the same generation is an error and throws `InvalidOperationException`. `OperatorForce` from the admin endpoint is allowed once; subsequent force calls are no-ops returning the previous outcome.
2. **Deadline clamping** — the effective deadline is `min(GracefulShutdownOptions.DrainDeadline, HostOptions.ShutdownTimeout - safetyEpsilon)`. Safety epsilon is 500 ms; it exists to guarantee persistence finishes before the host kills the process.
3. **No new execution cycles after step 1** — after `BeginDrainAsync` returns, any attempt to start a execution cycle via the dispatcher is rejected. Internal workers skip their next dequeue; external ingress sources observe their own `PauseAsync` signal. This is the property that makes step 3's "wait for execution cycles to reach zero" terminate.
4. **Heartbeat survives** — `InstanceHeartbeatService` is NOT registered as an ingress source and is not paused. It stops only when the hosted-service ordering brings it down after the drain orchestrator (R5).
5. **No exceptions escape** — every exception path inside `DrainAsync` is captured into `DrainOutcome`. The only way `DrainAsync` throws is if `BeginDrainAsync` was called twice (invariant 1).

## `DrainOutcome` consumers

- `DrainOrchestratorHostedService` logs the outcome at `LogLevel.Information`, or `Warning` if `OverallResult ∈ { DeadlineExceeded, Forced }`.
- The admin `status` endpoint does NOT surface historical drain outcomes (drain is per-generation; after drain the process is exiting anyway).

## Integration with existing runtime types

- `ExecutionCycleRegistry` is the new accounting layer. Every call site that currently dispatches work — `IStimulusDispatcher`, `IWorkflowRunner.RunAsync`, `IWorkflowRestarter` — registers a `ExecutionCycleHandle` on entry and disposes it on exit. These are the three choke points; no other types create execution cycles.
- Cancellation propagation into activities reuses the existing `ActivityExecutionContext.CancellationToken` — no change needed to activity-authoring guidance.
