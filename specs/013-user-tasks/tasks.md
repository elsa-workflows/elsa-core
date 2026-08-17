# Tasks: User Tasks

Tasks use `[ID] [P?] [Story] Description with file path`. `[P]` tasks may run in parallel when their file ownership does not overlap.

## Phase 1: Durable specification

- [x] T001 Create the authoritative PRD in `specs/013-user-tasks/spec.md`.
- [x] T002 [P] Capture official-source decisions in `specs/013-user-tasks/research.md`.
- [x] T003 [P] Define the runtime data model in `specs/013-user-tasks/data-model.md`.
- [x] T004 [P] Define REST, runtime, identity, forms, invitations, persistence, and Studio contracts in `specs/013-user-tasks/contracts/`.
- [x] T005 Add requirements, security, and accessibility gates in `specs/013-user-tasks/checklists/`.
- [x] T006 Record architecture, sequence, risks, and verification in `specs/013-user-tasks/plan.md`.
- [x] T007 Reconcile dossier terminology and run cross-artifact analysis across `specs/013-user-tasks/`.
- [x] T008 Add canonical User Tasks terminology to `CONTEXT.md` and update the active Spec Kit pointer in `AGENTS.md`.

## Phase 2: Core domain and workflow slice

- [ ] T009 [US1] Scaffold `src/modules/Elsa.UserTasks/Elsa.UserTasks.csproj` and feature/shell registration.
- [ ] T010 [P] [US1] Add enums and value models for participants, lifecycle, health, actions, form pins, results, operations, and invitations under `src/modules/Elsa.UserTasks/Models/`.
- [ ] T011 [P] [US3] Add replaceable identity, access policy, directory, forms, invitations, manager, repository, clock, and scheduling contracts under `src/modules/Elsa.UserTasks/Contracts/`.
- [ ] T012 [US1] Implement the guarded state machine and aggregate invariants in `src/modules/Elsa.UserTasks/Services/DefaultUserTaskManager.cs`.
- [ ] T013 [P] [US1] Implement the in-memory repository with tenant-safe authorized cursor queries in `src/modules/Elsa.UserTasks/Persistence/`.
- [ ] T014 [P] [US3] Implement default claims resolver and default-deny access policy in `src/modules/Elsa.UserTasks/Services/`.
- [ ] T015 [US1] Implement the blocking `UserTask` activity, materialized bookmark payload, stimulus, and typed output under `src/modules/Elsa.UserTasks/Activities/`.
- [ ] T016 [US1] Project committed bookmarks and finalize removed bookmarks in `src/modules/Elsa.UserTasks/HostedServices/` and notification handlers.
- [ ] T017 [US5] Add the bounded startup/recurring reconciler for missing projections, stale operations, and orphan records.
- [ ] T018 [P] [US5] Add cluster-safe due scanning, idempotent overdue notification, and optional timeout operation.
- [ ] T019 [P] Add append-only safe audit and mediator lifecycle notification models/dispatch.
- [ ] T020 [P] Add domain, race, disclosure, identity, projection, reconciliation, and due tests in `test/unit/Elsa.UserTasks.UnitTests/`.

## Phase 3: REST and realtime slice

- [ ] T021 [US2] Add permissions, API DTOs, safe/protected mapping, and canonical API error mapping under `src/modules/Elsa.UserTasks/`.
- [ ] T022 [P] [US2] Implement authorized cursor search, detail, events, and capability endpoints under `Endpoints/UserTasks/`.
- [ ] T023 [P] [US2] Implement claim, release, assignment, and priority/due update endpoints.
- [ ] T024 [P] [US1] Implement asynchronous complete, cancel, and resolution-retry endpoints with operation idempotency.
- [ ] T025 [P] [US3] Implement optional participant lookup without Elsa Identity coupling.
- [ ] T026 [US2] Add metadata-free SignalR invalidation and polling-compatible lifecycle notifications.
- [ ] T027 [P] Add endpoint authorization, concealment, validation, cursor, idempotency, and conflict tests.

## Phase 4: Guest invitations

- [ ] T028 [US4] Implement invitation issuance, token hashing, expiry, sibling revocation, and manager APIs.
- [ ] T029 [US4] Implement Data Protection-backed transient delivery outbox and dispatcher retries.
- [ ] T030 [US4] Implement generic rate-limited challenge and verification endpoints.
- [ ] T031 [US4] Implement atomic guest claim and revocable task-scoped session issuance/validation.
- [ ] T032 [P] Add invitation secrecy, expiry, replay, race, guest capability, and recovery tests.

## Phase 5: Persistence providers

- [ ] T033 [US5] Scaffold `Elsa.UserTasks.Persistence.EFCore` with module DbContext, entity configurations, repository, and feature.
- [ ] T034 [P] [US5] Add normalized indexes and bounded JSON mappings for tasks, participants, audit, operations, invitations, guest sessions, and delivery outbox.
- [ ] T035 [P] [US5] Add SQLite provider configuration, migration, design-time factory, and shell feature.
- [ ] T036 [P] [US5] Add SQL Server provider configuration, migration, design-time factory, and shell feature.
- [ ] T037 [P] [US5] Add PostgreSQL provider configuration, migration, design-time factory, and shell feature.
- [ ] T038 [P] [US5] Add MySQL provider configuration, migration, design-time factory, and shell feature.
- [ ] T039 [P] [US5] Add Oracle provider configuration, migration, design-time factory, and shell feature.
- [ ] T040 [P] [US5] Add VNext document-store repository and feature.
- [ ] T041 [US5] Add shared persistence conformance and SQLite restart/index/tenant/cursor tests.

## Phase 6: Elsa Studio

- [ ] T042 [US2] Scaffold `Elsa.Studio.UserTasks` module, remote feature, service registration, and Workflows menu item.
- [ ] T043 [P] [US2] Add Refit client and safe/protected/capability/command models.
- [ ] T044 [US2] Implement URL-backed Assigned to me, Available, History, All, and Needs Attention queue views.
- [ ] T045 [US2] Implement desktop split detail and mobile detail route with workflow deep link, protected disclosure, timeline, health, and capability actions.
- [ ] T046 [P] [US2] Implement claim/release/assign/update/complete/cancel/retry interactions with asynchronous and conflict refresh states.
- [ ] T047 [P] [US1] Add User Task activity editor support and optional replaceable participant lookup picker with raw/expression fallback.
- [ ] T048 [US2] Add metadata-free SignalR requery coordinator and polling fallback without disrupting focus or form input.
- [ ] T049 [US4] Add replaceable anonymous guest verification and task completion page.
- [ ] T050 [P] Add Studio client/component tests for tabs, URL filters, disclosure, actions, responsive routing, realtime fallback, and accessibility.

## Phase 7: Documentation and local gates

- [ ] T051 Add module configuration, identity integration, forms, invitations, hosting, persistence, and upgrade documentation under `doc/`.
- [ ] T052 Add runnable sample workflows and host adapter examples referenced by `specs/013-user-tasks/quickstart.md`.
- [ ] T053 Run affected Core unit/integration tests and all User Tasks persistence conformance tests.
- [ ] T054 Run affected Studio tests and builds.
- [ ] T055 Build both repositories broadly across configured target frameworks without GitHub-dependent gates.
- [ ] T056 Re-run requirement-to-task-to-test traceability analysis and resolve every critical/high finding.
- [ ] T057 Run up to five local self-review passes for correctness, security, API compatibility, accessibility, and maintainability.
- [ ] T058 Confirm no unrelated files were changed and record local verification evidence.

## Dependencies

T001–T008 precede implementation. Domain T009–T015 precede runtime T016–T019 and REST commands. Auth contracts T011/T014 precede all public queries. Core REST T021–T027 precedes Studio T042–T050. Invitations depend on the core state machine and REST security. EF/VNext depend on repository contracts but can proceed alongside Studio after the Core slice stabilizes. Documentation and final gates follow all slices.

## Requirement traceability

| Requirements | Delivery tasks | Primary verification |
| --- | --- | --- |
| FR-001–FR-007, FR-033 | T009–T020 | Activity/runtime, transition-race, projection, bookmark, and reconciliation tests |
| FR-008–FR-014 | T011, T014, T020–T027 | Identity replacement, authorized query, exclusion, disclosure, and tenant-isolation tests |
| FR-015–FR-018 | T012, T015, T018, T020, T024 | Form pin/validation, no-form completion, overdue/timeout, and cancellation tests |
| FR-019–FR-022 | T021–T032 | REST contract, idempotency, invitation secrecy/rate-limit, and guest-session tests |
| FR-023–FR-026 | T033–T041 | Provider compile/migration review and shared persistence conformance suite |
| FR-027–FR-031 | T042–T050 | Studio client/component, responsive, disclosure, guest, and accessibility tests |
| FR-032, FR-034 | T013, T019, T022, T026, T027 | Cursor stability, search safety, notification, and invalidation tests |

## Local-only gate note

GitHub issue/project/branch/PR/review automation is intentionally omitted because the user reported GitHub downtime. This does not waive specification analysis, automated tests, builds, traceability, or self-review.
