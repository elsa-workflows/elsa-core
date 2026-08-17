using System.Security.Claims;
using System.Text.Json;
using Elsa.Mediator.Contracts;

namespace Elsa.UserTasks.Models;

public enum UserTaskParticipantType
{
    User,
    Group
}

public enum UserTaskMembershipResolutionMode
{
    Live,
    Snapshot
}

public enum UserTaskStatus
{
    Unassigned,
    Available,
    Assigned,
    Completing,
    TimingOut,
    Cancelling,
    Completed,
    TimedOut,
    Cancelled
}

public enum UserTaskHealthSeverity
{
    Advisory,
    Blocking
}

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

public enum UserTaskOperationKind
{
    Claim,
    Release,
    Assign,
    ScheduleUpdate,
    Complete,
    Timeout,
    Cancel,
    RetryResolution,
    InvitationVerification
}

public enum UserTaskOperationStatus
{
    Accepted,
    Completed,
    Failed
}

public enum UserTaskInvitationStatus
{
    Pending,
    Dispatched,
    Verified,
    Consumed,
    Revoked,
    Expired
}

public sealed record ParticipantReference(
    string TenantId,
    string Provider,
    UserTaskParticipantType Type,
    string Id,
    string? DisplayName = null)
{
    public bool Matches(ParticipantReference? other) => other != null
        && string.Equals(TenantId, other.TenantId, StringComparison.Ordinal)
        && string.Equals(Provider, other.Provider, StringComparison.Ordinal)
        && Type == other.Type
        && string.Equals(Id, other.Id, StringComparison.Ordinal);
}

public sealed record UserTaskActor(
    ParticipantReference Subject,
    IReadOnlyCollection<ParticipantReference> Groups,
    string? DisplayName = null)
{
    /// <summary>Indicates that the host has granted tenant-scoped manager access.</summary>
    public bool IsManager { get; init; }
    public IReadOnlySet<string> Permissions { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public bool HasPermission(string permission) => Permissions.Contains(permission) || Permissions.Contains("*");
}

public sealed record UserTaskAction(
    string Key,
    string Label,
    IReadOnlyDictionary<string, object?>? Metadata = null);

/// <summary>
/// Describes a guest invitation that is materialized with a task. The secret itself is created only
/// by Core at runtime and is never part of this definition.
/// </summary>
public sealed record UserTaskInvitationDefinition(
    string VerifierName,
    IReadOnlyCollection<string> AllowedActions,
    TimeSpan? Lifetime = null,
    bool BearerOnly = false,
    string? Recipient = null,
    IReadOnlyDictionary<string, object?>? Configuration = null);

public sealed record UserTaskFormReference(
    string ProviderName,
    string Key,
    string? Binding = null,
    string? Version = null);

public sealed record ResolvedUserTaskForm(
    UserTaskFormReference Requested,
    string PinnedVersion,
    IReadOnlyDictionary<string, object?> Metadata);

public sealed record UserTaskDefinitionSnapshot
{
    public string Title { get; init; } = "";
    public string? Summary { get; init; }
    public string? Reference { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = [];
    public string? TaskType { get; init; }
    public ParticipantReference? Requester { get; init; }
    public ParticipantReference? Assignee { get; init; }
    public IReadOnlyCollection<ParticipantReference> CandidateUsers { get; init; } = [];
    public IReadOnlyCollection<ParticipantReference> CandidateGroups { get; init; } = [];
    public IReadOnlyCollection<ParticipantReference> ExcludedUsers { get; init; } = [];
    public UserTaskMembershipResolutionMode MembershipResolutionMode { get; init; } = UserTaskMembershipResolutionMode.Live;
    public bool AllowManagerExclusionOverride { get; init; }
    public int Priority { get; init; } = 50;
    public DateTimeOffset? DueAt { get; init; }
    public string? Instructions { get; init; }
    public JsonElement? TaskData { get; init; }
    public UserTaskFormReference? FormReference { get; init; }
    public IReadOnlyCollection<UserTaskAction> Actions { get; init; } = [];
    public IReadOnlyCollection<UserTaskInvitationDefinition> Invitations { get; init; } = [];
    public bool EnableTimeoutOutcome { get; init; }
    public bool EnableCancellationOutcome { get; init; }

    public UserTaskDefinitionSnapshot Normalize()
    {
        var actions = Actions.Count == 0
            ? [new UserTaskAction("Complete", "Complete")]
            : Actions;

        if (string.IsNullOrWhiteSpace(Title))
            throw new ArgumentException("A User Task title is required.", nameof(Title));
        if (actions.Any(x => string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Label)))
            throw new ArgumentException("User Task action keys and labels are required.", nameof(Actions));
        if (actions.Any(x => string.Equals(x.Key, "Timeout", StringComparison.OrdinalIgnoreCase) || string.Equals(x.Key, "Cancelled", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Timeout and Cancelled are reserved User Task action keys.");
        if (actions.Select(x => x.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() != actions.Count)
            throw new ArgumentException("User Task action keys must be unique.");
        if (Priority is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(Priority), "Priority must be between 0 and 100.");
        if (Invitations.Any(x => string.IsNullOrWhiteSpace(x.VerifierName) || x.AllowedActions.Count == 0 || x.AllowedActions.Any(string.IsNullOrWhiteSpace)))
            throw new ArgumentException("Invitation verifier names and allowed actions are required.", nameof(Invitations));
        if (Invitations.Any(invitation => invitation.AllowedActions.Any(allowed => !actions.Any(action => string.Equals(action.Key, allowed, StringComparison.OrdinalIgnoreCase)))))
            throw new ArgumentException("Invitation actions must be configured User Task actions.", nameof(Invitations));

        return this with { Actions = actions };
    }
}

public sealed class UserTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = "";
    public string WorkflowDefinitionId { get; set; } = "";
    public string WorkflowInstanceId { get; set; } = "";
    public string ActivityInstanceId { get; set; } = "";
    public string BookmarkId { get; set; } = "";
    public string MaterializationKey { get; set; } = "";
    public string Title { get; set; } = "User task";
    public string? Summary { get; set; }
    public string? Reference { get; set; }
    public HashSet<string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? TaskType { get; set; }
    public ParticipantReference? Requester { get; set; }
    public ParticipantReference? Assignee { get; set; }
    public List<ParticipantReference> CandidateUsers { get; set; } = [];
    public List<ParticipantReference> CandidateGroups { get; set; } = [];
    public List<ParticipantReference> SnapshotMembers { get; set; } = [];
    public List<ParticipantReference> SnapshotGroups { get; set; } = [];
    public List<ParticipantReference> ExcludedUsers { get; set; } = [];
    public UserTaskMembershipResolutionMode MembershipResolutionMode { get; set; } = UserTaskMembershipResolutionMode.Live;
    public bool AllowManagerExclusionOverride { get; set; }
    public int Priority { get; set; } = 50;
    public DateTimeOffset? DueAt { get; set; }
    public bool IsOverdue { get; set; }
    public string? Instructions { get; set; }
    public JsonElement? TaskData { get; set; }
    public UserTaskFormReference? RequestedForm { get; set; }
    public ResolvedUserTaskForm? PinnedForm { get; set; }
    public List<UserTaskAction> Actions { get; set; } = [new("Complete", "Complete")];
    public List<UserTaskInvitationDefinition> InvitationDefinitions { get; set; } = [];
    public bool EnableTimeoutOutcome { get; set; }
    public bool EnableCancellationOutcome { get; set; }
    public UserTaskStatus Status { get; set; } = UserTaskStatus.Available;
    public UserTaskHealthSeverity? HealthSeverity { get; set; }
    public string? HealthCode { get; set; }
    public string? HealthMessage { get; set; }
    public string? CompletionActionKey { get; set; }
    public JsonElement? CompletionData { get; set; }
    public ParticipantReference? CompletedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int Revision { get; set; } = 1;
    public List<UserTaskEvent> Events { get; set; } = [];
    public List<UserTaskOperation> Operations { get; set; } = [];
    public List<UserTaskInvitation> Invitations { get; set; } = [];

    public bool IsTerminal => Status is UserTaskStatus.Completed or UserTaskStatus.TimedOut or UserTaskStatus.Cancelled;

    public bool IsOpen => !IsTerminal;
}

public sealed record UserTaskEvent(
    string Id,
    string TenantId,
    string TaskId,
    int Revision,
    string EventType,
    DateTimeOffset OccurredAt,
    ParticipantReference? Actor = null,
    string? OperationId = null,
    string? Reason = null,
    IReadOnlyDictionary<string, object?>? Metadata = null);

public sealed record UserTaskOperation(
    string Id,
    string TenantId,
    string TaskId,
    string OperationId,
    UserTaskOperationKind Kind,
    int ExpectedRevision,
    string RequestHash,
    UserTaskOperationStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? ActionKey = null,
    JsonElement? Data = null,
    string? ErrorCode = null);

public sealed record UserTaskInvitation(
    string Id,
    string TenantId,
    string TaskId,
    string? Recipient,
    string TokenHash,
    UserTaskInvitationStatus Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string? VerifierName = null,
    DateTimeOffset? VerifiedAt = null,
    DateTimeOffset? ConsumedAt = null,
    DateTimeOffset? RevokedAt = null,
    string? SiblingGroupId = null);

public sealed record UserTaskInvitationDelivery(
    string Id,
    string TenantId,
    string TaskId,
    string InvitationId,
    string DispatcherName,
    string Token,
    DateTimeOffset ExpiresAt);

public sealed record UserTaskInvitationChallenge(string Token, string? Code = null, string? State = null);

public sealed record UserTaskInvitationVerificationResult(bool Succeeded, string? FailureCode = null, string? Subject = null);

public sealed record GuestSessionResult(bool Succeeded, string? Token = null, DateTimeOffset? ExpiresAt = null, string? FailureCode = null, string? TaskId = null);

public sealed record UserTaskResult(
    string ActionKey,
    JsonElement? Data,
    ParticipantReference? CompletedBy,
    DateTimeOffset CompletedAt);

public sealed record UserTaskInvitationSummary(
    string Id,
    string TaskId,
    string? Recipient,
    UserTaskInvitationStatus Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string? VerifierName);

public sealed record UserTaskInvitationIssueRequest(
    int ExpectedRevision,
    string VerifierName,
    IReadOnlyCollection<string> AllowedActions,
    string? Recipient = null,
    TimeSpan? Lifetime = null,
    string? OperationId = null);

public sealed record UserTaskInvitationIssueResult(
    UserTaskInvitationSummary Invitation,
    string? OperationId = null);

public sealed record UserTaskInvitationVerificationResultWithSession(
    bool Succeeded,
    string? TaskId = null,
    string? SessionToken = null,
    DateTimeOffset? ExpiresAt = null,
    string? FailureCode = null);

public sealed record UserTaskMaterialization(
    string TenantId,
    string WorkflowDefinitionId,
    string WorkflowInstanceId,
    string ActivityInstanceId,
    string BookmarkId,
    UserTaskDefinitionSnapshot Definition,
    IReadOnlyCollection<ParticipantReference> SnapshotMembers,
    IReadOnlyCollection<ParticipantReference> SnapshotGroups,
    DateTimeOffset CreatedAt,
    string? TaskId = null);

public sealed record UserTaskStimulus(
    string TenantId,
    string TaskId,
    string OperationId,
    string ActionKey,
    JsonElement? CompletionData,
    ParticipantReference? CompletedBy = null,
    DateTimeOffset? CompletedAt = null,
    string? BookmarkId = null);

public sealed record UserTaskBookmarkRemoval(
    string TenantId,
    string TaskId,
    string BookmarkId,
    DateTimeOffset RemovedAt);

public sealed record UserTaskMutationRequest(int ExpectedRevision, string? OperationId = null);

public sealed record UserTaskAssignRequest(
    int ExpectedRevision,
    ParticipantReference Assignee,
    string? Reason = null,
    string? OperationId = null);

public sealed record UserTaskSchedulingUpdate(
    int ExpectedRevision,
    int? Priority = null,
    DateTimeOffset? DueAt = null,
    string? OperationId = null);

public sealed record UserTaskCompletionRequest(
    int ExpectedRevision,
    string OperationId,
    string ActionKey,
    JsonElement? Data = null);

public sealed record UserTaskCancelRequest(int ExpectedRevision, string OperationId, string Reason);

public sealed record UserTaskQueryScope(
    string TenantId,
    ParticipantReference Subject,
    IReadOnlyCollection<ParticipantReference> Groups,
    bool IsManager = false,
    bool IncludeAssigned = true,
    bool IncludeCandidateUsers = true,
    bool IncludeCandidateGroups = true,
    bool IncludeSnapshotMembers = true,
    bool IncludeHistory = true,
    bool ExcludeBlocking = true);

public sealed record UserTaskQuery
{
    public string TenantId { get; init; } = "";
    public UserTaskQueryScope? Scope { get; init; }
    public string? Cursor { get; init; }
    public int Limit { get; init; } = 50;
    public string? Search { get; init; }
    public UserTaskStatus? Status { get; init; }
    public string? TaskType { get; init; }
    public int? PriorityFrom { get; init; }
    public int? PriorityTo { get; init; }
    public DateTimeOffset? DueFrom { get; init; }
    public DateTimeOffset? DueTo { get; init; }
    public string? WorkflowDefinitionId { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public string? Reference { get; init; }
    public string Sort { get; init; } = "created";
    public bool Descending { get; init; }
    public bool IncludeTotalCount { get; init; }
}

public sealed record UserTaskQueryResult(
    IReadOnlyCollection<UserTask> Items,
    string? NextCursor,
    int? TotalCount);

public sealed record UserTaskProjectionResult(UserTask Task, bool Created);

public sealed record UserTaskReconciliationRequest(int PageSize = 100, DateTimeOffset? OlderThan = null)
{
    /// <summary>Runs a tenant-scoped pass. Hosts with a tenant catalog can invoke one pass per tenant.</summary>
    public string TenantId { get; init; } = "";
}

public sealed record UserTaskReconciliationResult(int Recreated, int Requeued, int Finalized, int Ambiguous);

public sealed record UserTaskOperationResult(
    UserTask Task,
    UserTaskOperation Operation,
    bool Accepted,
    string? ConflictCode = null);

public sealed record UserTaskQueryResultDto(
    IReadOnlyCollection<UserTaskSummary> Items,
    string? NextCursor,
    int? TotalCount);

public sealed record UserTaskSummary(
    string Id,
    string TenantId,
    string Title,
    string? Summary,
    string? Reference,
    IReadOnlyCollection<string> Tags,
    string? TaskType,
    UserTaskStatus Status,
    ParticipantReference? Assignee,
    int Priority,
    DateTimeOffset? DueAt,
    bool IsOverdue,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AssignedAt,
    DateTimeOffset? CompletedAt,
    int Revision,
    UserTaskHealthSeverity? HealthSeverity,
    string? HealthCode,
    IReadOnlyCollection<string> AllowedActions,
    string DataAccess);

public sealed record UserTaskDetail(
    UserTaskSummary Summary,
    string? Instructions,
    JsonElement? TaskData,
    UserTaskFormReference? Form,
    ResolvedUserTaskForm? PinnedForm,
    IReadOnlyCollection<UserTaskAction> Actions,
    JsonElement? CompletionData,
    ParticipantReference? CompletedBy,
    IReadOnlyCollection<UserTaskEvent> Events);

public sealed record UserTaskCapabilities(
    string TaskId,
    int Revision,
    IReadOnlyCollection<string> AllowedActions,
    bool CanReadProtected,
    bool CanManage);

public sealed record UserTaskParticipantQuery(string TenantId, string? Search = null, UserTaskParticipantType? Type = null, string? Cursor = null, int Limit = 50);

public sealed record ParticipantSearchResult(IReadOnlyCollection<ParticipantReference> Items, string? NextCursor, int? TotalCount);

public sealed record UserTaskFormValidationResult(bool Succeeded, JsonElement? NormalizedData = null, IReadOnlyCollection<string>? Errors = null);

public abstract record UserTaskLifecycleNotification(string TenantId, string TaskId, UserTaskStatus Status, int Revision) : INotification;

public sealed record UserTaskCreated(string TenantId, string TaskId, UserTaskStatus Status, int Revision) : UserTaskLifecycleNotification(TenantId, TaskId, Status, Revision);
public sealed record UserTaskChanged(string TenantId, string TaskId, UserTaskStatus Status, int Revision, IReadOnlyCollection<string> ChangedFields) : UserTaskLifecycleNotification(TenantId, TaskId, Status, Revision);
public sealed record UserTaskCompletionAccepted(string TenantId, string TaskId, UserTaskStatus Status, int Revision, string OperationId) : UserTaskLifecycleNotification(TenantId, TaskId, Status, Revision);
public sealed record UserTaskCompleted(string TenantId, string TaskId, UserTaskStatus Status, int Revision) : UserTaskLifecycleNotification(TenantId, TaskId, Status, Revision);
public sealed record UserTaskTimedOut(string TenantId, string TaskId, UserTaskStatus Status, int Revision) : UserTaskLifecycleNotification(TenantId, TaskId, Status, Revision);
public sealed record UserTaskCancelled(string TenantId, string TaskId, UserTaskStatus Status, int Revision) : UserTaskLifecycleNotification(TenantId, TaskId, Status, Revision);
public sealed record UserTaskOverdue(string TenantId, string TaskId, UserTaskStatus Status, int Revision) : UserTaskLifecycleNotification(TenantId, TaskId, Status, Revision);
public sealed record UserTaskInvitationChanged(string TenantId, string TaskId, UserTaskStatus Status, int Revision) : UserTaskLifecycleNotification(TenantId, TaskId, Status, Revision);
public sealed record UserTaskHealthChanged(string TenantId, string TaskId, UserTaskStatus Status, int Revision) : UserTaskLifecycleNotification(TenantId, TaskId, Status, Revision);
