---
description: "Task list for 002-graceful-shutdown feature implementation"
---

# Tasks: Graceful Shutdown for the Workflow Runtime

**Input**: Design documents from `/specs/002-graceful-shutdown/`
**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`
**Tests**: Included — Constitution V mandates xUnit tests for new runtime code, and the spec's user stories each define an Independent Test criterion.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: `[US1]`, `[US2]`, or `[US3]` — only on user-story tasks
- Paths are absolute from repo root `/Users/sipke/Projects/Elsa/elsa-core/main/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Configuration scaffolding and one new permission constant. No behaviour change.

- [X] T001 [P] Create `GracefulShutdownOptions` class with `DrainDeadline`, `IngressPauseTimeout`, `StimulusQueueMaxDepthWhilePaused`, `OverflowPolicy`, `PausePersistence`, `MaxForceCancelledInstanceIdsReported` + validation (all `TimeSpan` > 0, depth > 0) in `src/modules/Elsa.Workflows.Runtime/Options/GracefulShutdownOptions.cs`
- [X] T002 [P] Create `StimulusQueueOverflowPolicy` enum (`Buffer`, `Reject`) in `src/modules/Elsa.Workflows.Runtime/Options/StimulusQueueOverflowPolicy.cs`
- [X] T003 [P] Create `PausePersistencePolicy` enum (`SessionScoped`, `AcrossReactivations`) in `src/modules/Elsa.Workflows.Runtime/Options/PausePersistencePolicy.cs`
- [X] T004 Add `ManageWorkflowRuntime` constant to the existing `PermissionNames` class in `src/common/Elsa.Api.Common/PermissionNames.cs`
- [X] T005 Add `ConfigureGracefulShutdown(Action<GracefulShutdownOptions>)` fluent extension on the Runtime feature in `src/modules/Elsa.Workflows.Runtime/Extensions/WorkflowRuntimeFeatureExtensions.cs` that binds the options via `IOptions<GracefulShutdownOptions>` and wires it into DI

**Checkpoint**: Options surface compiles; no runtime behaviour yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core enums, models, contracts, and service implementations that every user story consumes. Independent of any single story.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

### Enum & sub-status extensions

- [X] T006 [P] Create `QuiescenceReason` `[Flags]` enum (`None`, `AdministrativePause`, `Drain`) in `src/modules/Elsa.Workflows.Runtime/Enums/QuiescenceReason.cs`
- [X] T007 [P] Create `IngressSourceState` enum (`Running`, `Pausing`, `Paused`, `PauseFailed`, `Resuming`, `ResumeFailed`) in `src/modules/Elsa.Workflows.Runtime/Enums/IngressSourceState.cs`
- [X] T008 Add `Interrupted` value to the core `WorkflowSubStatus` enum in `src/modules/Elsa.Workflows.Core/Enums/WorkflowSubStatus.cs` with XML-doc noting the graceful-interruption semantics (resumable; disjoint from `RestartInterruptedWorkflowsTask` because `IsExecuting = false`)
- [X] T009 [P] Mirror the new `Interrupted` value in the API Client enum at `src/clients/Elsa.Api.Client/Resources/WorkflowInstances/Enums/WorkflowSubStatus.cs`

### Core models

- [X] T010 [P] Create `QuiescenceState` record (Reason, IsAcceptingNewWork derived, PausedAt, DrainStartedAt, PauseReasonText, PauseRequestedBy, GenerationId) in `src/modules/Elsa.Workflows.Runtime/Models/QuiescenceState.cs`
- [X] T011 [P] Create `ExecutionCycleHandle` internal class (Id, WorkflowInstanceId, IngressSourceName?, StartedAt, internal `CancellationTokenSource`) in `src/modules/Elsa.Workflows.Runtime/Models/ExecutionCycleHandle.cs`
- [X] T012 [P] Create `IngressSourceSnapshot` record (Name, State, LastError?, LastTransitionAt) in `src/modules/Elsa.Workflows.Runtime/Models/IngressSourceSnapshot.cs`
- [X] T013 [P] Create `IngressSourceFinalState` record (Name, State, LastError?, WasForceStopped) in `src/modules/Elsa.Workflows.Runtime/Models/IngressSourceFinalState.cs`
- [X] T014 [P] Create `DrainResult` enum (`CompletedWithinDeadline`, `DeadlineExceeded`, `Forced`, `AbortedByUnhandledException`) in `src/modules/Elsa.Workflows.Runtime/Enums/DrainResult.cs`
- [X] T015 [P] Create `DrainOutcome` record per `data-model.md` §8 in `src/modules/Elsa.Workflows.Runtime/Models/DrainOutcome.cs`
- [X] T016 [P] Create `DrainTrigger` enum (`HostStopSignal`, `ShellDeactivation`, `OperatorForce`) in `src/modules/Elsa.Workflows.Runtime/Enums/DrainTrigger.cs`
- [X] T017 [P] Create `WorkflowInterruptedPayload` record (InterruptedAt, Reason, GenerationId, LastActivityId?, LastActivityNodeId?, IngressSourceName?, BurstDuration) in `src/modules/Elsa.Workflows.Runtime/Models/Payloads/WorkflowInterruptedPayload.cs`

### Core contracts

- [X] T018 [P] Create `IQuiescenceSignal` contract per `contracts/quiescence-signal.md` in `src/modules/Elsa.Workflows.Runtime/Contracts/IQuiescenceSignal.cs`
- [X] T019 [P] Create `IIngressSource` + `IForceStoppable` contracts per `contracts/ingress-source.md` in `src/modules/Elsa.Workflows.Runtime/Contracts/IIngressSource.cs`
- [X] T020 [P] Create `IIngressSourceRegistry` contract (`Snapshot()`, `MarkPauseFailedAsync`) in `src/modules/Elsa.Workflows.Runtime/Contracts/IIngressSourceRegistry.cs`
- [X] T021 [P] Create `IExecutionCycleRegistry` contract (`BeginBurstAsync(instanceId, ingressSourceName?)`, `CompleteBurst(handle)`, `ActiveCount`, `EnumerateActive()`) in `src/modules/Elsa.Workflows.Runtime/Contracts/IExecutionCycleRegistry.cs`
- [X] T022 [P] Create `IngressSourceRegistrationOptions` (with `PauseTimeoutOverride?`) in `src/modules/Elsa.Workflows.Runtime/Options/IngressSourceRegistrationOptions.cs`

### Core service implementations

- [X] T023 [US-foundational] Implement `QuiescenceSignal` thread-safe state-machine (single lock for transitions, volatile read of state reference, `StateChanged` event fires only on effective transitions, reads pause-persistence key in ctor when policy = `AcrossReactivations`, writes on pause, clears on resume; resume while `Drain` flag set is a no-op returning unchanged state) in `src/modules/Elsa.Workflows.Runtime/Services/QuiescenceSignal.cs`
- [X] T024 [US-foundational] Implement `IngressSourceRegistry` (name uniqueness on registration, per-source state tracking via `ConcurrentDictionary`, atomic `MarkPauseFailedAsync`) in `src/modules/Elsa.Workflows.Runtime/Services/IngressSourceRegistry.cs`
- [X] T025 [US-foundational] Implement `ExecutionCycleRegistry` (allocates `ExecutionCycleHandle.Id`, increments counter, attributes to ingress source; on execution cycle start checks `IIngressSourceRegistry` and flips source to `PauseFailed` with reason `"delivered-while-paused"` if the source reports `Paused`) in `src/modules/Elsa.Workflows.Runtime/Services/ExecutionCycleRegistry.cs`

### DI registration & static extension for ingress adapters

- [X] T026 Add `AddIngressSource<TSource>(Action<IngressSourceRegistrationOptions>?)` extension on `IServiceCollection` in `src/modules/Elsa.Workflows.Runtime/Extensions/IngressSourceServiceCollectionExtensions.cs` that registers the source as `IIngressSource` scoped-singleton, reads `IForceStoppable` via type-check, registers options, and queues the registration for `IIngressSourceRegistry` activation-time discovery
- [X] T027 Wire DI registrations for `IQuiescenceSignal` → `QuiescenceSignal`, `IIngressSourceRegistry` → `IngressSourceRegistry`, `IExecutionCycleRegistry` → `ExecutionCycleRegistry` (all singletons) inside `WorkflowRuntimeFeature.ConfigureServices` at `src/modules/Elsa.Workflows.Runtime/ShellFeatures/WorkflowRuntimeFeature.cs`

### Unit tests for foundational primitives

- [X] T028 [P] Unit tests for `QuiescenceSignal` state-machine (drain forward-only, pause idempotent, resume idempotent, resume-during-drain rejected, `StateChanged` fires exactly once per effective transition, composite reason flags) in `test/unit/Elsa.Workflows.Runtime.UnitTests/Quiescence/QuiescenceSignalTests.cs`
- [X] T029 [P] Unit tests for `QuiescenceSignal` pause persistence (`SessionScoped` ignores key-value; `AcrossReactivations` reads key in ctor, writes on pause, clears on resume; uses an in-memory `IKeyValueStore` fake) in `test/unit/Elsa.Workflows.Runtime.UnitTests/Quiescence/QuiescenceSignalPersistenceTests.cs`
- [X] T030 [P] Unit tests for `IngressSourceRegistry` (duplicate-name registration throws; `MarkPauseFailedAsync` is atomic under concurrent calls; snapshot returns consistent state) in `test/unit/Elsa.Workflows.Runtime.UnitTests/Quiescence/IngressSourceRegistryTests.cs`
- [X] T031 [P] Unit tests for `ExecutionCycleRegistry` (active count monotonic per pair of begin/complete calls, attribution recorded, detects `Paused`-but-delivering inconsistency by flipping source to `PauseFailed`) in `test/unit/Elsa.Workflows.Runtime.UnitTests/Quiescence/BurstRegistryTests.cs`

**Checkpoint**: Foundational primitives in place. Runtime still behaves as before — no drain, no pause, no recovery scan.

---

## Phase 3: User Story 1 — Host stop drain (Priority: P1) 🎯 MVP

**Goal**: When the host receives a stop signal, ongoing workflow execution cycles finish at their natural persistence boundary within the drain deadline; breaches are force-cancelled and marked `Interrupted`.

**Independent Test**: Start a long-running workflow; SIGTERM the host; verify no new execution cycles start after SIGTERM, active execution cycle completes, instance is persisted at a clean state. In a second run with `DrainDeadline = 1s`, confirm deadline breach produces an `Interrupted` instance + `WorkflowInterrupted` log entry.

### Contracts & orchestration

- [X] T032 [US1] Create `IDrainOrchestrator` contract with `DrainAsync(DrainTrigger, CancellationToken) : ValueTask<DrainOutcome>` in `src/modules/Elsa.Workflows.Runtime/Contracts/IDrainOrchestrator.cs`
- [X] T033 [US1] Implement `DrainOrchestrator` protocol per `contracts/drain-orchestrator.md` (1. `BeginDrainAsync`; 2. parallel `IIngressSource.PauseAsync` with linked tokens and `IForceStoppable` escalation on timeout, including a fake source WITHOUT `IForceStoppable` left in `PauseFailed` with no escalation; 3. poll `ActiveCount` at 10 ms; 4. on deadline breach, iterate live execution cycles, cancel, persist `SubStatus = Interrupted`, write `WorkflowInterrupted` log via the T038 helper; 5. return `DrainOutcome` — all failures captured, never propagated) in `src/modules/Elsa.Workflows.Runtime/Services/DrainOrchestrator.cs`. **Prerequisite**: T038 (the log helper) MUST be completed before T033 begins.
- [X] T034 [US1] Enforce deadline clamping: effective deadline = `min(GracefulShutdownOptions.DrainDeadline, HostOptions.ShutdownTimeout - 500ms)`; inject `IOptions<HostOptions>` into `DrainOrchestrator` in `src/modules/Elsa.Workflows.Runtime/Services/DrainOrchestrator.cs`
- [X] T035 [US1] Implement `DrainOrchestratorHostedService : IHostedService` that subscribes to `IHostApplicationLifetime.ApplicationStopping` and invokes `IDrainOrchestrator.DrainAsync(DrainTrigger.HostStopSignal, ct)` in `StopAsync`, logging the `DrainOutcome` at `Information` / `Warning` level in `src/modules/Elsa.Workflows.Runtime/HostedServices/DrainOrchestratorHostedService.cs`

### Execution cycle attribution plumbing

- [X] T036 [US1] Add optional `string? IngressSourceName` property to `DispatchWorkflowRequest` (and any symmetric request/response types in the runtime dispatch path) in `src/modules/Elsa.Workflows.Runtime/Messages/` — preserve existing API (property is optional, default null)
- [X] T037 [US1] Wrap the three execution cycle choke points (`IStimulusDispatcher.DispatchAsync`, `IWorkflowRunner.RunAsync`, `IWorkflowRestarter.RestartWorkflowAsync`) with `IExecutionCycleRegistry.BeginBurstAsync` / `CompleteBurst` calls, passing through `IngressSourceName` where present. Files touched: `src/modules/Elsa.Workflows.Runtime/Services/BackgroundStimulusDispatcher.cs`, plus the entry-point files identified by `grep -rn "IWorkflowRunner\|IWorkflowRestarter" src/modules/Elsa.Workflows.Runtime/Services/ | head`

### Execution-log integration

- [X] T038 [US1] Add `LogWorkflowInterruptedAsync(this IWorkflowExecutionLogStore store, WorkflowExecutionContext ctx, WorkflowInterruptedPayload payload)` helper in `src/modules/Elsa.Workflows.Runtime/Extensions/LogExtensions.cs` that writes a `WorkflowExecutionLogRecord` with `EventName = "WorkflowInterrupted"` and the typed payload serialised through the existing payload path

### Hosted-service registration order

- [X] T039 [US1] Register `DrainOrchestratorHostedService` in `WorkflowRuntimeFeature.ConfigureServices` AFTER `Elsa.Hosting.Management`'s `InstanceHeartbeatService` so that `IHostedService.StopAsync` runs drain first, heartbeat last (R5). Add an XML-doc comment on the registration explaining the ordering invariant at `src/modules/Elsa.Workflows.Runtime/ShellFeatures/WorkflowRuntimeFeature.cs`

### Tests

- [X] T040 [P] [US1] Unit tests for `DrainOrchestrator` parallel pause (succeeds fast when all sources return; fails one source without blocking the others; invokes `IForceStoppable.ForceStopAsync` on timeout) using fake `IIngressSource` implementations in `test/unit/Elsa.Workflows.Runtime.UnitTests/Quiescence/DrainOrchestratorPauseTests.cs`
- [X] T041 [P] [US1] Unit tests for `DrainOrchestrator` wait-for-execution cycles (returns `CompletedWithinDeadline` when execution cycles finish, `DeadlineExceeded` + cancel + persist + log when not; `AbortedByUnhandledException` swallowed into outcome) in `test/unit/Elsa.Workflows.Runtime.UnitTests/Quiescence/DrainOrchestratorWaitTests.cs`
- [X] T042 [P] [US1] Integration test covering US1 scenarios 1 & 2 (host stop completes within deadline; ingress sources pause before wait phase starts). Use `[Theory]` with ≥ 3 representative workflow shapes (single-activity execution cycle, composite-activity execution cycle, tight-loop execution cycle bounded by deadline) and assert 100% of execution cycles that CAN complete within the deadline DO complete — this is the SC-001 verification. File: `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/HostStopDrainTests.cs`
- [X] T043 [P] [US1] Integration test covering US1 scenario 3 + persistence-failure edge case. `[Theory]` with two cases: (a) clean deadline breach → `Interrupted` sub-status + `WorkflowInterrupted` log entry with `Reason = "DeadlineBreach"`; (b) persistence layer fails during drain (use a store decorator that throws on `SaveAsync`) → in-memory `Interrupted` marking + `WorkflowInterruptedPayload.Reason = "PersistenceFailure"` surfaced in the drain outcome. File: `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/DeadlineBreachTests.cs`
- [X] T044 [P] [US1] Integration test covering US1 scenario 4 (failing ingress source reported in `DrainOutcome.Sources` with `PauseFailed`; drain completes within deadline regardless) in `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/FailingIngressSourceTests.cs`
- [X] T045 [P] [US1] Unit test for execution cycle attribution inconsistency detection (FR-018 / SC-009): a fake source reports `Paused` but starts a execution cycle; registry flips the source to `PauseFailed` within one execution cycle in `test/unit/Elsa.Workflows.Runtime.UnitTests/Quiescence/BurstAttributionInconsistencyTests.cs`

**Checkpoint**: US1 fully functional. Host stop produces clean drains; deadline breaches produce `Interrupted` instances. No admin API yet; no automatic recovery on next start yet.

---

## Phase 4: User Story 2 — Administrative pause / resume (Priority: P2)

**Goal**: Authenticated operators can pause, resume, force, and query runtime quiescence at runtime without stopping the host.

**Independent Test**: `POST /admin/workflow-runtime/pause` while a workflow waits on a stimulus; send the stimulus; verify the execution log shows the stimulus buffered and not dispatched; `POST .../resume`; verify dispatch proceeds.

### Endpoint DTOs and four endpoints

- [X] T046 [P] [US2] Create request/response DTOs (`PauseRequest`, `ResumeRequest`, `ForceRequest`, `StatusResponse`, `IngressSourceStateDto`, `ForceResponse`) per `contracts/admin-endpoints.md` in `src/modules/Elsa.Workflows.Runtime/Endpoints/Admin/Models.cs`
- [X] T047 [P] [US2] Implement `POST /admin/workflow-runtime/pause` as `ElsaEndpoint<PauseRequest, StatusResponse>` with `ConfigurePermissions(PermissionNames.ManageWorkflowRuntime)`; delegates to `IQuiescenceSignal.PauseAsync`; captures `User.Identity?.Name` as `requestedBy`; returns composite status in `src/modules/Elsa.Workflows.Runtime/Endpoints/Admin/Pause/Endpoint.cs`
- [X] T048 [P] [US2] Implement `POST /admin/workflow-runtime/resume` as `ElsaEndpoint<ResumeRequest, StatusResponse>`; returns 409 with `{ code: "runtime-draining", state }` if `Drain` flag set; delegates to `IQuiescenceSignal.ResumeAsync` otherwise in `src/modules/Elsa.Workflows.Runtime/Endpoints/Admin/Resume/Endpoint.cs`
- [X] T049 [P] [US2] Implement `GET /admin/workflow-runtime/status` as `ElsaEndpointWithoutRequest<StatusResponse>`; readable during drain in `src/modules/Elsa.Workflows.Runtime/Endpoints/Admin/Status/Endpoint.cs`
- [X] T050 [P] [US2] Implement `POST /admin/workflow-runtime/force` as `ElsaEndpoint<ForceRequest, ForceResponse>`; invokes `IDrainOrchestrator.DrainAsync(DrainTrigger.OperatorForce, ct)` with zero-deadline; returns 409 if a drain is already in progress via a different trigger in `src/modules/Elsa.Workflows.Runtime/Endpoints/Admin/Force/Endpoint.cs`

### Audit events

- [X] T051 [P] [US2] Create mediator notifications `RuntimePauseRequested`, `RuntimeResumeRequested`, `RuntimeForceRequested` in `src/modules/Elsa.Workflows.Runtime/Notifications/RuntimeLifecycleNotifications.cs`
- [X] T052 [US2] Publish audit notifications from the four endpoints only on effective state changes (skip on idempotent no-ops per SC-007) — edit the four endpoint files from T047–T050

### Pause persistence + readiness + back-pressure

- [ ] T053 [P] [US2] Implement `BackpressureAwareBookmarkQueue` decorator that wraps the existing `StoreBookmarkQueue` and throws `StimulusQueueOverflowException` when `depth > GracefulShutdownOptions.StimulusQueueMaxDepthWhilePaused` AND policy = `Reject` (registered conditionally). Files: `src/modules/Elsa.Workflows.Runtime/Services/BackpressureAwareBookmarkQueue.cs`, `src/modules/Elsa.Workflows.Runtime/Exceptions/StimulusQueueOverflowException.cs`
- [ ] T054 [P] [US2] Implement `GracefulShutdownHealthCheck : IHealthCheck` that returns `Degraded` when paused AND queue depth above threshold, `Unhealthy` on overflow-reject policy breaches, `Healthy` otherwise; registered under a fixed name `"elsa-runtime"` in `src/modules/Elsa.Workflows.Runtime/HealthChecks/GracefulShutdownHealthCheck.cs`
- [ ] T055 [US2] Conditional DI registration in `WorkflowRuntimeFeature.ConfigureServices`: when `OverflowPolicy = Reject`, swap `IBookmarkQueue` to the decorator; when `PausePersistence = AcrossReactivations`, take a dependency on `IKeyValueStore` for `QuiescenceSignal` ctor — edit `src/modules/Elsa.Workflows.Runtime/ShellFeatures/WorkflowRuntimeFeature.cs`

### Internal worker pause compliance

- [X] T056 [US2] Make `BookmarkQueueWorker` (in-process) consult `IQuiescenceSignal` at the top of each dequeue iteration and skip dispatch when drain OR pause is active. Register the worker itself as an `IIngressSource` named `"internal.bookmark-queue-worker"`. Edit `src/modules/Elsa.Workflows.Runtime/Services/BookmarkQueueWorker.cs`

### Tests

- [X] T057 [P] [US2] Integration test US2 scenarios 1 & 2 plus SC-007 audit-entry invariant (pause propagates to all sources; resume restores operation and drains buffered stimuli in order; calling pause twice and resume twice produces exactly one `RuntimePauseRequested` and one `RuntimeResumeRequested` notification — no additional audit entries for idempotent no-ops) in `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/AdminPauseResumeTests.cs`
- [X] T058 [P] [US2] Integration test US2 scenario 3 (unauthorised caller rejected when `EndpointSecurityOptions.SecurityIsEnabled = true`) in `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/AdminAuthTests.cs`
- [ ] T059 [P] [US2] Integration test US2 scenario 4 (status response shape matches `contracts/admin-endpoints.md`; freezes `StatusResponse` shape) in `test/component/Elsa.Workflows.ComponentTests/GracefulShutdown/AdminStatusContractTests.cs`
- [X] T060 [P] [US2] Integration test US2 scenario 6 (pause during drain: drain completes; resume is rejected until the drain-in-progress flag naturally resolves — which, under host-stop drain, never does within the generation) in `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/PauseDuringDrainTests.cs`
- [ ] T061 [P] [US2] Integration test US2 scenario 7 (pause persistence across reactivation: with `PausePersistence = AcrossReactivations` and a fake shell reactivation, new generation starts `Paused`) in `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/PausePersistenceTests.cs`
- [ ] T062 [P] [US2] Unit tests for the back-pressure decorator + overflow exception contract in `test/unit/Elsa.Workflows.Runtime.UnitTests/Quiescence/BackpressureBookmarkQueueTests.cs`

**Checkpoint**: US1 AND US2 both work. Operators can control quiescence live. Drain still functions correctly when triggered by host stop.

---

## Phase 5: User Story 3 — Interrupted recovery on shell activation (Priority: P3)

**Goal**: After a drain with interruptions, affected instances requeue automatically on next shell activation, bypassing the periodic timeout-based crash-recovery cadence. The existing `RestartInterruptedWorkflowsTask` stays unchanged.

**Independent Test**: Force a 1-second drain deadline breach; verify `Interrupted` instance present; restart the host; verify the instance is requeued within the activation path, observable before the `RestartInterruptedWorkflowsTask` recurring cadence next fires.

- [X] T063 [US3] Create `IInterruptedRecoveryScanner` contract with `ScanAndRequeueAsync(CancellationToken) : ValueTask<int>` (returns count requeued) in `src/modules/Elsa.Workflows.Runtime/Contracts/IInterruptedRecoveryScanner.cs`
- [X] T064 [US3] Implement `RecoverInterruptedWorkflowsStartupTask : IStartupTask` that calls `IWorkflowInstanceStore.EnumerateSummariesAsync` with filter `{ SubStatus = WorkflowSubStatus.Interrupted }` (scoped to the container's tenant context), requeues each via `IWorkflowRestarter.RestartWorkflowAsync`, logs the count, continues on per-instance failure. File: `src/modules/Elsa.Workflows.Runtime/StartupTasks/RecoverInterruptedWorkflowsStartupTask.cs`
- [X] T065 [US3] Register `RecoverInterruptedWorkflowsStartupTask` as a startup task in `WorkflowRuntimeFeature.ConfigureServices`, alongside (but separate from) the existing `RestartInterruptedWorkflowsTask` recurring task. File: `src/modules/Elsa.Workflows.Runtime/ShellFeatures/WorkflowRuntimeFeature.cs`
<!-- T066 removed after analysis: WorkflowInstanceFilter.WorkflowSubStatus and WorkflowInstanceFilter.WorkflowSubStatuses already exist at src/modules/Elsa.Workflows.Management/Filters/WorkflowInstanceFilter.cs with EF translation wired. No work required. -->

### Tests

- [X] T067 [P] [US3] Unit test for `RecoverInterruptedWorkflowsStartupTask` (enumerates only `Interrupted` sub-status, calls `IWorkflowRestarter.RestartWorkflowAsync` once per instance, swallows per-instance failures) using a fake `IWorkflowInstanceStore` and `IWorkflowRestarter` in `test/unit/Elsa.Workflows.Runtime.UnitTests/Recovery/RecoverInterruptedWorkflowsStartupTaskTests.cs`
- [X] T068 [P] [US3] Integration test US3 scenarios 1 & 2 (drain deadline breach → `Interrupted` persisted with log entry → next shell activation requeues before `RestartInterruptedWorkflowsTask` cadence fires). Use `[Theory]` with ≥ 3 representative interruption batch sizes (1 instance, 10 instances, 100 instances) and assert 100% of `Interrupted` instances are requeued — this is the SC-003 verification. File: `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/InterruptedRecoveryTests.cs`
- [X] T069 [P] [US3] Integration test US3 scenario 3 (simulated ungraceful crash → `IsExecuting=true` stale instance recovered by existing `RestartInterruptedWorkflowsTask`, NOT by the new startup task; verifies filter disjointness) in `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/UngracefulCrashRecoveryTests.cs`
- [X] T070 [P] [US3] Component test freezing the `WorkflowInterruptedPayload` JSON shape so that historical log entries remain parseable after payload evolution in `test/component/Elsa.Workflows.ComponentTests/GracefulShutdown/WorkflowInterruptedPayloadContractTests.cs`
- [X] T071 [P] [US3] Integration test US3 scenario 5 (dashboard-filter semantics): query the execution log and instance store with `SubStatus = Interrupted` and verify only graceful-interruption instances are returned (no cancelled, faulted, or ungracefully-crashed instances) in `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/InterruptedSubStatusFilterTests.cs`

**Checkpoint**: All three user stories functional. Drain, pause, and recovery work end-to-end.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: First-party ingress adapters (so the feature delivers production value beyond the internal worker), multi-shell isolation, and docs.

### First-party ingress-source adapters

- [X] T072 [P] Implement `HttpTriggerIngressSource : IIngressSource` that pauses/resumes `HttpWorkflowsMiddleware` dispatch (pause causes middleware to short-circuit to a 503 Service Unavailable response with a `Retry-After` header; resume restores normal pass-through). Register via `AddIngressSource` in `HttpFeature.ConfigureServices`. Files: `src/modules/Elsa.Http/IngressSources/HttpTriggerIngressSource.cs`, edit `src/modules/Elsa.Http/Middleware/HttpWorkflowsMiddleware.cs`, edit `src/modules/Elsa.Http/Features/HttpFeature.cs`
- [X] T073 [P] Implement `ScheduledTriggerIngressSource : IIngressSource` that pauses/resumes the scheduler tick loop in `Elsa.Scheduling`. Register via `AddIngressSource` in the Scheduling feature. Files: `src/modules/Elsa.Scheduling/IngressSources/ScheduledTriggerIngressSource.cs`, edit the scheduler's hosted-service/tick class (grep for `IHostedService` in `src/modules/Elsa.Scheduling/`)
- [X] T074 [P] Make `DistributedBookmarkQueueWorker` consult `IQuiescenceSignal` at the top of each dequeue iteration (mirrors T056); register it as an `IIngressSource` named `"internal.distributed-bookmark-queue-worker"` in `src/modules/Elsa.Workflows.Runtime.Distributed/Services/DistributedBookmarkQueueWorker.cs`
- [X] T075 [P] Integration test covering US1 scenario 5 + US2 scenario 5 (multi-shell isolation: pausing one shell does not affect another shell's active-execution cycle throughput; draining one shell does not drain another) — uses `Elsa.Shells.Api` reload to simulate shell boundaries in `test/integration/Elsa.Workflows.IntegrationTests/GracefulShutdown/MultiShellIsolationTests.cs`

### Cross-cutting

- [X] T076 Run the three manual verification scenarios from `quickstart.md` against a local build; capture logs in a scratch note attached to the PR description (do NOT commit the log note)
- [X] T077 Update module-author documentation: add a short "Registering an ingress source" section to `src/modules/Elsa.Workflows.Runtime/README.md` (or the equivalent docs file referenced from CONTRIBUTING) linking to `quickstart.md`. Only edit if an existing README/docs location is present — do NOT create a new README if one is not already there
- [X] T078 Verify SC-008 (no regression in timeout-based recovery) by running the existing `RestartInterruptedWorkflowsTask` integration tests in isolation; document no-regression in the PR description for the US3 PR

**Final Checkpoint**: Feature complete end-to-end. First-party adapters in place so the default `Elsa.ModularServer.Web` deployment exercises the full drain protocol on shutdown.

---

## Dependencies & Execution Order

### Phase dependencies

- **Phase 1 (Setup)**: No dependencies. Start immediately.
- **Phase 2 (Foundational)**: Depends on Phase 1. Blocks every user-story phase.
- **Phase 3 (US1)**: Depends on Phase 2.
- **Phase 4 (US2)**: Depends on Phase 2. Can start in parallel with Phase 3 once Phase 2 is done.
- **Phase 5 (US3)**: Depends on Phase 2 AND on Phase 3 (reuses the `Interrupted` persistence path written by the drain orchestrator in T033). Cannot fully start until T033 lands; unit-test stubs against fake stores can start earlier.
- **Phase 6 (Polish)**: Depends on Phase 3 (for the attribution plumbing the adapters populate). T075 multi-shell test depends on Phase 4. T074 depends on T056.

### Within each phase

- Models/contracts (T010–T022) before implementations (T023–T025).
- Implementations before unit tests that exercise them (T028–T031).
- In US1: contract (T032) → **log helper (T038, prerequisite of T033)** → orchestrator (T033–T034) → hosted service (T035) → attribution plumbing (T036–T037) → registration (T039) → tests (T040–T045). T038 is deliberately implemented ahead of its numeric position because T033's "write `WorkflowInterrupted` log" step calls the helper.
- In US2: DTOs (T046) before endpoints (T047–T050); endpoints before audit wiring (T052).
- In US3: contract (T063) → implementation (T064) → registration (T065) → tests. (T066 was removed during analysis — the required filter already exists.)

### PR slicing (aligns with `plan.md` Phase 2 Handoff)

| PR | Phases | Concern |
|----|--------|---------|
| 1 | Phases 1 + 2 | Core quiescence machinery, no behaviour change |
| 2 | Phase 3 | Drain orchestrator + host-stop integration (US1 / MVP) |
| 3 | Phase 5 | `Interrupted` recovery on activation (US3) |
| 4 | Phase 4 | Admin endpoints + pause persistence + back-pressure (US2) |
| 5 | Phase 6 | First-party ingress adapters + multi-shell test + docs |

Each PR is a single concern per Constitution VI.

---

## Parallel Opportunities

### Inside Phase 1 (Setup)
- T001, T002, T003 — three options files, independent → run in parallel.

### Inside Phase 2 (Foundational)
- **Batch A** (all [P] enums and models): T006, T007, T010, T011, T012, T013, T014, T015, T016, T017.
- **Batch B** (all [P] contracts): T018, T019, T020, T021, T022.
- **Batch C** (implementations): T023, T024, T025 each touch a different file; can run in parallel AFTER Batches A+B.
- **Batch D** (unit tests): T028, T029, T030, T031 run in parallel after Batch C.
- T008 and T009 are independent — can run in parallel with Batch A.

### Inside Phase 3 (US1)
- T040, T041, T042, T043, T044, T045 all in separate test files → parallel after T033–T039.

### Inside Phase 4 (US2)
- T047, T048, T049, T050 each live in their own endpoint folder → parallel after T046.
- T053, T054 parallel with each other.
- T057–T062 all independent test files → parallel after implementation.

### Inside Phase 5 (US3)
- T067, T068, T069, T070, T071 independent test files → parallel after T063–T066.

### Inside Phase 6
- T072, T073, T074 are in different modules → parallel.

### Between phases
- Phase 3 and Phase 4 can run in parallel by different developers once Phase 2 completes (per plan.md PR slicing).

---

## Parallel Example: Phase 2 batch kickoff

```text
# After Phase 1 is done, kick off the foundational enums and models in one wave:
Task: T006 Create QuiescenceReason enum
Task: T007 Create IngressSourceState enum
Task: T010 Create QuiescenceState record
Task: T011 Create ExecutionCycleHandle class
Task: T012 Create IngressSourceSnapshot record
Task: T013 Create IngressSourceFinalState record
Task: T014 Create DrainResult enum
Task: T015 Create DrainOutcome record
Task: T016 Create DrainTrigger enum
Task: T017 Create WorkflowInterruptedPayload record
Task: T008 Add Interrupted to WorkflowSubStatus
Task: T009 Mirror Interrupted in API Client enum
```

Then a second wave for contracts:

```text
Task: T018 IQuiescenceSignal
Task: T019 IIngressSource + IForceStoppable
Task: T020 IIngressSourceRegistry
Task: T021 IExecutionCycleRegistry
Task: T022 IngressSourceRegistrationOptions
```

---

## Implementation Strategy

### MVP first (US1 only)

1. Finish Phase 1 (Setup).
2. Finish Phase 2 (Foundational).
3. Finish Phase 3 (US1 host-stop drain).
4. **STOP** and validate US1's Independent Test manually from quickstart.md.
5. Ship PR 1 + PR 2 as the MVP. The runtime now drains gracefully on shutdown and marks breached execution cycles `Interrupted` — even without the recovery scan, the timeout-based `RestartInterruptedWorkflowsTask` will eventually recover them (regression-free per SC-008).

### Incremental delivery

6. Add Phase 5 (US3) → PR 3. Now interrupted workflows recover immediately on next start.
7. Add Phase 4 (US2) → PR 4. Operators can pause/resume live.
8. Add Phase 6 (Polish) → PR 5. First-party adapters + multi-shell test.

### Parallel team strategy

Once Phase 2 lands:
- Developer A → Phase 3 (US1).
- Developer B → Phase 4 (US2) stubs against the foundational contracts.
- Developer C → Phase 5 (US3) unit-test stubs against fake stores.

Integration tests for US2 and US3 must wait for Phase 3's orchestrator to land, but unit-level work proceeds in parallel.

---

## Notes

- `[P]` tasks touch different files and have no incomplete dependencies.
- `[Story]` label is `[US-foundational]` for phase 2 implementation tasks that are not tied to one user story, and `[US1] | [US2] | [US3]` otherwise per template.
- Constitution V: every new production file has at least one associated unit test; integration tests exercise the Independent Test criterion of each user story.
- Commit after each task or each tight logical group. Avoid mixing concerns per Constitution VI.
- The existing `RestartInterruptedWorkflowsTask` is **intentionally untouched** throughout this task list. A rename for clarity (spec.md naming vs. existing class name) is a follow-up PR outside this feature's scope.
- Analysis (2026-04-24) confirmed `WorkflowInstanceFilter.WorkflowSubStatus[es]` at `src/modules/Elsa.Workflows.Management/Filters/WorkflowInstanceFilter.cs` already supports the filter US3 requires — no extension task needed.
