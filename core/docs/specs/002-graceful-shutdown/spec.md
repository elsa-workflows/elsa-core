# Feature Specification: Graceful Shutdown for the Workflow Runtime

**Feature Branch**: `002-graceful-shutdown`  
**Created**: 2026-04-23  
**Status**: Draft  
**Input**: User description: "When the host application receives a stop signal, ongoing workflow executions should be given a chance to finish their current execution cycle of execution before the process exits. Event ingress points (message consumers, schedulers, HTTP triggers, internal pollers) must be able to consult a runtime quiescence signal and stop feeding new work to the engine while drain is in progress. An administrator must also be able to place the runtime into a reversible paused state for operational maintenance without stopping the host. The design must interoperate with the platform's shell lifecycle so that a shell being retired or reloaded behaves analogously to a host shutdown, scoped to that shell."

## Clarifications

### Session 2026-04-23

- Q: What should happen to an active execution cycle of execution when drain begins — break at the next cancellation check, or let it run to its next natural suspension point bounded by the drain deadline? → A: Let the current execution cycle complete; the drain deadline is the outer bound. Individual activities that call external systems must still honour the cancellation token passed to them. Only a deadline breach or operator force forces cancellation mid-execution cycle.
- Q: Where does pause apply in a multi-shell deployment — globally, per shell, or both? → A: Container-scoped. The runtime quiescence signal is a service registered in the same container as the workflow runtime. Hosts that run a single runtime (no shell platform) get a global pause; hosts that run multiple shells get per-shell pause automatically. No cross-shell coordination is built.
- Q: Should the durable stimulus queue keep accepting new writes while the runtime is paused, or should writes be rejected? → A: Keep accepting writes. Only the processor that drains the queue is paused. A policy-level back-pressure threshold MAY trigger a degraded readiness signal and optionally reject writes, but the default is to buffer so upstream transports are not forced into redelivery loops.
- Q: How should the system behave when an individual ingress source fails to acknowledge a pause request (throws, hangs, or keeps delivering)? → A: Each ingress source has its own bounded pause timeout, independent of the drain deadline. A source that does not complete its pause within its timeout is recorded in a `PauseFailed` state and surfaced via the status API. Drain does not block on any single failing source. If a source implements an optional force-stop capability, it is invoked as escalation. The runtime also tracks which source started each execution cycle so that a source claiming `Paused` while still delivering is detectable.
- Q: How should force-cancelled workflows (deadline breach or operator force) be represented so they can be recovered without being confused with ungraceful crashes? → A: Introduce a new `Interrupted` sub-status, orthogonal to `Suspended`. Set by the drain path before the process exits. On shell activation the runtime performs a scoped scan for `Interrupted` instances and requeues them immediately, bypassing the existing crash-recovery timeout. Ungraceful crashes continue to be recovered by the existing timeout-based path; the two mechanisms coexist.
- Q: Where should the forensic record of an interruption live? → A: In the existing per-instance execution log, using a new `WorkflowInterrupted` event name with a typed payload describing reason, shell generation identifier, and last active activity. No new storage entity. A separate spec will address broadening the execution log to cover lifecycle transitions more generally.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Host receives a stop signal during active workflow execution (Priority: P1)

An operator deploys a new version of the host application, or an orchestrator rolls a pod, and the process receives a stop signal while workflow instances are mid-execution. Those instances must be given the opportunity to finish their current execution cycle of execution and persist at a clean boundary, rather than being aborted mid-activity.

**Why this priority**: Without this, every deployment risks leaving workflows in inconsistent states. This is the core safety story that justifies the feature. Any other user story is refinement on top.

**Independent Test**: Start a long-running workflow whose execution cycle takes several seconds to complete. Issue a stop signal to the host. Verify that the host does not exit until the execution cycle has completed and the instance is persisted at its natural suspension point, and that no new execution cycles were dispatched after the stop signal.

**Acceptance Scenarios**:

1. **Given** a workflow instance is executing a execution cycle of activities, **When** the host receives a stop signal, **Then** the execution cycle runs to completion and the instance reaches a persisted suspension, completion, or fault state before the process exits.
2. **Given** external event ingress is actively delivering messages into the runtime, **When** the host receives a stop signal, **Then** ingress sources pause before the drain waits for active execution cycles, and no new execution cycles start after the pause point.
3. **Given** an active execution cycle has not completed by the drain deadline, **When** the deadline elapses, **Then** the affected instances are cancelled, marked with a distinguishable interruption state, and forensic details are recorded before the process exits.
4. **Given** an individual ingress source fails to acknowledge its pause request, **When** the drain proceeds, **Then** the failing source is surfaced in the status output and drain is not blocked by it; the runtime still completes within the drain deadline.
5. **Given** the host platform exposes shell lifecycle hooks, **When** a single shell is retired or reloaded, **Then** that shell drains in the same manner as a host shutdown, and other shells on the same host are unaffected.

---

### User Story 2 - Administrator pauses the runtime for maintenance (Priority: P2)

An administrator needs to place the workflow runtime into a reversible paused state — for example, to drain a broker queue for migration, to run a potentially disruptive maintenance script, or to investigate a production incident — without stopping the host process.

**Why this priority**: Pause is an operational capability layered on the quiescence machinery introduced in P1. It is valuable but the host-shutdown story ships before it.

**Independent Test**: Invoke the pause API against a running runtime. Verify that ingress sources transition to `Paused`, that workflow instances resulting from already-in-flight execution cycles continue to completion, that no new execution cycles begin, and that invoking the resume API restores normal operation including drainage of any stimuli buffered during the pause.

**Acceptance Scenarios**:

1. **Given** the runtime is in normal operation, **When** an authorised administrator invokes the pause action, **Then** every ingress source transitions to `Paused` (or `PauseFailed` with a visible error), already-running execution cycles continue to completion, and no new execution cycles begin.
2. **Given** the runtime is paused, **When** an authorised administrator invokes the resume action, **Then** ingress sources transition back to `Running`, any buffered stimuli are drained, and normal operation resumes.
3. **Given** the runtime is paused, **When** an unauthorised caller attempts to pause, resume, or query status, **Then** the request is rejected with an authorisation failure outcome.
4. **Given** the runtime is paused, **When** the status action is invoked, **Then** the response identifies the overall quiescence state, the reason (administrative pause), per-ingress-source state, and the count of active execution cycles.
5. **Given** the host runs multiple shells, **When** an administrator pauses one shell's runtime, **Then** the other shells continue normal operation and the paused shell's state is independently reported.
6. **Given** pause is requested while a drain is already in progress, **When** the drain completes, **Then** the runtime does not auto-transition out of pause; resume remains an explicit operator action.
7. **Given** pause state is configured to persist across shell activations, **When** a shell is reloaded while paused, **Then** the new shell generation starts in the paused state so that stimuli accumulated during the reload are not dispatched until resume.

---

### User Story 3 - Interrupted workflows recover automatically on next activation (Priority: P3)

When a drain exceeds its deadline or an operator issues a force-stop, workflows that were mid-execution cycle at that moment are force-cancelled. On the next host start (or next shell activation), these instances should recover automatically, without operator intervention, and should be distinguishable in dashboards and metrics from workflows that died in an ungraceful crash.

**Why this priority**: The runtime already provides timeout-based recovery for ungracefully terminated workflows, so P1 and P2 can ship without this. P3 is a refinement that closes the latency gap (recovery in seconds on startup rather than minutes of staleness detection) and improves operational observability.

**Independent Test**: Force a drain deadline breach on an active execution cycle. Verify that the affected instance is marked with the interrupted sub-status, that a forensic entry is written to the per-instance execution log, and that on next shell activation the instance is requeued and resumed without waiting for the existing timeout-based recovery cadence.

**Acceptance Scenarios**:

1. **Given** a execution cycle is force-cancelled during drain, **When** persistence completes before process exit, **Then** the instance carries the interrupted sub-status and an execution log entry describing the interruption.
2. **Given** a set of instances in the interrupted sub-status, **When** a shell activates, **Then** each instance is requeued for execution within the shell's activation path and does not wait for the periodic recovery task.
3. **Given** an instance died because the host crashed ungracefully (no chance to mark it interrupted), **When** the existing timeout-based recovery task next runs, **Then** that instance is recovered by the existing mechanism exactly as it is today.
4. **Given** an interrupted instance resumes successfully, **When** an operator queries its audit history, **Then** the forensic interruption record remains visible alongside any subsequent events.
5. **Given** dashboards filter workflows by sub-status, **When** an operator views the interrupted bucket, **Then** they see only instances that were force-cancelled by graceful shutdown, not instances that were user-cancelled, faulted, or crash-recovered.

---

### Edge Cases

- A execution cycle schedules additional activities in a tight loop, never naturally reaching suspension. Drain deadline bounds the wait; force-cancel applies.
- An ingress source acknowledges its pause but continues to deliver messages due to a buggy implementation. The runtime detects this via per-execution cycle ingress attribution and flips the source's recorded state to `PauseFailed`.
- Pause is requested while another pause request is in flight. The second request is a no-op with the same reason; the runtime does not create parallel pause operations.
- Resume is requested while drain is in progress. The request is rejected until drain completes; resume is only meaningful for administrative pause.
- A shell is retired before its runtime has ever transitioned into normal operation. No drain is necessary; termination proceeds.
- The durable stimulus queue exceeds its configured back-pressure threshold while paused. The runtime surfaces a degraded readiness signal and, if configured, rejects new writes with a typed error that transports can translate into their own back-pressure primitives.
- A third-party ingress source is registered after application startup. Out of scope: dynamic registration is not supported; changes require a shell activation.
- Multiple operators invoke pause and resume actions concurrently. Requests serialise at the runtime and each receives a deterministic outcome with the post-request state.
- A force-stop request arrives for an ingress source that does not implement the force-stop capability. The source is left in `PauseFailed`; the drain does not block.
- The persistence layer is unavailable at the moment of drain. Execution cycles that complete and cannot be persisted transition into the interrupted sub-status by policy, and the failure is surfaced in the drain outcome.

## Requirements *(mandatory)*

### Functional Requirements

**Quiescence model**

- **FR-001**: The runtime MUST expose a single quiescence signal, readable synchronously by any collaborator, that indicates whether the runtime is currently accepting new work and, if not, the reason (drain, administrative pause, or both).
- **FR-002**: Drain state MUST be forward-only within a single runtime generation. Once entered, the runtime cannot return to normal operation without a new generation (host restart or shell reactivation).
- **FR-003**: Administrative pause state MUST be reversible via an explicit resume action within the same runtime generation.
- **FR-004**: Drain and administrative pause MUST be composable: a runtime may be in both states simultaneously, and the resume action MUST only clear the administrative pause component.
- **FR-005**: The quiescence signal MUST be scoped to the container in which the workflow runtime is registered. In single-runtime hosts this is global; in multi-shell hosts each shell has its own independent quiescence state.

**Ingress source contract**

- **FR-006**: The runtime MUST define a contract by which any component that injects external events into the workflow engine — including first-party message consumers, first-party schedulers, first-party HTTP trigger handlers, internal durable queue processors, internal recurring tasks that enqueue work, and third-party modules — registers itself as an ingress source.
- **FR-007**: Each ingress source MUST support, at minimum, a pause operation, a resume operation, and a state query.
- **FR-008**: Each ingress source MAY declare an optional force-stop capability used by the runtime as escalation when a pause request cannot be completed within its configured timeout.
- **FR-009**: Each ingress source MUST have a configurable pause timeout independent of the overall drain deadline; the default MUST be documented and overridable at registration time.
- **FR-010**: Pause and resume operations on an ingress source MUST be idempotent and safe under concurrent invocation.
- **FR-011**: An ingress source MUST transition through a documented state machine (`Running`, `Pausing`, `Paused`, `PauseFailed`, `Resuming`, `ResumeFailed`). Transitions MUST be observable via the status query.
- **FR-012**: A pause attempt that throws, hangs past the per-source timeout, or is otherwise not completed MUST leave the source in `PauseFailed` with a captured error; it MUST NOT block the overall drain or pause operation.
- **FR-013**: When the runtime enters drain or administrative pause, it MUST invoke pause on all registered ingress sources in parallel, honour each source's individual timeout, and record failures without aborting the overall transition.

**Execution cycle-of-execution lifecycle**

- **FR-014**: The runtime MUST maintain a registry of active execution cycles of execution, including which ingress source initiated each execution cycle where that attribution is available.
- **FR-015**: During drain, active execution cycles MUST be permitted to run to their next natural persistence boundary (suspension, completion, or fault) within the drain deadline.
- **FR-016**: The drain process MUST wait for the active-execution cycle count to reach zero before allowing the host to exit, bounded by the drain deadline.
- **FR-017**: When the drain deadline elapses or an operator-initiated force is issued, the runtime MUST cancel outstanding execution cycles, persist the affected instances in the interrupted sub-status, record forensic detail, and proceed to termination.
- **FR-018**: A execution cycle started by an ingress source that claims to be in the `Paused` state MUST cause the runtime to flip that source's recorded state to `PauseFailed` and surface the inconsistency in the status output.

**Interruption and recovery**

- **FR-019**: The workflow sub-status model MUST include an interrupted value distinct from suspended, cancelled, and faulted. It MUST represent "last execution cycle was force-cancelled by the runtime; the instance remains resumable."
- **FR-020**: Every force-cancelled execution cycle MUST record a forensic entry in the existing per-instance execution log, using a stable event name and a typed payload that includes interruption time, reason, shell generation identifier, and the identifier of the last active activity.
- **FR-021**: On shell activation, the runtime MUST scan for instances in the interrupted sub-status scoped to its container, and requeue each one for execution without waiting for the periodic crash-recovery cadence.
- **FR-022**: The existing timeout-based crash-recovery mechanism MUST continue to operate unchanged as a safety net for ungraceful terminations that had no opportunity to mark instances as interrupted.
- **FR-023**: An interrupted instance that resumes MUST transition out of the interrupted sub-status through the normal execution path; the execution log entry describing its interruption MUST remain.

**Durable stimulus queue**

- **FR-024**: The durable stimulus queue MUST continue to accept new writes while the runtime is in administrative pause or drain; only its processor is subject to pause.
- **FR-025**: The runtime MUST support a configurable maximum depth for the stimulus queue while paused. When exceeded, the runtime MUST surface a degraded readiness signal.
- **FR-026**: When the configured maximum depth is exceeded and the policy is set to reject writes, the runtime MUST reject new stimuli with a typed error that upstream transports can translate into their own back-pressure primitive. The default policy MUST be to buffer.

**Shell lifecycle integration**

- **FR-027**: When the host platform exposes shell lifecycle transitions, the runtime MUST translate a shell moving into its deactivation phase into a drain of that shell's runtime, scoped so that sibling shells are unaffected.
- **FR-028**: Pause state MUST survive shell reactivation when the persistence policy so dictates, so that a runtime reloaded while paused re-enters pause immediately rather than briefly dispatching accumulated stimuli.
- **FR-029**: The runtime MUST continue writing its per-host liveness signal throughout drain, so that the timeout-based crash-recovery path does not false-positive on instances being gracefully handled.

**Administrative interface**

- **FR-030**: The runtime MUST expose an authenticated and authorised action to pause its quiescence state, with an optional caller-provided reason.
- **FR-031**: The runtime MUST expose an authenticated and authorised action to resume its quiescence state.
- **FR-032**: The runtime MUST expose an authenticated and authorised action to query status, returning the composite quiescence state and reason(s), per-ingress-source state and last error, and the active-execution cycle count.
- **FR-033**: Pause, resume, and force-stop actions MUST be idempotent and MUST return the post-request state rather than a transient acknowledgement.
- **FR-034**: Administrative pause, resume, and operator-force events MUST be audited.

**Configuration**

- **FR-035**: The runtime MUST expose configuration for: overall drain deadline, per-ingress-source default pause timeout, stimulus-queue depth threshold while paused, stimulus-queue overflow policy (buffer or reject), and pause-persistence policy (session-scoped or across shell reactivations).

### Key Entities *(include if feature involves data)*

- **Quiescence State**: The composite signal describing whether the runtime is accepting new work and why not. Exposes whether drain, administrative pause, or both are active.
- **Ingress Source**: An adapter through which external events enter the workflow engine. Has a name, a state (`Running`, `Pausing`, `Paused`, `PauseFailed`, `Resuming`, `ResumeFailed`), a configured pause timeout, and an optional force-stop capability.
- **Execution cycle Handle**: A handle representing one in-flight execution cycle of execution. Carries the initiating workflow instance identifier and, where available, the name of the ingress source that caused the execution cycle to start.
- **Interrupted Sub-Status**: A new value in the existing workflow sub-status enumeration, denoting an instance whose last execution cycle was force-cancelled by the runtime and is awaiting recovery.
- **Interruption Record**: A new category of entry in the existing per-instance execution log, carrying a stable event name and a typed payload describing interruption time, reason, shell generation identifier, and last active activity identifier.
- **Drain Outcome**: The structured result of a completed drain, including overall status, elapsed time for each phase, per-source final state with any captured error, and the count of execution cycles force-cancelled at the deadline.

## Assumptions

- The host platform provides a stop signal that can be observed by the runtime.
- The host platform's shell lifecycle, where available, provides forward-only deactivation transitions with a cooperative deadline model. The runtime plugs into that model rather than inventing a parallel one.
- Workflow activities that perform external I/O already observe the cancellation token passed to them, so that force-cancel at drain deadline propagates through in-flight calls. Activities that do not observe cancellation are a per-activity contract issue, not a graceful-shutdown issue.
- Only privileged operational users or automation are expected to invoke pause, resume, and force actions.
- A single runtime generation corresponds to a single logical container — either the host process or one shell activation.
- Deployments that run a distributed cluster tolerate individual nodes refusing new work while draining; cross-node handoff of in-memory execution cycle state is explicitly out of scope.

## Dependencies

- The host platform's shell lifecycle model (where deployments use it) for drain trigger propagation and forward-only state transitions with deadline policy.
- The existing per-instance execution log and its store abstraction, which carry the new interruption event records.
- The existing workflow dispatcher, which is the resume mechanism invoked by the shell-activation recovery scan and the periodic crash-recovery task.
- The existing workflow sub-status enumeration, extended with the new interrupted value.
- The existing host-level liveness heartbeat service, whose lifecycle must continue throughout drain so that the timeout-based crash recovery does not false-positive.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In validation scenarios, 100% of active execution cycles that can complete within the configured drain deadline do so before the host exits, and the corresponding workflow instances reach a persisted suspension, completion, or fault state.
- **SC-002**: In validation scenarios, 100% of execution cycles that cannot complete within the drain deadline result in instances marked with the interrupted sub-status and a corresponding forensic entry in the per-instance execution log.
- **SC-003**: After a drain with interruptions, 100% of interrupted instances are requeued for execution on the next shell activation without waiting for the periodic crash-recovery cadence.
- **SC-004**: An ingress source that fails to pause within its configured timeout does not extend the overall drain duration beyond its deadline, and its failure is reported in the drain outcome.
- **SC-005**: Administrative pause stops all registered ingress sources from initiating new execution cycles within their configured pause timeouts, and administrative resume restores normal operation with accumulated stimuli dispatched in order.
- **SC-006**: Pausing one shell in a multi-shell deployment does not affect the active-execution cycle throughput of any other shell on the same host.
- **SC-007**: 100% of pause, resume, force-stop, and status actions are idempotent: repeated invocations with the same arguments converge on the same observable state and produce no additional audit entries beyond the first effective state change.
- **SC-008**: In validation scenarios, an ungracefully terminated host recovers its in-flight workflows through the existing timeout-based recovery path without regression in recovery latency or success rate relative to the current implementation.
- **SC-009**: A buggy ingress source that acknowledges pause but continues to deliver messages is detected within the first subsequent execution cycle it initiates, and its recorded state transitions to `PauseFailed`.
- **SC-010**: Dashboards filtering workflow instances by sub-status distinguish interrupted instances from suspended, cancelled, faulted, and crash-recovered instances without ambiguity.
