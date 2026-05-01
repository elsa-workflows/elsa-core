# Quickstart: Graceful Shutdown for the Workflow Runtime

**Audience**: Elsa module authors and integrators.
**Prerequisite**: Elsa 3.x with `WorkflowRuntimeFeature` enabled.

This quickstart shows three scenarios: (a) observing graceful drain on host stop, (b) driving the runtime via the admin endpoints, (c) implementing a new ingress source.

---

## a) Observe graceful drain on host stop

No code change is required. After installing the feature:

```csharp
builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.ConfigureGracefulShutdown(options =>
        {
            options.DrainDeadline = TimeSpan.FromSeconds(30);
            options.IngressPauseTimeout = TimeSpan.FromSeconds(5);
            options.PausePersistence = PausePersistencePolicy.SessionScoped;
        });
    });
});
```

Send SIGTERM to the host process (or hit `Ctrl+C` in development). The graceful-drain implementation does run on shutdown, but the current log messages are slightly different from the original design notes.

What you should expect today is:

1. `Drain initiated (trigger=HostStopSignal, deadline=...)` on the legacy host-level runtime path, or `Drain initiated (trigger=ShellDeactivation, deadline=...)` in CShells-hosted deployments.
2. `Ingress pause phase complete in ...` after ingress-source pause attempts finish.
3. If the deadline is exceeded (or an operator forces a drain), `Drain deadline exceeded; force-cancelling N active execution cycle(s).`
4. `Drain completed: CompletedWithinDeadline ...` (or `Drain completed: DeadlineExceeded ...`).
5. A caller-level summary such as `Graceful drain finished: CompletedWithinDeadline ...` or `Shell drain finished: CompletedWithinDeadline ...`.
6. Normal host shutdown messages.

There are currently no separate informational log entries for “pause requested”, “all ingress sources paused”, “waiting for active execution cycles”, or “instance heartbeat stopped”. Those phases are still part of the shutdown protocol; they are just summarized differently in the current implementation.

These messages are emitted at `Information`/`Warning` level from the `Elsa.Workflows.Runtime` categories, so make sure your logging filters are not excluding them.

If any workflow was force-cancelled, it will have `SubStatus = Interrupted` in the database and a `WorkflowInterrupted` entry in its execution log. On next host start, `RecoverInterruptedWorkflowsStartupTask` will requeue it immediately — no waiting for the timeout-based crash recovery.

---

## b) Drive the runtime via admin endpoints

Assuming the host exposes the Elsa API on `https://localhost:8080` with Bearer auth:

**Pause**

```http
POST /admin/workflow-runtime/pause
Authorization: Bearer <token with ManageWorkflowRuntime>
Content-Type: application/json

{ "reason": "migrating broker" }
```

**Check status**

```http
GET /admin/workflow-runtime/status
Authorization: Bearer <token>
```

Response includes per-source state. A `PauseFailed` entry is expected behavior if one source is misbehaving — it does not block pause.

**Resume**

```http
POST /admin/workflow-runtime/resume
Authorization: Bearer <token>
```

Returns 409 if a drain has been initiated — that is the edge case "Resume is requested while drain is in progress".

**Force drain (operator escalation)**

```http
POST /admin/workflow-runtime/force-drain
Authorization: Bearer <token>
```

This immediately cancels active execution cycles and marks their instances `Interrupted`. The host keeps running but will accept no new work until restart.

---

## c) Implement a new ingress source

A third-party module (say, a Kafka consumer) registers itself so that graceful drain and administrative pause propagate into it uniformly.

```csharp
public sealed class KafkaConsumerIngressSource : IIngressSource, IForceStoppable
{
    private readonly KafkaConsumer _consumer;
    private int _state = (int)IngressSourceState.Running;

    public KafkaConsumerIngressSource(KafkaConsumer consumer) => _consumer = consumer;

    public string Name => "kafka.consumer";

    public TimeSpan PauseTimeout => TimeSpan.FromSeconds(10);   // overrides global default

    public IngressSourceState CurrentState => (IngressSourceState)Volatile.Read(ref _state);

    public async ValueTask PauseAsync(CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _state, (int)IngressSourceState.Pausing)
            == (int)IngressSourceState.Paused) return;

        await _consumer.StopConsumingAsync(ct);
        Volatile.Write(ref _state, (int)IngressSourceState.Paused);
    }

    public async ValueTask ResumeAsync(CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _state, (int)IngressSourceState.Resuming)
            == (int)IngressSourceState.Running) return;

        await _consumer.StartConsumingAsync(ct);
        Volatile.Write(ref _state, (int)IngressSourceState.Running);
    }

    public ValueTask ForceStopAsync(CancellationToken ct) => _consumer.AbortAsync(ct);
}
```

Registration:

```csharp
services.AddIngressSource<KafkaConsumerIngressSource>();
```

That is the entire integration. The runtime handles:
- Pause timeout enforcement,
- Force-stop escalation when the pause timeout elapses,
- State publication in the admin status endpoint,
- Per-execution cycle attribution (if the consumer sets `IngressSourceName = "kafka.consumer"` on the dispatch it initiates).

---

## Verifying the three user stories locally

| Story | Manual check |
|-------|--------------|
| US1 (host stop) | Run a workflow with a 10 s activity; send SIGTERM mid-execution cycle; observe the process waiting up to the deadline and the instance reaching a clean persisted state. |
| US2 (admin pause/resume) | Hit `pause`, schedule a stimulus (external event); observe the execution log shows the stimulus was buffered but not dispatched. Hit `resume`; observe dispatch. |
| US3 (interrupted recovery) | Set `DrainDeadline = 1s`; run a 10 s activity; SIGTERM; confirm `SubStatus = Interrupted` + `WorkflowInterrupted` log entry; restart host; observe immediate requeue without waiting for `RestartInterruptedWorkflowsTask`'s cadence. |

---

## Further reading

- [`plan.md`](./plan.md) — implementation plan and constitution alignment.
- [`research.md`](./research.md) — decisions R1–R10 with rationale.
- [`data-model.md`](./data-model.md) — entity shapes and invariants.
- [`contracts/`](./contracts/) — detailed interface contracts for the quiescence signal, ingress source, drain orchestrator, and admin endpoints.
