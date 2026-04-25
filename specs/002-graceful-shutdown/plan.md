# Implementation Plan: Graceful Shutdown for the Workflow Runtime

**Branch**: `002-graceful-shutdown` | **Date**: 2026-04-24 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-graceful-shutdown/spec.md`

## Summary

Introduce a container-scoped **Quiescence Signal** inside the workflow runtime, an **Ingress Source** contract that every component feeding external events into the engine implements, and a **Drain Orchestrator** that, on host stop or shell deactivation, pauses all ingress sources, waits for active bursts to reach their natural persistence boundary within a configured deadline, and marks any bursts that breach the deadline with a new `Interrupted` sub-status. On next host start or shell activation, a scoped startup scan immediately requeues `Interrupted` instances, bypassing the existing timeout-based `RestartInterruptedWorkflowsTask` (which continues unchanged as a safety net for ungraceful crashes). An authenticated admin surface exposes pause/resume/status over the same quiescence machinery. The mechanism plugs into the existing `IShellFeature` lifecycle and the existing `Elsa.Hosting.Management` heartbeat so that graceful drain never false-positives crash recovery.

## Technical Context

**Language/Version**: C# latest (`<LangVersion>latest</LangVersion>`), nullable reference types enabled, implicit usings enabled — per `src/Directory.Build.props`.
**Primary Dependencies**: `Elsa.Workflows.Runtime`, `Elsa.Workflows.Runtime.Distributed`, `Elsa.Hosting.Management` (existing heartbeat), `Elsa.Http` and `Elsa.Scheduling` (first ingress-source adapters), `Elsa.Api.Common` (`ElsaEndpoint<TRequest, TResponse>` on FastEndpoints), `Elsa.Features` (`IShellFeature`), `Microsoft.Extensions.DependencyInjection`, `Elsa.Mediator`.
**Storage**: EF Core per-provider (`Elsa.Persistence.EFCore.SqlServer`, `.PostgreSql`, `.MySql`, `.Sqlite`, `.Oracle`). Two persistence touchpoints: (1) the existing `WorkflowInstance` row (new `Interrupted` value for the `SubStatus` column — an enum already persisted as an integer, so no schema migration is strictly required, but each provider migration project MUST be verified); (2) the existing `WorkflowExecutionLogRecord` entity (new `WorkflowInterrupted` `EventName` with typed payload — no schema change).
**Testing**: xUnit. Unit tests per the constitution's `test/unit/` + `test/integration/` layout. Reuse `ActivityTestFixture` and `WorkflowTestFixture`. New fixtures for the quiescence state machine, fake ingress sources, and drain harness. No external services in unit tests.
**Target Platform**: Multi-target `.NET 8.0`, `.NET 9.0`, `.NET 10.0` (primary) via `src/Directory.Build.props`. Runs in any ASP.NET Core / hosted-service process; both single-runtime hosts and multi-shell hosts (`Elsa.Shells.Api`).
**Project Type**: Modular .NET library — the 100+ project solution already described by the constitution. This feature adds no new modules; all changes land in existing modules.
**Performance Goals**: Drain MUST complete within a configured deadline (default researched in Phase 0: 30 s). Pause must propagate to every registered ingress source in parallel, bounded by each source's own timeout (default researched in Phase 0: 5 s). Shell-activation recovery of `Interrupted` instances MUST requeue 100% of affected instances before the shell begins accepting new work — target < 2 s for ≤ 1 000 interrupted instances per shell.
**Constraints**: (1) Host process MUST NOT exit before either drain completes or the drain deadline elapses. (2) The existing `RestartInterruptedWorkflowsTask` (timeout-based crash recovery) MUST continue to operate unchanged; no regression in its behaviour or latency (SC-008). (3) The existing `InstanceHeartbeatService` MUST continue emitting heartbeats throughout drain (FR-029). (4) In distributed deployments, drain is node-local; no cross-node burst handoff. (5) Pause, resume, force-stop, and status actions MUST be idempotent (SC-007). (6) No new storage entity is introduced (clarification decision 6).
**Scale/Scope**: Applies to every deployment of `Elsa.Workflows.Runtime`. New code surface: 1 core contract group (`IQuiescenceSignal`, `IIngressSource`, `IDrainOrchestrator`, admin endpoints), 1 new enum value, 1 new execution-log event name, 2 first-party ingress-source adapters (HTTP, Scheduling), and a shell-activation startup scan. Estimate: 25–35 new/modified source files; all confined to existing modules.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Evaluated against `.specify/memory/constitution.md` v1.0.0:

| Principle | Verdict | Evidence |
|-----------|---------|----------|
| **I. Modular Architecture** | PASS | All new services, contracts, and endpoints land inside existing modules (`Elsa.Workflows.Runtime`, `Elsa.Http`, `Elsa.Scheduling`, `Elsa.Workflows.Runtime.Distributed`). No new module is introduced. Cross-module communication uses published contracts (`IIngressSource`, `IQuiescenceSignal`), not internal types. |
| **II. Composition & Extensibility** | PASS | `IIngressSource` is the extensibility point — first-party HTTP/Scheduler adapters and any third-party module can register. Pause timeout is overridable at registration. The `IForceStoppable` capability is opt-in per source. DI registration uses fluent `IServiceCollection` extensions. |
| **III. Convention-Driven Design** | PASS | Admin endpoints use `ElsaEndpoint<TRequest, TResponse>` with `ConfigurePermissions`. Stores remain unchanged (no new `I*Store`). State-machine enums follow existing naming. The execution-log event name `WorkflowInterrupted` mirrors existing event-name conventions in `LogExtensions`. |
| **IV. Async & Pipeline Execution** | PASS | Drain, pause, resume, and activation-scan paths are fully async with `ValueTask` on hot paths where sync completion dominates. Activity cancellation re-uses the existing `ActivityExecutionContext.CancellationToken`. The drain orchestrator uses the existing mediator for `WorkflowInterrupted` notifications. |
| **V. Testing Discipline** | PASS | Unit tests for the quiescence state machine, ingress state machine, and drain deadline logic. Integration tests for end-to-end drain, pause/resume, and shell-activation recovery using `WorkflowTestFixture` with a fake ingress source. No network-dependent tests. |
| **VI. Trunk-Based Development** | PASS | The plan separates into ~5 focused PRs (see Phase 2 handoff at the end of this file). Each PR has a single concern. API surface changes include doc updates. |
| **VII. Simplicity & Focus** | PASS | No new module; no speculative extensibility; the quiescence signal is a single service, not a parallel lifecycle framework. Existing crash recovery is preserved verbatim rather than generalised. No feature flags for hypothetical deployment modes. |

**Result**: All gates pass on the initial check. No entries required in the Complexity Tracking table.

## Project Structure

### Documentation (this feature)

```text
specs/002-graceful-shutdown/
├── plan.md                     # This file
├── research.md                 # Phase 0 output
├── data-model.md               # Phase 1 output
├── quickstart.md               # Phase 1 output
├── contracts/                  # Phase 1 output
│   ├── ingress-source.md
│   ├── quiescence-signal.md
│   ├── drain-orchestrator.md
│   └── admin-endpoints.md
├── checklists/
│   └── requirements.md         # From /speckit.specify
└── tasks.md                    # Phase 2 output (produced by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── modules/
│   ├── Elsa.Workflows.Runtime/
│   │   ├── Contracts/
│   │   │   ├── IQuiescenceSignal.cs               # NEW — container-scoped signal + reasons
│   │   │   ├── IIngressSource.cs                  # NEW — pause/resume/state-query contract
│   │   │   ├── IIngressSourceRegistry.cs          # NEW — registration + enumeration
│   │   │   ├── IForceStoppable.cs                 # NEW — optional escalation capability
│   │   │   ├── IDrainOrchestrator.cs              # NEW — runs the drain protocol
│   │   │   ├── IBurstRegistry.cs                  # NEW — active-burst accounting
│   │   │   └── IInterruptedRecoveryScan.cs        # NEW — activation-time scan
│   │   ├── Services/
│   │   │   ├── QuiescenceSignal.cs                # NEW — thread-safe state-machine
│   │   │   ├── IngressSourceRegistry.cs           # NEW
│   │   │   ├── DrainOrchestrator.cs               # NEW
│   │   │   ├── BurstRegistry.cs                   # NEW — wraps existing dispatcher entry points
│   │   │   └── InterruptedRecoveryScan.cs         # NEW
│   │   ├── HostedServices/
│   │   │   └── DrainOrchestratorHostedService.cs  # NEW — hooks IHostApplicationLifetime.StopRequested
│   │   ├── Tasks/
│   │   │   ├── RestartInterruptedWorkflowsTask.cs # UNCHANGED — timeout-based crash recovery (FR-022)
│   │   │   └── (no new recurring task — activation scan is a startup task)
│   │   ├── StartupTasks/
│   │   │   └── RecoverInterruptedWorkflowsStartupTask.cs  # NEW — runs once per shell activation
│   │   ├── Extensions/
│   │   │   └── LogExtensions.cs                   # EDIT — add WorkflowInterrupted helper
│   │   ├── Enums/
│   │   │   └── IngressSourceState.cs              # NEW
│   │   ├── Models/
│   │   │   ├── QuiescenceState.cs                 # NEW — composite signal type
│   │   │   ├── QuiescenceReason.cs                # NEW — flags enum
│   │   │   ├── BurstHandle.cs                     # NEW
│   │   │   ├── DrainOutcome.cs                    # NEW
│   │   │   └── Payloads/
│   │   │       └── WorkflowInterruptedPayload.cs  # NEW — typed log-event payload
│   │   ├── Options/
│   │   │   └── GracefulShutdownOptions.cs         # NEW — deadlines, timeouts, pause-persistence policy
│   │   ├── Endpoints/
│   │   │   └── Admin/
│   │   │       ├── Pause/Endpoint.cs              # NEW — ElsaEndpoint<Request, Response>
│   │   │       ├── Resume/Endpoint.cs             # NEW
│   │   │       ├── Status/Endpoint.cs             # NEW
│   │   │       ├── Force/Endpoint.cs              # NEW — operator-force drain
│   │   │       └── Models.cs                      # NEW — request/response DTOs
│   │   └── ShellFeatures/
│   │       └── WorkflowRuntimeFeature.cs          # EDIT — register quiescence + drain + startup scan
│   ├── Elsa.Workflows.Runtime.Distributed/
│   │   └── Services/
│   │       └── DistributedBookmarkQueueWorker.cs  # EDIT — honour IQuiescenceSignal on dequeue
│   ├── Elsa.Workflows.Core/
│   │   └── Enums/
│   │       └── WorkflowSubStatus.cs               # EDIT — add Interrupted
│   ├── Elsa.Http/
│   │   └── IngressSources/
│   │       └── HttpTriggerIngressSource.cs        # NEW — wraps HttpWorkflowsMiddleware dispatch
│   ├── Elsa.Scheduling/
│   │   └── IngressSources/
│   │       └── ScheduledTriggerIngressSource.cs   # NEW — wraps the scheduler tick loop
│   └── Elsa.Persistence.EFCore.{SqlServer,PostgreSql,MySql,Sqlite,Oracle}/
│       └── Migrations/                            # NO-OP unless a persistence review requires one
├── clients/
│   └── Elsa.Api.Client/
│       └── Resources/WorkflowInstances/Enums/
│           └── WorkflowSubStatus.cs               # EDIT — mirror Interrupted on the API client
└── common/
    └── Elsa.Features/
        └── Contracts/
            └── IShellFeature.cs                   # EDIT (OPTIONAL, see research R3) — add async DeactivateAsync

test/
├── unit/
│   └── Elsa.Workflows.Runtime.UnitTests/
│       ├── Quiescence/
│       │   ├── QuiescenceSignalTests.cs          # NEW
│       │   ├── IngressSourceStateMachineTests.cs # NEW
│       │   └── DrainOrchestratorTests.cs         # NEW — fake ingress sources + fake bursts
│       └── Recovery/
│           └── RecoverInterruptedWorkflowsStartupTaskTests.cs  # NEW
├── integration/
│   └── Elsa.Workflows.IntegrationTests/
│       ├── GracefulShutdown/
│       │   ├── HostStopDrainTests.cs             # US1
│       │   ├── AdminPauseResumeTests.cs          # US2
│       │   ├── InterruptedRecoveryTests.cs       # US3
│       │   └── MultiShellIsolationTests.cs       # US1#5, US2#5
└── component/
    └── Elsa.Workflows.ComponentTests/
        └── DrainOutcomeContractTests.cs          # NEW — contract shape freezes
```

**Structure Decision**: Single modular .NET solution (the existing `src/modules/**` layout). All new code is scoped to existing modules per Constitution I & VII. `Elsa.Workflows.Runtime` is the home of the quiescence machinery, admin endpoints, and activation-time recovery scan. `Elsa.Http` and `Elsa.Scheduling` each gain one adapter file that implements `IIngressSource` against their existing middleware / scheduler loop. `Elsa.Persistence.EFCore.*` providers only change if research flags a schema touch — default expectation is no migrations, since `WorkflowSubStatus` is already persisted as an integer. The optional `IShellFeature.DeactivateAsync` contract change under `src/common/Elsa.Features/` is called out as a research decision (R3); the fallback is to drain from an `IHostedService.StopAsync` without touching `IShellFeature`.

## Phase 0 Output

See [research.md](./research.md) — resolves the ten research items below:

- **R1** Pause-timeout default
- **R2** Drain-deadline default and configuration source
- **R3** Shell deactivation hook: extend `IShellFeature` vs. pure `IHostedService.StopAsync`
- **R4** Distinguishing the new `Interrupted` sub-status from the existing `RestartInterruptedWorkflowsTask` (naming & filter)
- **R5** Interaction of the existing `InstanceHeartbeatService` with drain
- **R6** Stimulus-queue back-pressure policy representation
- **R7** Per-burst ingress attribution plumbing
- **R8** Pause persistence across shell reactivation (storage location)
- **R9** Authorization permission name for admin endpoints
- **R10** Distributed runtime worker integration (`BookmarkQueueWorker`, `BackgroundStimulusDispatcher`)

All `NEEDS CLARIFICATION` items from Technical Context are resolved in `research.md`. **Result: zero unresolved research items blocking Phase 1.**

## Phase 1 Output

- [data-model.md](./data-model.md) — entities, state machines, invariants.
- [contracts/ingress-source.md](./contracts/ingress-source.md) — `IIngressSource` + `IForceStoppable`.
- [contracts/quiescence-signal.md](./contracts/quiescence-signal.md) — `IQuiescenceSignal`, composite state, reason flags.
- [contracts/drain-orchestrator.md](./contracts/drain-orchestrator.md) — drain protocol, deadline model, `DrainOutcome`.
- [contracts/admin-endpoints.md](./contracts/admin-endpoints.md) — pause / resume / status / force REST surface.
- [quickstart.md](./quickstart.md) — developer-facing walkthrough to register a new ingress source, observe drain, and exercise pause.

## Post-Design Constitution Re-Check

Re-evaluated against v1.0.0 after Phase 1 artifacts exist:

| Principle | Verdict | Post-design evidence |
|-----------|---------|----------------------|
| I. Modular Architecture | PASS | The four contract files land under `Elsa.Workflows.Runtime/Contracts/`. The HTTP and Scheduling adapters live inside their own modules and depend only on the public `IIngressSource` contract — no back-references. |
| II. Composition & Extensibility | PASS | Third-party ingress registration shown in quickstart.md is a single `services.AddIngressSource<T>()` call; no runtime changes needed. |
| III. Convention-Driven Design | PASS | The four admin endpoints each sit in `Endpoints/Admin/{Action}/Endpoint.cs` per the `ElsaEndpoint<TRequest, TResponse>` single-endpoint-per-class convention. |
| IV. Async & Pipeline Execution | PASS | Every contract method returns `Task`/`ValueTask`. The drain orchestrator composes pause calls with `Task.WhenAll` + per-source `CancellationTokenSource` for independent timeouts. |
| V. Testing Discipline | PASS | Contract tests freeze the shape of `DrainOutcome` and the status response. Integration tests cover all three user stories. |
| VI. Trunk-Based Development | PASS | Phase 2 PR slicing (below) keeps each PR at a single concern. |
| VII. Simplicity & Focus | PASS | Only the minimum surface needed to satisfy FR-001..FR-035 is introduced. No cross-node handoff, no dynamic ingress registration, no metrics/telemetry layer beyond what the existing mediator already carries. |

**Result**: No post-design gate failures. Complexity Tracking table remains empty.

## Phase 2 Handoff (for `/speckit.tasks`)

The task command should slice work into these PR-shaped buckets, in order:

1. **Core quiescence machinery** — `IQuiescenceSignal`, `IIngressSource`, `IIngressSourceRegistry`, `BurstRegistry`, unit tests. No behaviour change to running workflows yet.
2. **Drain orchestrator + host-stop integration** — `IDrainOrchestrator`, `DrainOrchestratorHostedService`, `GracefulShutdownOptions`, unit + integration tests exercising US1.
3. **`Interrupted` sub-status + execution-log event + activation scan** — enum edits (core + API client), `WorkflowInterruptedPayload`, `RecoverInterruptedWorkflowsStartupTask`, log-extension helper, US3 integration tests. The existing `RestartInterruptedWorkflowsTask` is untouched.
4. **Admin endpoints** — pause/resume/status/force, permission name, US2 integration tests.
5. **First-party ingress adapters** — `HttpTriggerIngressSource`, `ScheduledTriggerIngressSource`, and the distributed bookmark-queue-worker edit. Each wired in via its respective feature registration.

Optional sub-PR (contingent on R3 outcome): extend `IShellFeature` with an async deactivation hook. If the research picks the `IHostedService.StopAsync` fallback, this PR is dropped.

## Complexity Tracking

No constitution violations — table intentionally empty.
