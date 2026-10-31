using System.Text.Json;

namespace Elsa.Studio.UserTasks.Models;

public static class UserTaskScopes
{
    public const string Assigned = "assigned";
    public const string Available = "available";
    public const string History = "history";
    public const string All = "all";
    public const string NeedsAttention = "needs-attention";
}

/// <summary>
/// Server-issued action names. They arrive on every row and detail response as <c>allowedActions</c>; the
/// client renders a control only when the name is present, never because a capability flag looks right.
/// </summary>
public static class UserTaskActions
{
    public const string Claim = "claim";
    public const string Release = "release";
    public const string Assign = "assign";
    public const string UpdateScheduling = "update-scheduling";
    public const string Complete = "complete";
    public const string Cancel = "cancel";
    public const string Invite = "invite";
    public const string RetryResolution = "retry-resolution";
}

public sealed record UserTaskListQuery
{
    public string Scope { get; init; } = UserTaskScopes.Assigned;
    public IReadOnlyCollection<string> Status { get; init; } = [];
    public int? PriorityFrom { get; init; }
    public int? PriorityTo { get; init; }
    public string? Due { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
    public string? Search { get; init; }
    public string? WorkflowDefinitionId { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public string? Cursor { get; init; }
    public int PageSize { get; init; } = 25;
    public string Sort { get; init; } = "due";
    public string Direction { get; init; } = "asc";

    public bool HasFilters =>
        Status.Count != 0 || PriorityFrom.HasValue || PriorityTo.HasValue
        || !string.IsNullOrWhiteSpace(Due) || !string.IsNullOrWhiteSpace(From) || !string.IsNullOrWhiteSpace(To)
        || !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(WorkflowDefinitionId) || !string.IsNullOrWhiteSpace(WorkflowInstanceId);
}

/// <summary>Tenant- and actor-scoped feature descriptor from <c>GET /user-tasks/capabilities</c>.</summary>
public sealed record UserTaskFeatureCapabilities
{
    public bool Enabled { get; init; }
    public bool CanList { get; init; }
    public bool CanRead { get; init; }
    public bool CanReadAll { get; init; }
    public bool CanClaim { get; init; }
    public bool CanComplete { get; init; }
    public bool CanRelease { get; init; }
    public bool CanAssign { get; init; }
    public bool CanUpdate { get; init; }
    public bool CanCancel { get; init; }
    public bool CanCreateGuestLinks { get; init; }
    public bool CanViewProtected { get; init; }
    public bool ParticipantPicker { get; init; }
    public bool Realtime { get; init; }
    public int PollingIntervalSeconds { get; init; } = 30;
}

/// <summary>Per-task capability and concurrency projection from <c>GET /user-tasks/{id}/capabilities</c>.</summary>
public sealed record UserTaskCapabilities
{
    public string TaskId { get; init; } = "";
    public int Revision { get; init; }
    public IReadOnlyCollection<string> AllowedActions { get; init; } = [];
    public bool CanReadProtected { get; init; }
    public bool CanManage { get; init; }
}

public sealed record UserTaskListResponse
{
    public IReadOnlyCollection<UserTaskSummary> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public int? TotalCount { get; init; }
}

public record UserTaskSummary
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Summary { get; init; }
    public string? Reference { get; init; }
    public IReadOnlyCollection<string> Tags { get; init; } = [];
    public string? TaskType { get; init; }
    public string Status { get; init; } = "";
    public int Priority { get; init; } = 50;
    public UserTaskParticipantSummary? Assignee { get; init; }
    public string? CandidateSummary { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public bool IsOverdue { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? AssignedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public string? WorkflowDefinitionId { get; init; }
    public string? WorkflowDefinitionName { get; init; }
    public int? WorkflowDefinitionVersion { get; init; }
    public string? WorkflowInstanceId { get; init; }
    public string? WorkflowInstanceReference { get; init; }
    public string? HealthSeverity { get; init; }
    public string? HealthCode { get; init; }
    public IReadOnlyCollection<string> AllowedActions { get; init; } = [];
    public int Revision { get; init; }

    public bool IsTerminal => Status is "Completed" or "Cancelled" or "TimedOut";
    public bool IsTransitioning => Status is "Completing" or "Cancelling" or "TimingOut";
    public bool Allows(string action) => AllowedActions.Contains(action, StringComparer.OrdinalIgnoreCase);
}

public sealed record UserTaskDetail : UserTaskSummary
{
    public string? Instructions { get; init; }
    public JsonElement? Data { get; init; }
    public UserTaskDisclosure Disclosure { get; init; } = new();
    public UserTaskWorkflowContext? Workflow { get; init; }
    public UserTaskFormProjection? Form { get; init; }
    public IReadOnlyCollection<UserTaskFormAction> Actions { get; init; } = [];
    public string? Outcome { get; init; }
    public JsonElement? Response { get; init; }
    public UserTaskParticipantSummary? CompletedBy { get; init; }
}

public sealed record UserTaskDisclosure
{
    public bool CanViewProtected { get; init; }
    public bool CanViewWorkflow { get; init; }
    public bool CanViewHistory { get; init; }
    public bool GuestVisible { get; init; }
}

public sealed record UserTaskParticipantSummary
{
    public string Kind { get; init; } = "user";
    public string? Provider { get; init; }
    public string Id { get; init; } = "";
    public string? DisplayName { get; init; }

    /// <summary>Display label. Falls back to the opaque reference only when the directory supplied no name.</summary>
    public string Label => string.IsNullOrWhiteSpace(DisplayName) ? Id : DisplayName!;
}

public sealed record UserTaskWorkflowContext
{
    public string? DefinitionId { get; init; }
    public string? DefinitionName { get; init; }
    public int? DefinitionVersion { get; init; }
    public string? InstanceId { get; init; }
    public string? InstanceReference { get; init; }
}

public sealed record UserTaskFormProjection
{
    public string Provider { get; init; } = "";
    public string Key { get; init; } = "";
    public string? Version { get; init; }
    public IReadOnlyCollection<UserTaskFormField> Fields { get; init; } = [];
    public IReadOnlyCollection<UserTaskFormAction> Actions { get; init; } = [];
}

public sealed record UserTaskFormField
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string Type { get; init; } = "text";
    public bool Required { get; init; }
    public bool Masked { get; init; }
    public bool CanReveal { get; init; }
    /// <summary>Always absent for masked fields; those are read through the explicit reveal command.</summary>
    public JsonElement? Value { get; init; }
}

public sealed record UserTaskFormAction
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
}

public sealed record UserTaskEvent
{
    public string Id { get; init; } = "";
    public string Kind { get; init; } = "";
    public string? Summary { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string? ActorDisplayName { get; init; }
}

public sealed record UserTaskEventsResponse
{
    public IReadOnlyCollection<UserTaskEvent> Items { get; init; } = [];
    public string? NextCursor { get; init; }
}

/// <summary>
/// Command envelope. <c>operationId</c> is generated once per user submission and reused across retries so a
/// retry is recognised as the same command rather than accepted as a second one.
/// </summary>
public sealed record UserTaskMutationRequest(int ExpectedRevision, string? OperationId = null, string? Reason = null);

public sealed record UserTaskAssignRequest(int ExpectedRevision, UserTaskParticipantSummary Assignee, string? OperationId = null, string? Reason = null);

public sealed record UserTaskSchedulingUpdate(int ExpectedRevision, int? Priority = null, DateTimeOffset? DueAt = null, string? OperationId = null);

public sealed record UserTaskCompletionRequest(int ExpectedRevision, string OperationId, string ActionKey, JsonElement? Data = null);

public sealed record UserTaskCancelRequest(int ExpectedRevision, string OperationId, string Reason);

public sealed record UserTaskRevealFieldRequest(string FieldKey);

public sealed record UserTaskRevealFieldResponse
{
    public string FieldKey { get; init; } = "";
    public JsonElement? Value { get; init; }
}

public sealed record UserTaskOperationResponse
{
    public string? OperationId { get; init; }
    /// <summary><c>completed</c> for a synchronous change, <c>accepted</c> for an asynchronous terminal command.</summary>
    public string? Status { get; init; }
    public int Revision { get; init; }
    public UserTaskSummary? Task { get; init; }
}

/// <summary>The server's safe error body. The code drives client behavior; the message is display copy.</summary>
public sealed record UserTaskErrorResponse
{
    public string Code { get; init; } = "";
    public string Message { get; init; } = "";
}

public sealed record UserTaskParticipantSearchResponse
{
    public IReadOnlyCollection<UserTaskParticipantSummary> Items { get; init; } = [];
    public string? NextCursor { get; init; }
}

public sealed record UserTaskInvitationRequest
{
    public int ExpectedRevision { get; init; }
    public string VerifierName { get; init; } = "";
    public IReadOnlyCollection<string> AllowedActions { get; init; } = [];
    public string? Recipient { get; init; }
    public TimeSpan? Lifetime { get; init; }
    public string? OperationId { get; init; }
}

public sealed record UserTaskInvitationSummary
{
    public string Id { get; init; } = "";
    public string TaskId { get; init; } = "";
    public string? Recipient { get; init; }
    public string Status { get; init; } = "";
    public DateTimeOffset IssuedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public string? VerifierName { get; init; }
}

/// <summary>Issue result. It deliberately carries metadata only — the secret is delivered out of band.</summary>
public sealed record UserTaskInvitationIssueResult
{
    public UserTaskInvitationSummary? Invitation { get; init; }
    public string? OperationId { get; init; }
}

public sealed record UserTaskInvitationListResponse
{
    public IReadOnlyCollection<UserTaskInvitationSummary> Items { get; init; } = [];
}

public sealed record UserTaskInvalidation
{
    public string Kind { get; init; } = "";
    public string TaskId { get; init; } = "";
    public int Revision { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}

public sealed record UserTaskGuestChallenge
{
    public string ChallengeType { get; init; } = "code";
    public string Prompt { get; init; } = "";
    public bool RequiresCode { get; init; } = true;
}

public sealed record UserTaskGuestVerificationRequest(string? Code = null, string? State = null);

public sealed record UserTaskGuestSession
{
    public string? SessionCredential { get; init; }
    public string? TaskId { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
