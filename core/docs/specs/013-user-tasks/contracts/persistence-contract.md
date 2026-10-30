# User Tasks Persistence Contract

**Feature**: `013-user-tasks`  
**Status**: Approved for implementation  
**Persistence precedent**: `Elsa.Secrets` module and its EF Core provider packages

This contract defines the storage boundary and provider-parity requirements. It describes logical storage units rather than prescribing one physical database design, but every provider must preserve the same behavior and invariants.

## Package and registration boundary

The module is split as follows:

```text
Elsa.UserTasks
├── contracts, models, activity, manager, in-memory repository
├── claims resolver, access policy seam, projection/reconciliation
├── endpoints, permissions, notifications, invitation orchestration
└── no EF Core or Elsa.Identity dependency

Elsa.UserTasks.Persistence.EFCore
├── UserTasksElsaDbContext
├── entity configurations and repository
└── EFCoreUserTasksPersistenceFeature

Elsa.UserTasks.Persistence.EFCore.Sqlite
Elsa.UserTasks.Persistence.EFCore.SqlServer
Elsa.UserTasks.Persistence.EFCore.PostgreSql
Elsa.UserTasks.Persistence.EFCore.MySql
Elsa.UserTasks.Persistence.EFCore.Oracle
├── provider configuration and design-time factory
├── provider-specific migration assembly
└── provider shell feature

Elsa.UserTasks.Persistence.VNext
├── IPersistenceSchemaProvider descriptor
└── document-store adapter with equivalent semantics
```

The Core feature registers an in-memory repository for development/tests. EF Core and VNext packages replace the repository through feature composition, following the `Elsa.Secrets` pattern. Providers must not add a dependency on `Elsa.Identity`.

## Logical storage units

All units are tenant-scoped. `TenantId` is either an explicit host tenant value or the configured default tenant; it is never taken from an untrusted arbitrary query string.

### `UserTasks`

One row/document per User Task instance.

Required identity and workflow fields:

- `Id`, `TenantId`.
- `WorkflowDefinitionId`, `WorkflowInstanceId`, `ActivityInstanceId`, `BookmarkId`.
- `MaterializationKey`, the normalized tenant/workflow-instance/activity-instance tuple.

Safe query and presentation fields:

- `Title`, `Summary`, `Reference`, `TaskType`, `Tags` (bounded safe JSON or normalized tag representation).
- Requester provider/type/ID and optional display snapshot.
- `Priority`, `DueAt`, `CreatedAt`, `UpdatedAt`, `AssignedAt`, `CompletedAt`.
- `Status`, `IsOverdue` (computed or persisted consistently), and health summary.

Assignment fields:

- Assignee provider/type/ID and optional display snapshot.
- `MembershipResolutionMode`.
- Safe configuration flags such as exclusion-override enabled and timeout/cancellation enabled.

Protected and operational fields:

- `InstructionsJson`/`TaskDataJson`, encrypted or provider-protected bounded JSON.
- Requested and pinned form reference metadata, protected where provider-private.
- Terminal action key, protected normalized completion data, and completion actor reference.
- `Revision`, an integer optimistic-concurrency token.
- `CreatedFromBookmarkRevision` or equivalent workflow marker when needed for reconciliation.

Protected fields are never part of search indexes, list projections, events, notifications, or authorization scopes.

### `UserTaskCandidates`

Materialized candidate relationships used for authorized list and claim queries:

- `Id`, `TenantId`, `TaskId`.
- Participant `Provider`, `Type`, and `ParticipantId`.
- Optional display snapshot.
- Source (`DirectUser`, `DirectGroup`, `SnapshotMember`, or `Invitation`) and source group reference where relevant.
- Created/updated timestamps.

The canonical uniqueness key is tenant, task, provider, participant type, participant ID, and source identity. Duplicate bookmark notifications must not duplicate candidate rows.

For live groups, the group candidate is retained and evaluated against current actor claims. For snapshot groups, the expanded user rows are retained with the original group reference for audit. Exclusions are stored separately or with an unambiguous relationship marker and are checked before candidate eligibility.

### `UserTaskSnapshotMembers` and `UserTaskExclusions`

These optional normalized units make snapshot membership and exclusion checks queryable without deserializing protected task data:

- Tenant/task identifiers.
- Provider, participant type, participant ID, and optional source group.
- Creation timestamp and safe source metadata.

An implementation may combine these with `UserTaskCandidates` when it preserves the same uniqueness and authorization semantics.

### `UserTaskEvents`

Append-only audit timeline:

- `Id`, `TenantId`, `TaskId`, and monotonically increasing task `Revision`.
- `EventType`, `OccurredAt`, operation ID, actor provider/type/ID.
- Safe reason and bounded safe metadata JSON. V1 has no generic completion comment.

Events never contain instructions, task data, forms, completion data, invitation secrets, guest session tokens, or provider-private payloads. Event writes occur in the same transaction as the state transition.

### `UserTaskOperations`

Idempotency and asynchronous transition state:

- `Id`, `TenantId`, `TaskId`, and client `OperationId`.
- Operation kind (`Claim`, `Release`, `Assign`, `ScheduleUpdate`, `Complete`, `Timeout`, `Cancel`, `RetryResolution`, or invitation verification).
- Expected revision, canonical request hash, state, attempts, and created/updated/completed timestamps.
- Safe outcome/status metadata and a protected completion payload when the operation is a completion request.

Unique key: `(TenantId, TaskId, OperationId)`. A repeated operation with the same request hash returns the existing state. A different request hash is a conflict.

### `UserTaskInvitations`

One-time guest invitation state:

- `Id`, `TenantId`, `TaskId`, and optional sibling/replacement group ID.
- Recipient reference or opaque destination metadata, never an Elsa.Identity foreign key.
- Cryptographically strong token hash and challenge/verifier provider name/configuration.
- `Status` (`Pending`, `Dispatched`, `Verified`, `Consumed`, `Revoked`, or `Expired`).
- `ExpiresAt`, `IssuedAt`, `VerifiedAt`, `ConsumedAt`, and `RevokedAt`.

Only the token hash is retained in the aggregate. A raw token must not be recoverable from ordinary reads or logs.

### `UserTaskInvitationDeliveries`

Encrypted transient delivery outbox:

- `Id`, `TenantId`, `TaskId`, `InvitationId`, and dispatcher/provider name.
- Encrypted token payload, delivery metadata, attempt count, state, `AvailableAt`, expiry, and last error code.
- Idempotency key and timestamps.

The encrypted payload is decrypted only for a dispatch attempt and returned once to the dispatcher boundary. Delivery failures are represented by safe error codes. Cleanup is independent of terminal task retention and must respect in-flight retries.

### `UserTaskGuestSessions`

Task-scoped guest capability:

- `Id`, `TenantId`, `TaskId`, `InvitationId`.
- Hashed session token, guest participant reference, granted capability set, issued/expiry timestamps, and revocation timestamp.

The raw session token is returned only at issuance. The hash is unique and all queries are task- and tenant-scoped. A guest session cannot release, reassign, or access another task.

## Required indexes and constraints

Names may vary by provider, but the following logical constraints are mandatory:

- Primary key for every unit on its opaque `Id`.
- Unique `(TenantId, MaterializationKey)` on `UserTasks`.
- Unique `(TenantId, BookmarkId)` on `UserTasks`.
- Indexes for tenant/status; tenant/assignee provider/type/ID; tenant/priority; tenant/due time; workflow definition; workflow instance; activity instance; created/completed time; and revision/health where used by reconciliation.
- Candidate lookup index on tenant/provider/type/participant ID/task status.
- Unique event ordering `(TaskId, Revision)` or an equivalent monotonic event sequence.
- Unique operation key `(TenantId, TaskId, OperationId)` and an index for stale pending operations.
- Unique invitation token hash and indexes for task/status/expiry.
- Unique guest session token hash and indexes for task/expiry/revocation.

Safe search may use normalized columns or a bounded safe search projection. It must cover title, summary, reference, tags, task type, workflow/correlation fields, and safe requester fields without inspecting protected JSON. Cursor pagination uses a stable `(sortValue, Id)` key; counts are optional.

## Repository contracts and transaction behavior

```csharp
public interface IUserTaskRepository
{
    Task<UserTask?> GetAsync(string tenantId, string taskId, CancellationToken cancellationToken = default);
    Task<UserTaskQueryResult> QueryAsync(UserTaskQuery query, CancellationToken cancellationToken = default);
    Task<UserTask?> FindByMaterializationKeyAsync(string tenantId, string key, CancellationToken cancellationToken = default);
    Task<UserTask?> FindByBookmarkIdAsync(string tenantId, string bookmarkId, CancellationToken cancellationToken = default);
    Task SaveAsync(UserTask task, int expectedRevision, CancellationToken cancellationToken = default);
    Task AddProjectionAsync(UserTaskProjection projection, CancellationToken cancellationToken = default);
}
```

The concrete repository may expose child-unit operations through a unit-of-work abstraction. It must support:

1. Authorized, store-translatable list scopes that do not load protected payloads.
2. Atomic compare-and-swap by task ID and expected revision.
3. Atomic candidate/snapshot/exclusion updates.
4. Atomic append of the lifecycle event and operation record with the state transition.
5. Atomic insertion of a workflow-resumption outbox marker for asynchronous completion, timeout, and cancellation.
6. At-least-once delivery with idempotent operation and bookmark keys.

If a database transaction is unavailable, the provider must provide equivalent compare-and-swap and durable ordering guarantees and document the recovery behavior. A mutation must never report success while dropping its event, operation, or required outbox marker.

## Projection and reconciliation storage behavior

The workflow runtime is the source of truth for bookmark existence and workflow resumption. User Tasks is a durable projection with recovery responsibilities.

- `WorkflowBookmarksPersisted` projects committed materializations after the workflow transaction commits.
- Projection is idempotent by `(TenantId, MaterializationKey)` and `BookmarkId`.
- A missing projection is recreated from the committed bookmark payload in bounded pages.
- A stale `Completing`, `TimingOut`, or `Cancelling` operation is requeued or finalized from its durable operation marker.
- A task whose bookmark no longer exists is finalized as `Completed` only when a matching pending operation marker proves the intended resumption; otherwise it is finalized as `Cancelled`.
- Ambiguous, cross-tenant, or malformed records are retained for manager diagnostics and reported as health issues.
- Reconciliation is safe to run on multiple nodes. A distributed lock or compare-and-swap lease prevents duplicate work, while duplicate delivery remains harmless.

## EF Core provider contract

`Elsa.UserTasks.Persistence.EFCore` provides a dedicated `UserTasksElsaDbContext : ElsaDbContextBase`, entity configurations, repository, and `EFCoreUserTasksPersistenceFeature` depending on `UserTasksFeature`. Configuration must:

- Use the Elsa schema abstraction (`IElsaDbContextSchema`) and provider-appropriate migration assembly.
- Configure bounded string lengths, UTC `DateTimeOffset` values, string enum storage, JSON/protected payload columns, and the required keys/indexes.
- Mark `Revision` as an optimistic concurrency token. An update with zero rows affected is a revision conflict, not a successful no-op.
- Keep protected payload properties out of safe projections and indexes.
- Use one transaction for state, candidates, events, operations, and outbox markers whenever the provider supports it.

Provider packages must provide:

- `Elsa.UserTasks.Persistence.EFCore.Sqlite` with SQLite configuration, design-time context factory, migrations, and shell feature.
- `Elsa.UserTasks.Persistence.EFCore.SqlServer` with SQL Server configuration, design-time context factory, migrations, and shell feature.
- `Elsa.UserTasks.Persistence.EFCore.PostgreSql` with PostgreSQL configuration, design-time context factory, migrations, and shell feature.
- `Elsa.UserTasks.Persistence.EFCore.MySql` with MySQL configuration, design-time context factory, migrations, and shell feature.
- `Elsa.UserTasks.Persistence.EFCore.Oracle` with Oracle configuration, design-time context factory, migrations, and shell feature.

The provider differences are dialect and column-type details only. All providers must expose the same lifecycle transitions, revision conflicts, idempotency behavior, visibility scope, retention behavior, and projection/reconciliation semantics. SQLite stores UTC timestamps in its provider-compatible ISO-8601 representation; other providers use their native UTC-capable representation. JSON may use provider-native JSON types or text, but round-trip semantics and payload limits are identical.

Each provider migration must create its complete User Tasks schema, including child units, constraints, indexes, and any required schema marker. Migration tests must apply from an empty database and verify restart durability and indexed query behavior.

## VNext provider parity

`Elsa.UserTasks.Persistence.VNext` registers an `IPersistenceSchemaProvider` describing the same logical storage units, fields, indexes, JSON payload limits, and uniqueness constraints under the `Elsa.UserTasks` schema namespace. Its document-store adapter uses compare-and-swap document versions for `Revision` and supports equivalent indexed queries for safe fields.

The VNext adapter must preserve:

- tenant/materialization/bookmark uniqueness;
- candidate, exclusion, and snapshot membership semantics;
- append-only events;
- operation idempotency and stale-operation scans;
- encrypted invitation delivery outbox and hashed invitation/session tokens;
- terminal-only retention purge; and
- the same protected-data and authorization boundaries.

No provider may make Elsa.Identity a prerequisite or silently weaken tenant isolation.

## Retention and cleanup

- Terminal task and event retention is indefinite by default.
- An opt-in purge job deletes only terminal tasks and their associated terminal events after the configured age.
- Open tasks (`Unassigned`, `Available`, `Assigned`, and transitional states) are never purged.
- Expired/revoked invitations, guest sessions, and delivered invitation outbox entries may be purged under separate bounded policies.
- Purge is tenant-scoped, paged, observable, and safe to retry. It must not remove a task with a pending operation, unresolved health issue requiring recovery, or open workflow bookmark.
- Purge never writes raw payloads to logs or audit records.

## Verification obligations

The provider test suite must cover:

- empty-database migration and restart/reload round trips;
- indexed tenant/status/assignment/due/workflow/cursor queries without protected payload loading;
- unique materialization and bookmark projection under duplicate delivery;
- concurrent claim, completion, timeout, cancellation, and revision conflicts;
- repeated and divergent operation IDs;
- event ordering and append-only behavior;
- invitation token hash, sibling revocation, delivery retry, guest-session expiry, and no raw-token persistence;
- reconciliation of missing projections, stale operations, and orphaned bookmarks; and
- terminal-only retention cleanup.
