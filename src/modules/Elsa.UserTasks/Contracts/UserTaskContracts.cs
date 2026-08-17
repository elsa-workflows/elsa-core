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
    Task<UserTaskQueryScope> CreateScopeAsync(UserTaskActor actor, UserTaskAccessOperation operation, CancellationToken cancellationToken = default);
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
    Task SaveAsync(UserTask task, int expectedRevision, CancellationToken cancellationToken = default);
    Task AddProjectionAsync(UserTask task, CancellationToken cancellationToken = default);
    Task<bool> TryMutateAsync(string tenantId, string taskId, int expectedRevision, Func<UserTask, bool> mutation, CancellationToken cancellationToken = default);
}

public interface IUserTaskManager
{
    Task<UserTaskProjectionResult> ProjectAsync(UserTaskMaterialization materialization, CancellationToken cancellationToken = default);
    Task<UserTaskQueryResultDto> QueryAsync(UserTaskQuery query, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskDetail?> GetAsync(string tenantId, string taskId, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskCapabilities?> GetCapabilitiesAsync(string tenantId, string taskId, UserTaskActor actor, CancellationToken cancellationToken = default);
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

public interface IUserTaskGuestSessionIssuer
{
    Task<GuestSessionResult> IssueAsync(UserTaskInvitation invitation, CancellationToken cancellationToken = default);
}

public interface IUserTaskInvitationService
{
    Task<UserTaskInvitationIssueResult?> IssueAsync(string tenantId, string taskId, UserTaskInvitationIssueRequest request, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserTaskInvitationSummary>?> ListAsync(string tenantId, string taskId, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<bool> RevokeAsync(string tenantId, string taskId, string invitationId, int expectedRevision, UserTaskActor actor, CancellationToken cancellationToken = default);
    Task<UserTaskInvitationVerificationResultWithSession> VerifyAsync(UserTaskInvitationChallenge challenge, CancellationToken cancellationToken = default);
}

public interface IUserTaskNotificationSink
{
    Task PublishAsync(UserTaskLifecycleNotification notification, CancellationToken cancellationToken = default);
}
