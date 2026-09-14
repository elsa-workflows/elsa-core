# Feature Specification: User Tasks

**Feature Branch**: `013-user-tasks`  
**Created**: 2026-08-17  
**Status**: Approved for implementation  
**Input**: Add an identity-neutral, durable human-task module to Elsa Core and a production-capable task workbench to Elsa Studio.

## Product Intent

User Tasks let a workflow pause for a human decision, expose the work through a secure queue, and resume with a typed outcome. Elsa owns task durability and workflow coordination, while the host owns people, groups, authentication, and policy. No required dependency or foreign key may couple the module to `Elsa.Identity`.

## User Scenarios & Testing

### User Story 1 - Model and execute a user task (Priority: P1)

A workflow designer adds a User Task activity, configures visible context, assignment rules, completion actions, and an optional form. At runtime the workflow suspends until an authorized worker completes the task.

**Why this priority**: Durable workflow suspension and typed human completion are the feature's core value.

**Independent Test**: Publish and run a workflow containing one User Task, complete it through REST, and verify the selected action and normalized form data resume the correct activity exactly once.

**Acceptance Scenarios**:

1. **Given** a task with a direct assignee, **When** the activity executes, **Then** an `Assigned` task and matching bookmark are persisted and only the assignee or a manager can read protected content.
2. **Given** a task with candidates, **When** an eligible worker claims it, **Then** the claim is atomic and competing claimants receive a conflict.
3. **Given** a valid completion request, **When** it is accepted, **Then** the API returns `202`, transitions through `Completing`, resumes the workflow, and eventually records `Completed` once.

---

### User Story 2 - Work from a task inbox (Priority: P1)

Workers use Elsa Studio to see work assigned to them, discover available work, inspect authorized details, claim or release it, and complete it. Managers monitor all work and repair or reassign tasks that need attention.

**Why this priority**: A human task that cannot be found and acted upon is not operationally useful.

**Independent Test**: Seed tasks in each lifecycle state and verify each Studio tab, filter, action, disclosure rule, and mobile detail route against the REST API.

**Acceptance Scenarios**:

1. **Given** an authenticated worker, **When** they open User Tasks, **Then** Assigned to me, Available, and History contain only relationship-authorized safe records.
2. **Given** an unclaimed candidate task, **When** a candidate opens it, **Then** only its safe summary is visible until claim succeeds.
3. **Given** a manager, **When** they open All or Needs Attention, **Then** they can diagnose, reassign, retry resolution, update due date or priority, and optionally cancel without completing on another person's behalf.

---

### User Story 3 - Integrate a host-owned identity system (Priority: P1)

An Elsa integrator maps its authenticated principals and directory to opaque, tenant-scoped participant references and supplies task authorization without adopting Elsa Identity.

**Why this priority**: Identity neutrality is a hard compatibility boundary for embedded Elsa deployments.

**Independent Test**: Replace the default claims adapter and directory with test implementations and verify list visibility, live and snapshot groups, exclusions, management scope, and display resolution.

**Acceptance Scenarios**:

1. **Given** a namespaced participant reference unknown to the directory, **When** an exact authenticated claim matches it, **Then** live-mode authorization can succeed and activation does not fail.
2. **Given** snapshot membership that cannot be enumerated, **When** the task activates, **Then** it is created with a manager-only blocking health issue.
3. **Given** an excluded participant, **When** they otherwise match assignment, **Then** access is denied unless the activity permits a manager override and an audited reason is supplied.

---

### User Story 4 - Complete a task from an external portal (Priority: P2)

A workflow invites an external person. The host delivers a one-time token, the guest completes a pluggable verification challenge, and a task-scoped guest session permits completion without creating an Elsa user.

**Why this priority**: Customer approvals and document requests are high-value workflow scenarios.

**Independent Test**: Issue multiple invitations, verify one, confirm it atomically claims the task and revokes its siblings, and complete through the guest page with no identity-module dependency.

**Acceptance Scenarios**:

1. **Given** an unverified invitation link, **When** it is opened, **Then** the response reveals generic content only.
2. **Given** a valid challenge, **When** verification succeeds, **Then** a bounded task-scoped session is issued, the task is claimed, and competing invitations are revoked.
3. **Given** an expired, consumed, or invalid token, **When** verification is attempted, **Then** the API returns a generic rate-limited failure without revealing task existence.

---

### User Story 5 - Meet deadlines and recover reliably (Priority: P2)

Operators rely on due-date notifications, optional timeout outcomes, reconciliation, audit history, and provider-backed persistence across restarts and multiple nodes.

**Why this priority**: Human work is asynchronous and must survive failures and races.

**Independent Test**: Exercise completion-versus-timeout races on two nodes, restart between bookmark commit and projection, and verify first-writer-wins, idempotent notifications, and reconciliation.

**Acceptance Scenarios**:

1. **Given** an overdue task without timeout enabled, **When** the scanner processes it repeatedly, **Then** one overdue notification is published and the task remains open.
2. **Given** timeout enabled, **When** due time wins the terminal transition, **Then** the workflow resumes with reserved action `Timeout` and the task becomes `TimedOut`.
3. **Given** a committed bookmark lacking a task projection, **When** reconciliation runs, **Then** the missing task is recreated without duplication.

## Edge Cases

- A task with neither assignee, candidates, nor invitations becomes `Unassigned`, visible only to managers, and records a warning.
- Release immediately revokes protected access and returns to `Available` or `Unassigned`.
- Reuse of an operation ID with the same payload is idempotent; reuse with different content conflicts.
- Completion, timeout, cancellation, and bookmark removal races use expected revision and a transitional state so exactly one terminal result wins.
- A configured form that cannot be resolved blocks worker completion and creates a manager-only health issue; repair retries the original reference.
- “Latest” form references are pinned at activation so later form changes cannot alter an open task.
- Search never examines protected instructions, task data, form data, or completion data.
- Terminal history grants protected data only to the completer and managers; prior assignees receive summary and audit only.
- Unknown display names degrade to opaque participant IDs and a health indication, not workflow failure.
- Invitations expire at the configured bound, defaulting to the earlier of seven days or task due time.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST provide a blocking `UserTask` workflow activity with expression-capable design inputs and typed result outputs.
- **FR-002**: The activity MUST materialize title, summary, reference, tags, task type, requester, priority, due time, instructions, bounded task data, assignment rules, exclusions, actions, form reference, and invitation definitions at activation.
- **FR-003**: Completion action keys MUST be immutable literals; `Timeout` and `Cancelled` are reserved; labels MAY be expressions.
- **FR-004**: Task status MUST follow `Unassigned`, `Available`, `Assigned`, transitional `Completing`/`TimingOut`/`Cancelling`, and terminal `Completed`/`TimedOut`/`Cancelled` states.
- **FR-005**: Claim, release, direct assign, reassign, due-date update, priority update, completion, timeout, and optional cancellation MUST be atomic, revision-checked, idempotent where applicable, and audited.
- **FR-006**: Completion MUST be asynchronous: accept a command, persist operation state, enqueue bookmark resumption, and finalize from bookmark removal or reconciliation.
- **FR-007**: The module MUST project committed User Task bookmarks and reconcile missing projections, stale operations, and orphan task records in bounded pages.
- **FR-008**: The module MUST use opaque participant references composed of tenant, provider namespace, participant type, and external ID, with no required Elsa Identity dependency.
- **FR-009**: The host MUST be able to replace principal mapping, identity resolution, participant lookup, task policy, form, invitation delivery, invitation verification, and guest session services.
- **FR-010**: Authorization MUST require both operation permission and a task relationship; managers remain tenant-scoped and guest sessions remain task-scoped.
- **FR-011**: Candidate users MUST see safe summary only; protected instructions, task data, and forms MUST be disclosed only after assignment and revoked on release.
- **FR-012**: Live group membership MUST use current principal claims; snapshot membership MUST expand at activation and create a manager-only issue if enumeration fails.
- **FR-013**: Explicit exclusions MUST override ordinary assignment; an optional manager override MUST require a reason and audit event.
- **FR-014**: The requester MUST be searchable and visible but MUST confer no access.
- **FR-015**: Forms MUST be provider-neutral, version-pinned at activation, validated and normalized by the provider, and repairable only by retrying the original reference.
- **FR-016**: Tasks without a form MUST accept only a configured action and no arbitrary completion JSON.
- **FR-017**: Due processing MUST publish one idempotent overdue notification; when timeout is enabled it MUST resume with `Timeout` and finalize as `TimedOut`.
- **FR-018**: Manager cancellation MUST be activity-enabled, permission-protected, reason-required, and resume with `Cancelled`.
- **FR-019**: REST APIs under `/user-tasks` MUST provide cursor-based authorized search, details, events, capabilities, claim/release/assign, priority/due update, complete/cancel, resolution retry, participant lookup, and invitation operations.
- **FR-020**: Terminal mutation APIs MUST require `expectedRevision` and `operationId`, return `202` when accepted, `409` for races or divergent operation reuse, and `422` for domain validation.
- **FR-021**: Anonymous invitation APIs MUST use generic errors and rate limiting; raw invitation secrets MUST never be stored outside an encrypted transient delivery outbox and MUST be returned only once to a dispatcher.
- **FR-022**: The first successfully verified invitation MUST atomically claim the task, revoke sibling invitations, and issue a bounded guest session; guests MUST NOT release tasks.
- **FR-023**: Persistence MUST offer an in-memory default plus EF Core packages for SQLite, SQL Server, PostgreSQL, MySQL, and Oracle, and a VNext provider abstraction with equivalent behavior.
- **FR-024**: Storage MUST index normalized query fields and use bounded JSON only for protected task, form, and completion payloads; default payload limit is 256 KiB.
- **FR-025**: Audit events MUST be append-only, tenant-scoped, and exclude raw protected payloads.
- **FR-026**: Terminal retention MUST be indefinite by default with configurable purge that never deletes open tasks.
- **FR-027**: Studio MUST add Workflows → User Tasks with Assigned to me, Available, History, and manager-only All and Needs Attention views.
- **FR-028**: Studio MUST provide desktop queue/detail and mobile detail routes, URL-backed filters, workflow-instance deep links, capability-driven actions, safe disclosure, and no embedded diagram.
- **FR-029**: Studio MUST consume metadata-free SignalR invalidations and requery, with polling fallback.
- **FR-030**: Studio's generic activity editor MUST support all activity fields; participant lookup is optional with raw namespaced references and expressions as fallback.
- **FR-031**: Studio MUST ship a replaceable guest task page that reveals no task-specific information before successful verification.
- **FR-032**: Search MUST be limited to safe title, summary, reference, tags, task type, workflow, and correlation fields and MUST use opaque cursor pagination with stable sorting and optional totals.
- **FR-033**: Existing generic `RunTask` behavior and `/tasks/{id}/complete` MUST remain unchanged.
- **FR-034**: The system MUST publish mediator lifecycle notifications and metadata-free realtime invalidations for external integrations.

### Key Entities

- **UserTask**: Durable runtime projection linked to tenant, workflow, activity instance, and bookmark; owns lifecycle, safe/protected content, timing, assignment, form pin, health, revision, and terminal result.
- **ParticipantReference**: Opaque tenant/provider/type/ID identity value with an optional display snapshot.
- **Candidate / Exclusion / SnapshotMember**: Assignment relationships materialized for authorization and querying.
- **UserTaskAction**: Stable completion key and materialized display label.
- **UserTaskOperation**: Idempotency and asynchronous transition record keyed by task and operation ID.
- **UserTaskEvent**: Append-only audit event with actor reference and safe metadata.
- **UserTaskInvitation**: Hashed one-time invitation, challenge policy, expiry, state, and guest claim relationship.
- **GuestSession**: Revocable, task-scoped capabilities and expiry.
- **InvitationDelivery**: Encrypted transient outbox entry for retryable host delivery.

## Success Criteria

- **SC-001**: A published workflow can suspend at a User Task and resume exactly once with a typed result across restart and retry tests.
- **SC-002**: Two concurrent claimants or terminal actors yield exactly one winner in all automated race tests.
- **SC-003**: Authorization tests prove no candidate, released worker, unrelated user, cross-tenant manager, or unverified guest can read protected content.
- **SC-004**: Authorized list queries remain cursor-stable at the target envelope of 100,000 open tasks and millions of terminal tasks per tenant without loading protected payloads.
- **SC-005**: All supported persistence providers expose the same schema contract and the SQLite integration suite proves restart durability and indexed query behavior.
- **SC-006**: A worker can locate, claim, and complete a seeded task from Studio on desktop and a narrow viewport without entering an Elsa Identity user ID.
- **SC-007**: Reconciliation restores every intentionally interrupted bookmark/task/operation test fixture without duplicate tasks or workflow resumes.
- **SC-008**: Requirements, contracts, implementation tasks, automated tests, and documentation retain stable traceability identifiers.

## Assumptions and Scope

- Hosts already provide authentication and a permission pipeline; the module provides replaceable adapters and safe defaults, not a user database.
- Server time is UTC and persistence providers support optimistic concurrency.
- External files are referenced by URI or provider ID; they are not embedded in task JSON.
- V1 excludes standalone tasks, a native form builder, drafts, comments, attachments, formal delegation, bulk actions, named saved filters, and multi-response tasks.
- GitHub issue, project, pull-request, and remote review gates are intentionally skipped for this delivery because of reported GitHub downtime; all local specification, test, build, and self-review gates remain in force.
