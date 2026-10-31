# Phase 0 Research: Graceful Shutdown for the Workflow Runtime

**Feature**: `002-graceful-shutdown`
**Date**: 2026-04-24
**Inputs**: spec.md, constitution.md v1.0.0, repository inventory of `src/modules/Elsa.Workflows.Runtime`, `src/modules/Elsa.Hosting.Management`, `src/modules/Elsa.Shells.Api`, `src/common/Elsa.Features`, `src/common/Elsa.Api.Common`, and `src/modules/Elsa.Workflows.Core/Enums/WorkflowSubStatus.cs`.

Each research item resolves to a single **Decision** the implementation follows, a **Rationale**, and the **Alternatives considered**. No NEEDS CLARIFICATION markers remain after this document.

---

## R1 — Pause-timeout default (per-ingress-source)

**Decision**: Default per-source pause timeout of **5 seconds**, overridable at registration and by configuration under `GracefulShutdownOptions:IngressPauseTimeout` (TimeSpan).

**Rationale**: 5 s matches the behaviour ASP.NET Core gives HTTP middleware shutdown before forced cancellation, and it matches the order of magnitude of the existing `BookmarkQueueWorker` poll interval. It is long enough for a message consumer to acknowledge in-flight deliveries but short enough that an overall 30 s drain deadline can absorb ≥ 5 serial pause failures without force-stop escalation dominating the budget.

**Alternatives considered**:
- 1 s — too aggressive for HTTP request listeners mid-flush.
- 30 s — equal to the default drain deadline, which would make a single bad source block the entire drain budget.
- "No timeout; wait forever" — violates FR-012 and SC-004.

---

## R2 — Drain-deadline default and configuration source

**Decision**: Default overall drain deadline of **30 seconds**, configured via `GracefulShutdownOptions:DrainDeadline` (TimeSpan). Also sourced from `IHostApplicationLifetime` / `HostOptions.ShutdownTimeout` when the host has set it explicitly; the shorter of the two wins so that the runtime never exceeds the host's own shutdown budget.

**Rationale**: 30 s is the .NET host default for `HostOptions.ShutdownTimeout` and is the de-facto Kubernetes `terminationGracePeriodSeconds` default. Aligning on it means production tuning does not have to reconcile two numbers. Taking the minimum of the runtime option and the host option prevents a badly-tuned runtime from being killed mid-persistence by the orchestrator.

**Alternatives considered**:
- Use only the host's `ShutdownTimeout` and expose no runtime-level option — loses the ability to tune drain separately from, say, HTTP server quiescence.
- Use only a runtime option — breaks the invariant that the runtime can never outlast the host's terminating process.

---

## R3 — Shell deactivation hook: extend `IShellFeature` vs. pure `IHostedService.StopAsync`

**Decision**: **Start with the `IHostedService.StopAsync` fallback.** Register `DrainOrchestratorHostedService` inside `WorkflowRuntimeFeature`; it observes `IHostApplicationLifetime.ApplicationStopping` and runs the drain. Do **not** extend `IShellFeature` in this feature.

**Rationale**: `IShellFeature` in `src/common/Elsa.Features/` is load-bearing across the entire solution (100+ projects). Adding an async `DeactivateAsync` method is a breaking contract change that would cascade through every feature in the codebase — violating Constitution VII (Simplicity & Focus) for a benefit that is already achievable. `IHostApplicationLifetime.ApplicationStopping` fires before the DI container is disposed, which is enough to satisfy FR-027 in a single-shell host. For multi-shell hosts built on `Elsa.Shells.Api`, the existing reload endpoints (`Reload`, `ReloadAll`) already tear down and rebuild shell-scoped service providers; a per-shell quiescence signal registered in the shell container is automatically disposed when the shell is rebuilt, which naturally scopes drain to that shell. If a future feature genuinely needs a first-class deactivation hook, it should be a separate, minor-version `IShellFeature` change covered by its own spec.

**Alternatives considered**:
- Extend `IShellFeature` with `ValueTask DeactivateAsync(CancellationToken)` as an optional default-implemented method — attractive for semantic clarity but risks a cascade of trivial "implemented it but did nothing" overrides across unrelated features.
- Use `IAsyncDisposable` on the shell container's service provider — works in theory, but the DI container's disposal order is not guaranteed relative to other disposables, and throwing inside `DisposeAsync` is discouraged.

---

## R4 — Distinguishing the new `Interrupted` sub-status from the existing `RestartInterruptedWorkflowsTask`

**Decision**: Keep the existing `RestartInterruptedWorkflowsTask` **unchanged** (its filter `{ IsExecuting = true, BeforeLastUpdated < now - InactivityThreshold }` is correct for ungraceful crash recovery). The new graceful-shutdown scan is a separate **startup task** named `RecoverInterruptedWorkflowsStartupTask` with filter `{ SubStatus = WorkflowSubStatus.Interrupted }`. Their filters are disjoint: a gracefully-interrupted instance has `IsExecuting = false` and `SubStatus = Interrupted`; a crash-victim instance has `IsExecuting = true` and any sub-status. No rename of the existing task in this feature — a rename would be a confusingly-scoped change and would churn public-ish class names. Document the historical naming in the execution log commentary.

**Rationale**: FR-022 mandates that the existing mechanism be preserved verbatim. The filters being disjoint means the two paths cannot ever double-recover the same instance. Renaming `RestartInterruptedWorkflowsTask` for clarity is a reasonable follow-up but belongs in its own PR so that it doesn't mix with the behaviour change in this feature (Constitution VI).

**Alternatives considered**:
- Rename the existing task to `RestartStaleWorkflowsTask` as part of this feature — mixes concerns; requires coordinated config-key migration for anyone overriding `RestartInterruptedWorkflowsBatchSize`.
- Merge both recovery paths into one task — breaks the explicit cardinality difference (one runs every shell activation, exactly once; the other is a recurring background task) and makes it harder to reason about which mechanism fired.

---

## R5 — Interaction of the existing `InstanceHeartbeatService` with drain

**Decision**: `InstanceHeartbeatService` MUST remain running throughout drain and stop only after the drain orchestrator signals completion. Registered `StartAsync`/`StopAsync` ordering in `WorkflowRuntimeFeature` guarantees this by listing `DrainOrchestratorHostedService` **after** `InstanceHeartbeatService` — `IHostedService.StopAsync` runs in reverse registration order, so drain stops first, heartbeat stops last.

**Rationale**: FR-029 exists because the timeout-based `RestartInterruptedWorkflowsTask` on a sibling node uses the heartbeat to decide whether a "stale" instance truly indicates a dead host. If the heartbeat stopped before drain completed, another node could false-positive-recover a workflow that is quietly finishing its execution cycle here. Ordering via hosted-service registration is the idiomatic .NET way to enforce this without extra coordination primitives.

**Alternatives considered**:
- Let both services stop in parallel and have the drain orchestrator write the heartbeat directly — unnecessary coupling; two services writing the same row invites a race.
- Extend `InstanceHeartbeatService` with a "drain in progress" flag — solves no real problem; the timestamp already conveys liveness.

---

## R6 — Stimulus-queue back-pressure policy representation

**Decision**: Introduce a `StimulusQueueOverflowPolicy` enum on `GracefulShutdownOptions` with values `{ Buffer, Reject }`. Default: `Buffer` (per clarification decision 3). The existing `IBookmarkQueue` is not modified; a new `BackpressureAwareBookmarkQueue` decorator (registered only when `Reject` is selected) wraps the existing `StoreBookmarkQueue` and throws a typed `StimulusQueueOverflowException` past the configured `MaxDepthWhilePaused`. Readiness degradation is surfaced via an `IHealthCheck` implementation (`GracefulShutdownHealthCheck`) that reads from `IQuiescenceSignal` and the current queue depth.

**Rationale**: The decorator pattern preserves FR-024 (writes keep flowing in the default policy) and avoids touching `IBookmarkQueue` callers. Using `IHealthCheck` for the degraded-readiness signal reuses the standard ASP.NET Core plumbing — transports that translate readiness into their own back-pressure primitive (Kubernetes readiness probes, message-broker-side circuit-breakers) already watch this.

**Alternatives considered**:
- Push the policy into every caller of `IBookmarkQueue.Enqueue` — scatters logic across modules.
- Expose the queue depth only via a custom event — forces every consumer to wire a new subscription.

---

## R7 — Per-execution cycle ingress attribution plumbing

**Decision**: Threading the originating `IIngressSource` name through the dispatcher is done by adding an optional `string? IngressSourceName` property to the existing `DispatchWorkflowRequest`/`DispatchWorkflowResponse` message shape (already flowing through `IStimulusDispatcher` / `BackgroundStimulusDispatcher`). When a execution cycle starts, the `ExecutionCycleRegistry` records the pair `(ExecutionCycleHandle, IngressSourceName)`. When a execution cycle completes, the registry compares the recorded name against the source's current state — if a execution cycle starts while the source's registry entry says `Paused`, the registry calls `IIngressSourceRegistry.MarkPauseFailed(sourceName, reason: "delivered-while-paused")`.

**Rationale**: Attribution is cheap (an optional string) and the alternative — a side-channel diagnostic queue — duplicates state. The detection logic lives entirely inside the runtime module, so ingress adapters only need to set the name correctly at dispatch time.

**Alternatives considered**:
- AsyncLocal attribution set by the adapter when it calls `IStimulusSender` — works but is brittle across `Task.Run` boundaries.
- Pass attribution only via mediator notifications — adds ordering requirements between the notification and the execution cycle registration.

---

## R8 — Pause persistence across shell reactivation (storage location)

**Decision**: When `GracefulShutdownOptions.PausePersistence = AcrossReactivations`, pause state is written to the existing `KeyValues` module (`src/modules/Elsa.KeyValues`) under a container-scoped key of the shape `elsa.quiescence.pause.{shellName}`. On shell activation, `QuiescenceSignal` reads this key in its constructor and initialises to `Paused` if set. Resume clears the key. Default policy is `SessionScoped` — no persistence.

**Rationale**: `Elsa.KeyValues` already exists for small operational state and is provider-agnostic (same provider story as workflow persistence). No new entity, no new migration, one pre-existing index. This is the simplest persistence story consistent with Constitution VII.

**Alternatives considered**:
- A dedicated `PauseState` table — over-engineered for a single boolean + reason string.
- An in-memory-only pause that does not survive reactivation — fails FR-028 scenario 7 for deployments that configure persistence.

---

## R9 — Authorization permission name for admin endpoints

**Decision**: Introduce one new permission, **`ManageWorkflowRuntime`**, in `Elsa.Api.Common`'s `PermissionNames` constants. All four admin endpoints (`pause`, `resume`, `status`, `force`) require this permission via `ConfigurePermissions(PermissionNames.ManageWorkflowRuntime)` with the standard `EndpointSecurityOptions.SecurityIsEnabled` gate already used elsewhere (evidenced in `Elsa.Shells.Api` endpoints).

**Rationale**: A single coarse permission matches the operational reality — anyone who can pause the runtime can also read its status and, by escalation, force-drain. Splitting into `ViewWorkflowRuntimeStatus`/`PauseWorkflowRuntime`/`ForceDrainWorkflowRuntime` is tempting but has no consuming identity model that distinguishes these roles today (cf. Constitution VII).

**Alternatives considered**:
- Anonymous status endpoint — rejected by FR-032's explicit "authenticated and authorised" language.
- Separate read vs. write permissions — adds configuration surface for no demonstrated need; can be added later without breaking change.

---

## R10 — Distributed runtime worker integration

**Decision**: In `Elsa.Workflows.Runtime.Distributed`, `DistributedBookmarkQueueWorker` (and, symmetrically, `BackgroundStimulusDispatcher` in the core runtime) consult `IQuiescenceSignal` at the top of each dequeue loop iteration. If the signal indicates drain **or** administrative pause, the worker skips dispatch and short-sleeps until the signal clears (for pause) or until the host stops (for drain). The worker registers itself as an `IIngressSource` named `"internal.bookmark-queue-worker"` so that its state is surfaced in the status API alongside external sources.

**Rationale**: Treating the internal worker uniformly as an ingress source (FR-006: "internal durable queue processors") gives operators a single pane of glass: if the bookmark queue is paused, the status response shows why. Reusing the cooperative `IQuiescenceSignal` poll avoids inventing a separate cancellation flow for the worker.

**Alternatives considered**:
- Cancel the worker via `CancellationToken.Cancel()` at pause time — works for drain but breaks the reversibility requirement of administrative pause.
- Keep the worker running and filter at the handler — forces each handler to re-check the signal, leaking the concern.

---

## Summary

| Item | Decision |
|------|----------|
| R1 | Default per-source pause timeout = 5 s |
| R2 | Default drain deadline = 30 s; clamp to `HostOptions.ShutdownTimeout` |
| R3 | Hook drain via `IHostedService.StopAsync`; do not modify `IShellFeature` |
| R4 | Keep `RestartInterruptedWorkflowsTask` unchanged; add disjoint `RecoverInterruptedWorkflowsStartupTask` |
| R5 | Register heartbeat service before drain orchestrator so stop order is drain → heartbeat |
| R6 | Back-pressure = decorator pattern on `IBookmarkQueue`; readiness via `IHealthCheck` |
| R7 | Thread `IngressSourceName` through `DispatchWorkflowRequest`; detect inconsistencies in `ExecutionCycleRegistry` |
| R8 | Pause persistence via `Elsa.KeyValues` under a container-scoped key; default off |
| R9 | Single `ManageWorkflowRuntime` permission for all admin endpoints |
| R10 | Internal workers consult `IQuiescenceSignal` and register as ingress sources |

**No NEEDS CLARIFICATION markers remain. Ready for Phase 1.**
