# User Tasks Runtime Contract

**Feature**: `013-user-tasks`  
**Status**: Approved for implementation  
**Audience**: Elsa Core module and host integrations

This contract defines the provider-neutral runtime seams. Names are normative unless explicitly marked as an implementation detail. All asynchronous methods accept cancellation and all identity values are external opaque references.

## Runtime principles

- User Tasks are workflow-bound. The activity suspends on a dedicated User Task bookmark and resumes only through `UserTaskStimulus`.
- The existing generic `RunTask` activity and `/tasks/{id}/complete` endpoint are not changed.
- Core owns task state, optimistic concurrency, operation idempotency, protected-data disclosure, projection, reconciliation, and audit events.
- Hosts own authentication, participant identity, group membership, directory display data, policy, form implementations, invitation delivery, invitation verification, and guest session infrastructure.
- Elsa.Identity is optional. No contract accepts an Elsa.Identity user or group entity.

## Identity-neutral contracts

### ParticipantReference

```csharp
public enum UserTaskParticipantType
{
    User,
    Group
}

public sealed record ParticipantReference(
    string TenantId,
    string Provider,
    UserTaskParticipantType Type,
    string Id,
    string? DisplayName = null);
```

`TenantId`, `Provider`, `Type`, and `Id` are required and form the canonical identity. `DisplayName` is a non-authoritative presentation snapshot. Implementations must normalize and compare the canonical fields consistently, but must not rewrite the external ID.

### Principal mapping

```csharp
public sealed record UserTaskActor(
    ParticipantReference Subject,
    IReadOnlyCollection<ParticipantReference> Groups,
    string? DisplayName = null);

public interface IUserTaskIdentityResolver
{
    ValueTask<UserTaskActor?> ResolveAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default);
}
```

The default implementation is claims-based. Subject, provider/issuer, display-name, and group claim names are configurable. The default subject lookup prefers `NameIdentifier`, then `sub`; the provider falls back to the configured namespace when no issuer is present. Hosts may replace the resolver entirely.

### Task authorization

Authorization has two independent gates: the module permission and a task relationship/policy scope. The policy must be usable before protected task data is loaded so list queries cannot leak invisible tasks.

```csharp
public enum UserTaskAccessOperation
{
    ReadSummary,
    ReadProtected,
    Claim,
    Release,
    Assign,
    UpdateScheduling,
    Complete,
    Cancel,
    Manage,
    IssueInvitation,
    RetryResolution
}

public interface IUserTaskAccessPolicy
{
    Task<UserTaskQueryScope> CreateScopeAsync(
        UserTaskActor actor,
        UserTaskAccessOperation operation,
        CancellationToken cancellationToken = default);

    Task<bool> AuthorizeAsync(
        UserTask task,
        UserTaskActor actor,
        UserTaskAccessOperation operation,
        CancellationToken cancellationToken = default);
}
```

`CreateScopeAsync` must produce a store-translatable tenant/relationship scope. `AuthorizeAsync` is the final check after a task has been loaded. Managers remain tenant-scoped. An inaccessible task is reported as not found at the API boundary.

### Participant directory

Directory lookup is optional and never authoritative for authorization.

```csharp
public interface IUserTaskParticipantDirectory
{
    Task<ParticipantSearchResult> SearchAsync(
        UserTaskParticipantQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ParticipantReference>> ResolveDisplayNamesAsync(
        IReadOnlyCollection<ParticipantReference> participants,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ParticipantReference>> EnumerateGroupMembersAsync(
        ParticipantReference group,
        CancellationToken cancellationToken = default);
}
```

If a directory is not registered, capability discovery reports it as unavailable and Studio falls back to raw namespaced references or expressions. Live group authorization still uses the actor's exact namespaced claims. Snapshot enumeration failure creates a durable manager-only health issue.

## Form provider contract

```csharp
public sealed record UserTaskFormReference(
    string ProviderName,
    string Key,
    string? Binding = null,
    string? Version = null);

public sealed record ResolvedUserTaskForm(
    UserTaskFormReference Requested,
    string PinnedVersion,
    IReadOnlyDictionary<string, object?> Metadata);

public interface IUserTaskFormProvider
{
    string Name { get; }

    Task<ResolvedUserTaskForm?> ResolveAsync(
        UserTaskFormReference reference,
        CancellationToken cancellationToken = default);

    Task<UserTaskFormValidationResult> ValidateAndNormalizeAsync(
        ResolvedUserTaskForm form,
        string actionKey,
        JsonElement data,
        CancellationToken cancellationToken = default);
}
```

The provider resolves and pins the form at task activation. Completion always validates against that pinned version. A missing provider or failed resolution blocks worker completion and is repairable only by retrying the original reference. Server-supplied HTML is not a Core contract and must not be rendered by Studio without a trusted installed renderer.

Tasks without a form must reject arbitrary completion JSON. A form provider returns normalized data; Core stores it as protected completion data and exposes it only according to task visibility rules.

## Task manager contract

The public manager is the only lifecycle orchestration entry point. Repositories and projection stores are not exposed to workflow activities or API endpoints directly.

```csharp
public interface IUserTaskManager
{
    Task<UserTaskProjectionResult> ProjectAsync(
        UserTaskMaterialization materialization,
        CancellationToken cancellationToken = default);

    Task<UserTaskQueryResult> QueryAsync(
        UserTaskQuery query,
        UserTaskActor actor,
        CancellationToken cancellationToken = default);

    Task<UserTaskDetail?> GetAsync(
        string tenantId,
        string taskId,
        UserTaskActor actor,
        CancellationToken cancellationToken = default);

    Task<UserTaskOperationResult> ClaimAsync(
        string tenantId,
        string taskId,
        UserTaskMutationRequest request,
        UserTaskActor actor,
        CancellationToken cancellationToken = default);

    Task<UserTaskOperationResult> ReleaseAsync(
        string tenantId,
        string taskId,
        UserTaskMutationRequest request,
        UserTaskActor actor,
        CancellationToken cancellationToken = default);

    Task<UserTaskOperationResult> AssignAsync(
        string tenantId,
        string taskId,
        UserTaskAssignRequest request,
        UserTaskActor actor,
        CancellationToken cancellationToken = default);

    Task<UserTaskOperationResult> UpdateSchedulingAsync(
        string tenantId,
        string taskId,
        UserTaskSchedulingUpdate request,
        UserTaskActor actor,
        CancellationToken cancellationToken = default);

    Task<UserTaskOperationResult> CompleteAsync(
        string tenantId,
        string taskId,
        UserTaskCompletionRequest request,
        UserTaskActor actor,
        CancellationToken cancellationToken = default);

    Task<UserTaskOperationResult> CancelAsync(
        string tenantId,
        string taskId,
        UserTaskCancelRequest request,
        UserTaskActor actor,
        CancellationToken cancellationToken = default);

    Task<UserTaskOperationResult> RetryResolutionAsync(
        string tenantId,
        string taskId,
        UserTaskMutationRequest request,
        UserTaskActor actor,
        CancellationToken cancellationToken = default);
}
```

The concrete DTO names may follow repository conventions, but the following fields and semantics are required:

- `UserTaskMutationRequest`: `ExpectedRevision`, and `OperationId` for retried/asynchronous operations.
- `UserTaskAssignRequest`: target canonical participant, optional reason, `ExpectedRevision`, and `OperationId`.
- `UserTaskSchedulingUpdate`: optional `Priority` and `DueAt`; no other task definition fields may be changed.
- `UserTaskCompletionRequest`: `ExpectedRevision`, `OperationId`, configured `ActionKey`, and optional JSON `Data` only when a form is installed. V1 has no generic completion comment; explanation fields belong to the pinned form.
- `UserTaskCancelRequest`: `ExpectedRevision`, `OperationId`, and mandatory safe `Reason`.

Claim, release, assignment, and scheduling updates are synchronous state transitions. Completion and manager cancellation persist a transitional operation, enqueue workflow resumption, and return an accepted result while the task is `Completing` or `Cancelling`. Timeout uses the same operation path with `TimingOut` and reserved action `Timeout`.

## Workflow bookmark and projection contracts

The activity materializes its evaluated definition into a dedicated bookmark. The bookmark payload is safe to retry and contains no host-only identity object.

```csharp
public sealed record UserTaskMaterialization(
    string TenantId,
    string WorkflowDefinitionId,
    string WorkflowInstanceId,
    string ActivityInstanceId,
    string BookmarkId,
    UserTaskDefinitionSnapshot Definition,
    IReadOnlyCollection<ParticipantReference> SnapshotMembers,
    IReadOnlyCollection<ParticipantReference> SnapshotGroups,
    DateTimeOffset CreatedAt);

public sealed record UserTaskStimulus(
    string TenantId,
    string TaskId,
    string OperationId,
    string ActionKey,
    JsonElement? CompletionData);
```

`UserTaskStimulus` is the only completion stimulus for this module. The activity binds the resulting `UserTaskResult` and selected action to its outputs and emits the configured workflow outcome.

```csharp
public interface IUserTaskProjectionService
{
    Task ProjectCommittedBookmarksAsync(
        IReadOnlyCollection<UserTaskMaterialization> materializations,
        CancellationToken cancellationToken = default);

    Task FinalizeBookmarkRemovalAsync(
        UserTaskBookmarkRemoval removal,
        CancellationToken cancellationToken = default);
}

public interface IUserTaskReconciler
{
    Task<UserTaskReconciliationResult> ReconcileAsync(
        UserTaskReconciliationRequest request,
        CancellationToken cancellationToken = default);
}
```

The `WorkflowBookmarksPersisted` path calls the projection service only after the workflow commit. Projection is idempotent by the materialization key and bookmark ID. Bookmark removal finalizes a pending matching completion as `Completed`; removal without a pending completion finalizes the task as `Cancelled` and never independently resumes the workflow.

The bounded reconciler must:

1. Recreate a missing task projection for every committed User Task bookmark.
2. Requeue stale `Completing`, `TimingOut`, or `Cancelling` operations whose workflow stimulus was not delivered.
3. Finalize task records whose bookmark was removed, subject to the matching operation marker.
4. Report, but not silently delete, ambiguous or cross-tenant records.

Task creation, operation state, lifecycle event, and transactional outbox marker must commit together where the provider supports a transaction. Delivery is at least once; consumers are idempotent by task and operation ID.

## Invitation, delivery, and guest-session contracts

```csharp
public interface IUserTaskInvitationDispatcher
{
    Task DispatchAsync(
        UserTaskInvitationDelivery delivery,
        CancellationToken cancellationToken = default);
}

public interface IUserTaskInvitationVerifier
{
    Task<UserTaskInvitationVerificationResult> VerifyAsync(
        UserTaskInvitationChallenge challenge,
        CancellationToken cancellationToken = default);
}

public interface IUserTaskGuestSessionIssuer
{
    Task<GuestSessionResult> IssueAsync(
        UserTaskInvitation invitation,
        CancellationToken cancellationToken = default);
}
```

Invitation issuance stores only a hash of the redemption token in the task aggregate. The raw token is returned once to the delivery outbox/dispatcher boundary and is never included in logs, events, API responses, or ordinary persistence reads. The transient delivery outbox stores the encrypted token for retry and decrypts it only for a dispatch attempt. Delivery implementations must use the delivery ID as an idempotency key.

Verification must use generic failure responses and rate limits. The first valid verification atomically claims the task for the guest, revokes all sibling invitations, and issues a task-scoped guest session with an explicit expiry. A guest session cannot release or reassign the task and cannot be used for another task.

Invitation expiry defaults to the earlier of seven days or the task due time, when due time exists. Expired, consumed, revoked, and invalid invitations must be indistinguishable at the anonymous API boundary.

## Notifications and audit

Core publishes mediator notifications only after the lifecycle transaction commits. Notifications and realtime invalidations contain metadata such as tenant, task ID, status, revision, and changed safe fields; they never contain protected task/form/completion data, invitation tokens, or provider-private metadata.

Minimum lifecycle notifications are `UserTaskCreated`, `UserTaskChanged`, `UserTaskCompletionAccepted`, `UserTaskCompleted`, `UserTaskTimedOut`, `UserTaskCancelled`, `UserTaskOverdue`, `UserTaskInvitationChanged`, and `UserTaskHealthChanged`.

`UserTaskEvent` is the durable audit record. Hosts may subscribe to mediator notifications for email, webhooks, or escalations; notification delivery is not a required v1 Core feature.
