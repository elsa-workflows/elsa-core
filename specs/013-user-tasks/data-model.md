# User Tasks Data Model

**Feature**: `013-user-tasks`  
**Status**: Approved for implementation  
**Source of truth**: [`spec.md`](./spec.md)

This document fixes the vocabulary, aggregate boundaries, lifecycle invariants, and persistence-safe representation for User Tasks. A User Task is always created by a workflow activity; v1 does not support standalone tasks.

## Domain vocabulary

| Term | Meaning |
| --- | --- |
| User Task Definition | Configuration stored in a workflow definition. It is evaluated when the activity executes. |
| User Task Instance | Durable runtime work item created from a committed User Task bookmark. |
| Participant Reference | An identity-neutral `{ tenant, provider, type, id }` value. `type` is `user` or `group` for authenticated participants. |
| Candidate | A participant eligible to claim an available task. |
| Assignee | The single participant currently accountable for the task. The assignee is normally a user; an invitation may establish a task-scoped guest assignee. |
| Requester | Optional informational participant. Requester status never grants task access. |
| Action | A stable workflow outcome key and its materialized display label. |
| Form Reference | A provider-neutral reference to a form definition. The concrete version is pinned when the task activates. |
| Task Health | Operational resolution or delivery state independent of lifecycle status. |
| Operation | An idempotent command record used for asynchronous completion, timeout, cancellation, or another retried mutation. |
| Invitation | A single-task guest candidacy secured by a one-time token and a pluggable verification challenge. |
| Guest Session | A bounded, revocable, task-scoped capability issued after invitation verification. |
| Invitation Delivery | An encrypted, transient outbox entry used to deliver a one-time invitation secret. |

## User Task instance

The instance is the durable projection of one committed bookmark. It has one workflow owner and one terminal result.

### Identity and workflow linkage

- `Id` is a stable opaque task identifier.
- `TenantId` is required for every task and every child record. A host may use a single default tenant, but the storage contract remains tenant-scoped.
- `WorkflowDefinitionId`, `WorkflowInstanceId`, and `ActivityInstanceId` identify the workflow execution that owns the task.
- `BookmarkId` identifies the dedicated User Task bookmark.
- `MaterializationKey` is the normalized tuple `(TenantId, WorkflowInstanceId, ActivityInstanceId)`. It is unique and prevents retries or duplicate bookmark notifications from creating two task instances.
- The existing generic `RunTask` activity and `/tasks/{id}/complete` contract are unrelated and remain unchanged.

### Safe task fields

These fields may be returned to a candidate before claim and may be used for authorized search:

- `Title` (required), `Summary`, `Reference`, `Tags`, and `TaskType`.
- Materialized requester reference and workflow labels/identifiers.
- `Priority`, an integer from 0 through 100; the default is 50.
- `DueAt` in UTC and computed `IsOverdue`.
- `Status`, `CreatedAt`, `AssignedAt`, `CompletedAt`, and `UpdatedAt`.
- Materialized assignee display information when the caller is authorized to see it.

Protected fields are never searched or returned to an unassigned candidate:

- `Instructions`.
- `TaskData`.
- The pinned form reference and provider-private form metadata.
- Completion data and other provider-private payloads.

The default maximum serialized size for each protected task/form/completion payload is 256 KiB. External files are represented by a provider URI or ID; they are not embedded in task JSON.

### Assignment fields

- A direct `Assignee` may be materialized at activation.
- `CandidateUsers` and `CandidateGroups` are stored as namespaced participant references.
- `MembershipResolutionMode` is `Live` or `Snapshot`; `Live` is the default.
- `ExcludedUsers` are canonical participant references and are hard exclusions by default.
- `AllowManagerExclusionOverride` is materialized from the activity. If true, a manager may bypass an exclusion only with a mandatory audit reason.
- Invitations are separate child records and do not turn an external recipient into a global Elsa user.

Display names are optional snapshots used only for presentation. Authorization and uniqueness always use the provider namespace, participant type, and external ID.

## Participant references and membership

```text
ParticipantReference
├── TenantId       required, storage scope
├── Provider       required namespace/issuer
├── Type           user | group
├── Id             required external identifier
└── DisplayName    optional non-authoritative snapshot
```

The default claims adapter maps the authenticated principal to a user reference and group references. Hosts may replace that adapter and may use any external directory or authorization policy. Elsa.Identity is not required and no User Tasks table has a foreign key to an Elsa.Identity table.

### Live membership

Live tasks persist group references and evaluate the caller's current namespaced claims at list/action time. Directory lookup may enrich names or report health, but an unavailable directory does not invalidate an exact authoritative claim match.

### Snapshot membership

At activation, the participant directory enumerates each configured group and persists the resulting user references together with the original group reference. Later organization changes do not alter eligibility. If enumeration is unavailable or fails, the task is still durable but becomes manager-only with a blocking health issue.

### Exclusions

Exclusions are checked against canonical references for claiming, assignment, and guest verification. An excluded participant cannot claim or receive the task unless the activity allows manager override and the manager supplies a reason that is written to the audit event.

## Actions and forms

### Actions

Each action has:

- `Key`: a literal, immutable workflow outcome key.
- `Label`: a materialized display value; the source may be expression-capable.
- Optional safe presentation metadata.

Action keys are unique within a task. `Timeout` and `Cancelled` are reserved keys and cannot be configured by a designer. A task without an enabled form accepts only the selected configured action and no arbitrary completion JSON.

### Forms

A form reference contains `ProviderName`, `Key`, and the requested binding/version information. Activation resolves and pins a concrete provider version. Open tasks never follow a later “latest” form version.

The installed provider validates and normalizes submission data for the pinned version and selected action. If resolution or pinning fails, the task remains durable but is manager-only with a blocking health issue. Repair retries the original reference; managers cannot replace the live form reference ad hoc.

## Lifecycle

### States

| State | Meaning | Worker visibility |
| --- | --- | --- |
| `Unassigned` | No direct assignee, candidate, or invitation is available. | Manager-only; never an open “everyone” queue. |
| `Available` | At least one candidate or invitation may claim the task. | Eligible candidates see safe summary. |
| `Assigned` | One participant is accountable. | Assignee sees protected content; managers see it within tenant scope. |
| `Completing` | A valid completion operation won the race and bookmark resumption is pending. | No second terminal action is accepted. |
| `TimingOut` | A timeout operation won the race and reserved `Timeout` resumption is pending. | No worker action is accepted. |
| `Cancelling` | A manager cancellation operation won the race and reserved `Cancelled` resumption is pending. | No worker action is accepted. |
| `Completed` | Bookmark resumption committed with a configured action. | Terminal. |
| `TimedOut` | Bookmark resumption committed with reserved `Timeout`. | Terminal. |
| `Cancelled` | The task was cancelled by its workflow/activity or by an enabled manager operation. | Terminal. |

### Transitions

| Trigger | Transition and rule |
| --- | --- |
| Activity activation with direct assignee | Create `Assigned`. |
| Activity activation with candidates/invitations | Create `Available`. |
| Activation without any assignment | Create `Unassigned` and a warning/health issue. |
| Eligible claim | Atomically `Available → Assigned`; one winner only. |
| Release | `Assigned → Available`, or `Unassigned` when no eligible candidates/invitations remain. Protected access ends immediately. |
| Manager assign/reassign | Any open task becomes `Assigned`; manager completion still requires assignment to the accountable participant. |
| Authenticated completion | `Assigned → Completing`; enqueue the dedicated User Task bookmark resumption. |
| Due processing with timeout enabled | Open state `→ TimingOut`; resume through reserved `Timeout`; finalize `TimedOut`. |
| Due processing without timeout | Keep the task open, set `IsOverdue`, and publish one idempotent overdue notification. |
| Enabled manager cancellation | Open state `→ Cancelling`; resume through reserved `Cancelled`; finalize `Cancelled`. A reason is required. |
| Workflow/activity bookmark removal | Finalize `Completed` when a matching pending completion exists; otherwise finalize `Cancelled`. It never independently resumes the workflow. |

Completion, timeout, and cancellation use one optimistic transition path. The first committed transition wins; later requests receive a conflict. Guest verification is a special claim operation: the first valid invitation atomically claims the task and revokes sibling invitations. Guests cannot release tasks.

### Mutable after activation

Only priority and due date may be changed through the governed runtime update path. Assignment changes use dedicated claim/release/assign operations. Title, summary, instructions, protected task data, actions, exclusions, membership mode, and the pinned form are immutable for the live task.

## Operations and idempotency

An operation is scoped by `(TenantId, TaskId, OperationId)` and stores the canonical request hash, expected revision, operation kind, status, and timestamps.

- Every mutation requiring a race-safe result supplies `ExpectedRevision`.
- A matching repeated operation ID and request hash returns the existing result/state without performing the command twice.
- Reusing an operation ID with different content is a conflict.
- A completion operation stores normalized completion data only in protected task/operation storage, never in an event or notification payload.
- A stale `Completing`, `TimingOut`, or `Cancelling` operation is retried or completed by reconciliation according to its durable state; it is never silently discarded.

## Events and health

`UserTaskEvent` is append-only, tenant-scoped, and ordered by task revision. Events include the event kind, actor participant reference when known, timestamp, operation ID, safe reason, and safe metadata. They never include protected instructions, task data, form data, completion data, invitation secrets, or provider-private payloads.

At minimum, record `Created`, `Claimed`, `Released`, `Assigned`, `Reassigned`, `CompletionRequested`, `Completed`, `TimedOut`, `Cancelled`, `OverdueNotified`, `InvitationIssued`, `InvitationVerified`, `InvitationRevoked`, and failed privileged operations.

Health issues are separate from lifecycle status. They identify blocking or advisory problems such as form resolution failure, snapshot enumeration failure, unknown participant display data, or invitation delivery failure. Health data is safe for managers and diagnostics; it cannot disclose tokens or protected payloads.

## Visibility and retention invariants

- A candidate receives safe summary only. Protected content is disclosed only to the assignee or a manager.
- Releasing a task revokes protected access immediately.
- A completed task's protected data is visible to the completer and managers; prior assignees retain only summary and safe audit visibility.
- A requester receives no access merely by being the requester.
- Inaccessible task IDs are indistinguishable from missing IDs (`404` at the API boundary).
- All reads and writes are tenant-scoped and must pass both module permission and task relationship/policy checks.
- Terminal retention is indefinite by default. An opt-in purge removes only terminal tasks and their events. Open tasks and their operational records are never purged.
- Expired invitations, revoked guest sessions, and delivered invitation outbox entries may use separate short retention settings, but cleanup must not affect the task aggregate or audit history.

