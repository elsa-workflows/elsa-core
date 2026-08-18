using System.Security.Claims;
using System.Text.Json;
using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Contracts;

public interface IUserTaskIdentityResolver
{
    ValueTask<UserTaskActor?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}

public interface IUserTaskAccessPolicy
{
    /// <summary>
    /// Builds the authorization predicate for a list request, or returns <c>null</c> when the actor may not
    /// list in the requested scope at all. Callers must treat <c>null</c> as a denial, never as "no filter".
    /// </summary>
    Task<UserTaskQueryScope?> CreateScopeAsync(UserTaskActor actor, UserTaskQueryScopeKind kind, CancellationToken cancellationToken = default);
    Task<bool> AuthorizeAsync(UserTask task, UserTaskActor actor, UserTaskAccessOperation operation, CancellationToken cancellationToken = default);
}

public interface IUserTaskParticipantDirectory
{
    Task<ParticipantSearchResult> SearchAsync(UserTaskParticipantQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ParticipantReference>> ResolveDisplayNamesAsync(IReadOnlyCollection<ParticipantReference> participants, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<ParticipantReference>> EnumerateGroupMembersAsync(ParticipantReference group, CancellationToken cancellationToken = default);
}

public interface IUserTaskFormProvider
{
    string Name { get; }
    Task<ResolvedUserTaskForm?> ResolveAsync(UserTaskFormReference reference, CancellationToken cancellationToken = default);
    Task<UserTaskFormValidationResult> ValidateAndNormalizeAsync(ResolvedUserTaskForm form, string actionKey, JsonElement data, CancellationToken cancellationToken = default);
}

public interface IUserTaskRepository
{
    Task<UserTask?> GetAsync(string tenantId, string taskId, CancellationToken cancellationToken = default);
    Task<UserTaskQueryResult> QueryAsync(UserTaskQuery query, CancellationToken cancellationToken = default);
    Task<UserTask?> FindByMaterializationKeyAsync(string tenantId, string key, CancellationToken cancellationToken = default);
    Task<UserTask?> FindByBookmarkIdAsync(string tenantId, string bookmarkId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the task that owns an invitation by the invitation's stored token hash. Lookup is
    /// deliberately hash-keyed and tenant-agnostic: an anonymous caller presents only a secret, and the
    /// provider must never be asked to trust a caller-supplied tenant or task identifier.
    /// </summary>
    Task<(UserTask Task, UserTaskInvitation Invitation)?> FindByInvitationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task SaveAsync(UserTask task, int expectedRevision, CancellationToken cancellationToken = default);
    Task AddProjectionAsync(UserTask task, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends one audit entry without touching the aggregate's revision. Audit is append-only and must not
    /// consume the optimistic-concurrency token: recording a read would otherwise invalidate the expected
    /// revision a client is already holding and turn its next command into a spurious conflict.
    /// </summary>
    Task AppendEventAsync(string tenantId, string taskId, UserTaskEvent @event, CancellationToken cancellationToken = default);
    Task<bool> TryMutateAsync(string tenantId, string taskId, int expectedRevision, Func<UserTask, bool> mutation, CancellationToken cancellationToken = default);
}

public interface IUserTaskManager
{
    Task<UserTaskProjectionResult> ProjectAsync(UserTaskMaterialization materialization, CancellationToken cancellationToken = default);
    /// <summary>Returns <c>null</c> when the actor may not list in the requested scope.</summary>
    Task<UserTaskQueryResultDto?> QueryAsync(UserTaskQuery query, UserTaskQueryScopeKind scope, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskDetail?> GetAsync(string tenantId, string taskId, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskCapabilities?> GetCapabilitiesAsync(string tenantId, string taskId, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskEventsResult?> GetEventsAsync(string tenantId, string taskId, string? cursor, int limit, UserTaskActor actor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discloses one masked form field after an explicit, audited request. Returns <c>null</c> when the task
    /// is invisible, the field is unknown, or the field was not marked revealable by its form provider.
    /// </summary>
    Task<JsonElement?> RevealFieldAsync(string tenantId, string taskId, string fieldKey, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskOperationResult> ClaimAsync(string tenantId, string taskId, UserTaskMutationRequest request, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskOperationResult> ReleaseAsync(string tenantId, string taskId, UserTaskMutationRequest request, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskOperationResult> AssignAsync(string tenantId, string taskId, UserTaskAssignRequest request, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskOperationResult> UpdateSchedulingAsync(string tenantId, string taskId, UserTaskSchedulingUpdate request, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskOperationResult> CompleteAsync(string tenantId, string taskId, UserTaskCompletionRequest request, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskOperationResult> TimeoutAsync(string tenantId, string taskId, int expectedRevision, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<UserTaskOperationResult> CancelAsync(string tenantId, string taskId, UserTaskCancelRequest request, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskOperationResult> RetryResolutionAsync(string tenantId, string taskId, UserTaskMutationRequest request, UserTaskActor actor, CancellationToken cancellationToken = default);
}

public interface IUserTaskProjectionService
{
    Task ProjectCommittedBookmarksAsync(IReadOnlyCollection<UserTaskMaterialization> materializations, CancellationToken cancellationToken = default);
    Task FinalizeBookmarkRemovalAsync(UserTaskBookmarkRemoval removal, CancellationToken cancellationToken = default);
}

public interface IUserTaskReconciler
{
    Task<UserTaskReconciliationResult> ReconcileAsync(UserTaskReconciliationRequest request, CancellationToken cancellationToken = default);
}

public interface IUserTaskDueService
{
    Task<int> MarkOverdueAsync(string tenantId, DateTimeOffset? now = null, CancellationToken cancellationToken = default);
}

public interface IUserTaskWorkflowResumer
{
    Task ResumeAsync(UserTask task, UserTaskStimulus stimulus, CancellationToken cancellationToken = default);
}

public interface IUserTaskInvitationDispatcher
{
    Task DispatchAsync(UserTaskInvitationDelivery delivery, CancellationToken cancellationToken = default);
}

public interface IUserTaskInvitationVerifier
{
    Task<UserTaskInvitationVerificationResult> VerifyAsync(UserTaskInvitationChallenge challenge, CancellationToken cancellationToken = default);
}

/// <summary>
/// Issues and resolves revocable, task-scoped guest sessions. Implementations must store only a hash of the
/// session credential and must invalidate a session as soon as its task closes.
/// </summary>
public interface IUserTaskGuestSessionIssuer
{
    Task<GuestSessionResult> IssueAsync(UserTaskInvitation invitation, ParticipantReference subject, CancellationToken cancellationToken = default);

    /// <summary>Resolves a presented credential, or returns <c>null</c> for unknown, expired, or revoked sessions.</summary>
    Task<UserTaskGuestSession?> ResolveAsync(string credential, CancellationToken cancellationToken = default);

    /// <summary>Revokes every session issued for a task. Called when the task reaches a terminal state.</summary>
    Task RevokeForTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rate limiter for the anonymous invitation surface. The default implementation is a per-process sliding
/// window; hosts running multiple replicas should replace it with a shared-store implementation.
/// </summary>
public interface IUserTaskInvitationRateLimiter
{
    /// <summary>Returns <c>false</c> when the caller has exhausted its budget and must receive a 429.</summary>
    ValueTask<bool> TryAcquireAsync(string partitionKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Holds invitation secrets between issuance and successful delivery. Entries are encrypted at rest through
/// ASP.NET Core Data Protection and removed once delivery succeeds or the invitation expires.
/// </summary>
public interface IUserTaskInvitationOutbox
{
    Task EnqueueAsync(UserTaskInvitationDelivery delivery, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserTaskInvitationDelivery>> DequeueDueAsync(int maxCount, CancellationToken cancellationToken = default);
    Task CompleteAsync(string deliveryId, CancellationToken cancellationToken = default);
    Task RescheduleAsync(string deliveryId, DateTimeOffset notBefore, CancellationToken cancellationToken = default);
}

public interface IUserTaskInvitationService
{
    Task<UserTaskInvitationIssueResult?> IssueAsync(string tenantId, string taskId, UserTaskInvitationIssueRequest request, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserTaskInvitationSummary>?> ListAsync(string tenantId, string taskId, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(string tenantId, string taskId, string invitationId, int expectedRevision, UserTaskActor actor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Describes the challenge an anonymous holder must answer. The descriptor is deliberately generic so a
    /// caller cannot distinguish a missing, expired, consumed, or revoked invitation from a valid one.
    /// </summary>
    Task<UserTaskInvitationChallengeDescriptor> DescribeAsync(string token, CancellationToken cancellationToken = default);

    Task<UserTaskInvitationVerificationResultWithSession> VerifyAsync(UserTaskInvitationChallenge challenge, CancellationToken cancellationToken = default);
}

public interface IUserTaskNotificationSink
{
    Task PublishAsync(UserTaskLifecycleNotification notification, CancellationToken cancellationToken = default);
}
