using System.Text.Json;
using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Endpoints;

public sealed class ListUserTasksRequest
{
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 50;
    public string? Sort { get; set; }
    public string? Direction { get; set; }
    public UserTaskStatus? Status { get; set; }
    public int? PriorityFrom { get; set; }
    public int? PriorityTo { get; set; }
    public DateTimeOffset? DueFrom { get; set; }
    public DateTimeOffset? DueTo { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? Reference { get; set; }
    public string? TaskType { get; set; }
    public string? Search { get; set; }
    public bool IncludeTotalCount { get; set; }
}

public class UserTaskMutationApiRequest
{
    public int ExpectedRevision { get; set; }
    public string? OperationId { get; set; }
}

public sealed class AssignUserTaskApiRequest : UserTaskMutationApiRequest
{
    public ParticipantReference Assignee { get; set; } = null!;
    public string? Reason { get; set; }
}

public sealed class ScheduleUserTaskApiRequest : UserTaskMutationApiRequest
{
    public int? Priority { get; set; }
    public DateTimeOffset? DueAt { get; set; }
}

public sealed class CompleteUserTaskApiRequest
{
    public int ExpectedRevision { get; set; }
    public string OperationId { get; set; } = "";
    public string ActionKey { get; set; } = "";
    public JsonElement? Data { get; set; }
}

public sealed class CancelUserTaskApiRequest
{
    public int ExpectedRevision { get; set; }
    public string OperationId { get; set; } = "";
    public string Reason { get; set; } = "";
}

public sealed record UserTaskOperationResponse(UserTaskSummary Task, string OperationId, bool Accepted, string? ConflictCode);

public sealed class IssueUserTaskInvitationApiRequest
{
    public int ExpectedRevision { get; set; }
    public string VerifierName { get; set; } = "";
    public IReadOnlyCollection<string> AllowedActions { get; set; } = [];
    public string? Recipient { get; set; }
    public TimeSpan? Lifetime { get; set; }
    public string? OperationId { get; set; }
}

public sealed class VerifyUserTaskInvitationApiRequest
{
    public string? Code { get; set; }
    public string? State { get; set; }
}

public sealed class UserTaskParticipantLookupApiRequest
{
    public string? Search { get; set; }
    public UserTaskParticipantType? Type { get; set; }
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 50;
}
