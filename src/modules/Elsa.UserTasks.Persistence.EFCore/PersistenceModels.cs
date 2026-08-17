using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Persistence.EFCore;

/// <summary>
/// EF records intentionally keep JSON payloads and participant references flattened so every relational
/// provider has the same schema. The public User Tasks aggregate is materialized by the repository.
/// </summary>
public enum UserTaskPersistenceCandidateSource
{
    DirectUser,
    DirectGroup,
    SnapshotMember,
    Invitation
}

public sealed class UserTaskRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = default!;
    public string WorkflowDefinitionId { get; set; } = default!;
    public string WorkflowInstanceId { get; set; } = default!;
    public string ActivityInstanceId { get; set; } = default!;
    public string BookmarkId { get; set; } = default!;
    public string MaterializationKey { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Summary { get; set; }
    public string? Reference { get; set; }
    public string? TaskType { get; set; }
    public string TagsJson { get; set; } = "[]";
    public string? RequesterProvider { get; set; }
    public string? RequesterType { get; set; }
    public string? RequesterId { get; set; }
    public string? RequesterDisplayName { get; set; }
    public int Priority { get; set; } = 50;
    public DateTimeOffset? DueAt { get; set; }
    public bool IsOverdue { get; set; }
    public UserTaskStatus Status { get; set; }
    public bool TimeoutEnabled { get; set; }
    public bool CancellationEnabled { get; set; }
    public bool AllowManagerExclusionOverride { get; set; }
    public UserTaskMembershipResolutionMode? MembershipResolutionMode { get; set; }
    public string? AssigneeProvider { get; set; }
    public string? AssigneeType { get; set; }
    public string? AssigneeId { get; set; }
    public string? AssigneeDisplayName { get; set; }
    public string InstructionsJson { get; set; } = "null";
    public string TaskDataJson { get; set; } = "null";
    public string? FormReferenceJson { get; set; }
    public string? PinnedFormJson { get; set; }
    public string? ActionsJson { get; set; }
    public string InvitationDefinitionsJson { get; set; } = "[]";
    public string HealthIssuesJson { get; set; } = "[]";
    public UserTaskHealthSeverity? HealthSeverity { get; set; }
    public string? HealthCode { get; set; }
    public string? HealthMessage { get; set; }
    public string? CompletionActionKey { get; set; }
    public string? CompletionDataJson { get; set; }
    public string? CompletionActorJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AssignedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int Revision { get; set; }
    public long? CreatedFromBookmarkRevision { get; set; }
}

public sealed class UserTaskCandidateRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = default!;
    public string TaskId { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string ParticipantKey { get; set; } = default!;
    public UserTaskParticipantType ParticipantType { get; set; }
    public string ParticipantId { get; set; } = default!;
    public string? DisplayName { get; set; }
    public UserTaskPersistenceCandidateSource Source { get; set; }
    public string? SourceGroupProvider { get; set; }
    public string? SourceGroupId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class UserTaskSnapshotMemberRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = default!;
    public string TaskId { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string ParticipantKey { get; set; } = default!;
    public UserTaskParticipantType ParticipantType { get; set; } = UserTaskParticipantType.User;
    public string ParticipantId { get; set; } = default!;
    public string? SourceGroupProvider { get; set; }
    public string? SourceGroupId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class UserTaskExclusionRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = default!;
    public string TaskId { get; set; } = default!;
    public string Provider { get; set; } = default!;
    public string ParticipantKey { get; set; } = default!;
    public UserTaskParticipantType ParticipantType { get; set; } = UserTaskParticipantType.User;
    public string ParticipantId { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class UserTaskEventRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = default!;
    public string TaskId { get; set; } = default!;
    public int Revision { get; set; }
    public string EventType { get; set; } = default!;
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public string? OperationId { get; set; }
    public string? ActorProvider { get; set; }
    public string? ActorType { get; set; }
    public string? ActorId { get; set; }
    public string? ActorJson { get; set; }
    public string? Reason { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public enum UserTaskPersistenceOperationStatus
{
    Pending,
    Enqueued,
    Completed,
    Failed,
    Abandoned
}

public sealed class UserTaskOperationRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = default!;
    public string TaskId { get; set; } = default!;
    public string OperationId { get; set; } = default!;
    public string Kind { get; set; } = default!;
    public int ExpectedRevision { get; set; }
    public string RequestHash { get; set; } = default!;
    public UserTaskPersistenceOperationStatus Status { get; set; }
    public int Attempts { get; set; }
    public string? SafeResultJson { get; set; }
    public string? ProtectedPayloadJson { get; set; }
    public string? ActionKey { get; set; }
    public string? ErrorCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class UserTaskInvitationRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = default!;
    public string TaskId { get; set; } = default!;
    public string? SiblingGroupId { get; set; }
    public string? RecipientJson { get; set; }
    public string TokenHash { get; set; } = default!;
    public string VerifierProvider { get; set; } = default!;
    public string? ChallengeJson { get; set; }
    public UserTaskInvitationStatus Status { get; set; }
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public enum UserTaskPersistenceDeliveryStatus
{
    Pending,
    Processing,
    Delivered,
    Failed,
    Expired
}

public sealed class UserTaskInvitationDeliveryRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = default!;
    public string TaskId { get; set; } = default!;
    public string InvitationId { get; set; } = default!;
    public string DispatcherProvider { get; set; } = default!;
    public string EncryptedToken { get; set; } = default!;
    public string? DeliveryMetadataJson { get; set; }
    public UserTaskPersistenceDeliveryStatus Status { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset AvailableAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? LastErrorCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeliveredAt { get; set; }
}

public sealed class UserTaskGuestSessionRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string TenantId { get; set; } = default!;
    public string TaskId { get; set; } = default!;
    public string InvitationId { get; set; } = default!;
    public string SessionTokenHash { get; set; } = default!;
    public string GuestParticipantJson { get; set; } = default!;
    public string CapabilitiesJson { get; set; } = "[]";
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
