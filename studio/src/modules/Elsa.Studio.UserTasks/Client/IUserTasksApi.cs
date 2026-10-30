using Elsa.Studio.UserTasks.Models;
using Refit;

namespace Elsa.Studio.UserTasks.Client;

public interface IUserTasksApi
{
    [Get("/user-tasks/capabilities")]
    Task<UserTaskFeatureCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default);

    [Get("/user-tasks")]
    Task<UserTaskListResponse> ListAsync(
        string? scope = null,
        [Query(CollectionFormat.Multi)] IReadOnlyCollection<string>? status = null,
        int? priorityFrom = null,
        int? priorityTo = null,
        string? due = null,
        string? from = null,
        string? to = null,
        string? search = null,
        string? workflowDefinitionId = null,
        string? workflowInstanceId = null,
        string? cursor = null,
        int? limit = null,
        string? sort = null,
        string? direction = null,
        bool? includeTotalCount = null,
        CancellationToken cancellationToken = default);

    [Get("/user-tasks/{id}")]
    Task<UserTaskDetail> GetAsync(string id, CancellationToken cancellationToken = default);

    [Get("/user-tasks/{id}/events")]
    Task<UserTaskEventsResponse> GetEventsAsync(string id, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default);

    [Get("/user-tasks/{id}/capabilities")]
    Task<UserTaskCapabilities> GetTaskCapabilitiesAsync(string id, CancellationToken cancellationToken = default);

    [Post("/user-tasks/{id}/claim")]
    Task<UserTaskOperationResponse> ClaimAsync(string id, [Body] UserTaskMutationRequest request, CancellationToken cancellationToken = default);

    [Post("/user-tasks/{id}/release")]
    Task<UserTaskOperationResponse> ReleaseAsync(string id, [Body] UserTaskMutationRequest request, CancellationToken cancellationToken = default);

    [Post("/user-tasks/{id}/assign")]
    Task<UserTaskOperationResponse> AssignAsync(string id, [Body] UserTaskAssignRequest request, CancellationToken cancellationToken = default);

    [Patch("/user-tasks/{id}")]
    Task<UserTaskOperationResponse> UpdateSchedulingAsync(string id, [Body] UserTaskSchedulingUpdate request, CancellationToken cancellationToken = default);

    [Post("/user-tasks/{id}/complete")]
    Task<UserTaskOperationResponse> CompleteAsync(string id, [Body] UserTaskCompletionRequest request, CancellationToken cancellationToken = default);

    [Post("/user-tasks/{id}/cancel")]
    Task<UserTaskOperationResponse> CancelAsync(string id, [Body] UserTaskCancelRequest request, CancellationToken cancellationToken = default);

    [Post("/user-tasks/{id}/retry-resolution")]
    Task<UserTaskOperationResponse> RetryResolutionAsync(string id, [Body] UserTaskMutationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Discloses one masked field. The server audits the reveal and refuses unmarked fields.</summary>
    [Post("/user-tasks/{id}/reveal")]
    Task<UserTaskRevealFieldResponse> RevealFieldAsync(string id, [Body] UserTaskRevealFieldRequest request, CancellationToken cancellationToken = default);

    [Get("/user-task-participants")]
    Task<UserTaskParticipantSearchResponse> SearchParticipantsAsync(string? search = null, string? type = null, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default);

    [Post("/user-tasks/{id}/invitations")]
    Task<UserTaskInvitationIssueResult> CreateInvitationAsync(string id, [Body] UserTaskInvitationRequest request, CancellationToken cancellationToken = default);

    [Get("/user-tasks/{id}/invitations")]
    Task<UserTaskInvitationListResponse> ListInvitationsAsync(string id, CancellationToken cancellationToken = default);

    [Delete("/user-tasks/{id}/invitations/{invitationId}")]
    Task DeleteInvitationAsync(string id, string invitationId, [Body] UserTaskMutationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// The anonymous guest surface. Task detail and completion are addressed through the issued session rather
/// than by task ID, so a guest can only ever reach the single task their invitation was issued for.
/// </summary>
public interface IUserTaskGuestApi
{
    [Get("/user-task-invitations/{token}")]
    Task<UserTaskGuestChallenge> GetChallengeAsync(string token, CancellationToken cancellationToken = default);

    [Post("/user-task-invitations/{token}/verify")]
    Task<UserTaskGuestSession> VerifyAsync(string token, [Body] UserTaskGuestVerificationRequest request, CancellationToken cancellationToken = default);

    [Get("/user-task-sessions/current")]
    Task<UserTaskDetail> GetTaskAsync([Header("Authorization")] string authorization, CancellationToken cancellationToken = default);

    [Post("/user-task-sessions/current/complete")]
    Task<UserTaskOperationResponse> CompleteAsync([Header("Authorization")] string authorization, [Body] UserTaskCompletionRequest request, CancellationToken cancellationToken = default);
}
