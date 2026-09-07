using System.Text.Json;
using Elsa.UserTasks.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.UserTasks.Endpoints;

public sealed class ListUserTasksRequest
{
    /// <summary>One of <c>assigned</c>, <c>available</c>, <c>history</c>, <c>all</c>, or <c>needs-attention</c>.</summary>
    public string? Scope { get; set; }
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 50;
    public string? Sort { get; set; }
    public string? Direction { get; set; }
    /// <summary>Repeatable. Unknown values are ignored rather than rejected.</summary>
    public string[]? Status { get; set; }
    public int? PriorityFrom { get; set; }
    public int? PriorityTo { get; set; }
    /// <summary>A derived due filter: <c>overdue</c>, <c>today</c>, <c>thisWeek</c>, or <c>noDueDate</c>.</summary>
    public string? Due { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? WorkflowDefinitionId { get; set; }
    public string? WorkflowInstanceId { get; set; }
    public string? Reference { get; set; }
    public string? TaskType { get; set; }
    public string? Search { get; set; }
    public bool IncludeTotalCount { get; set; }
}

public sealed record UserTaskListResponse(IReadOnlyCollection<UserTaskSummary> Items, string? NextCursor, int? TotalCount);

public class UserTaskMutationApiRequest
{
    public int ExpectedRevision { get; set; }
    /// <summary>Reused verbatim across retries of one user action so a retry is idempotent, never a second command.</summary>
    public string? OperationId { get; set; }
    public string? Reason { get; set; }
}

public sealed class AssignUserTaskApiRequest : UserTaskMutationApiRequest
{
    public UserTaskParticipantSummary Assignee { get; set; } = null!;
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

public sealed class RevealUserTaskFieldApiRequest
{
    public string FieldKey { get; set; } = "";
}

public sealed record RevealUserTaskFieldResponse(string FieldKey, JsonElement? Value);

/// <summary>
/// The uniform command envelope. <c>status</c> is <c>completed</c> for a synchronous state change and
/// <c>accepted</c> for a terminal command that resumes the workflow asynchronously.
/// </summary>
public sealed record UserTaskOperationResponse(string OperationId, string Status, int Revision, UserTaskSummary? Task);

/// <summary>A safe error body. Codes are stable; messages never carry exception or payload detail.</summary>
public sealed record UserTaskErrorResponse(string Code, string Message);

public sealed class IssueUserTaskInvitationApiRequest
{
    public int ExpectedRevision { get; set; }
    public string VerifierName { get; set; } = "";
    public IReadOnlyCollection<string> AllowedActions { get; set; } = [];
    public string? Recipient { get; set; }
    public TimeSpan? Lifetime { get; set; }
    public string? OperationId { get; set; }
}

public sealed record UserTaskInvitationListResponse(IReadOnlyCollection<UserTaskInvitationSummary> Items);

public sealed class VerifyUserTaskInvitationApiRequest
{
    public string? Code { get; set; }
    public string? State { get; set; }
}

public sealed record UserTaskGuestSessionResponse(string? SessionCredential, string? TaskId, DateTimeOffset? ExpiresAt);

public sealed class UserTaskParticipantLookupApiRequest
{
    public string? Search { get; set; }
    public string? Type { get; set; }
    public string? Cursor { get; set; }
    public int Limit { get; set; } = 50;
}

public sealed record UserTaskParticipantSearchResponse(IReadOnlyCollection<UserTaskParticipantSummary> Items, string? NextCursor);

/// <summary>
/// Maps the domain's conflict codes onto HTTP semantics and safe copy. Keeping this in one place stops an
/// individual endpoint from inventing a status that leaks whether a task exists.
/// </summary>
internal static class UserTaskErrors
{
    public static int StatusCodeFor(string code) => code switch
    {
        // A denied command answers exactly like a missing one. The transport layer already rejected a
        // caller without the module permission, so reaching a domain denial means the relationship check
        // failed — and answering 403 there would tell an ID-guessing caller that the task exists.
        "not-found" or "forbidden" => StatusCodes.Status404NotFound,
        "revision-conflict" or "idempotency-conflict" or "terminal" or "transition-in-progress" or "not-claimable" => StatusCodes.Status409Conflict,
        "invalid-action" or "reserved-action" or "form-required" or "form-invalid" or "form-resolution-failed"
            or "form-provider-missing" or "payload-too-large" or "cancellation-disabled" or "reason-required"
            or "invalid-priority" or "excluded-assignee" or "cross-tenant-assignee" or "timeout-not-due" => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status409Conflict
    };

    public static UserTaskErrorResponse Describe(string code) => new(code, code switch
    {
        // Same copy for both, so the message cannot re-introduce the distinction the status code hides.
        "not-found" or "forbidden" => "This task is no longer available.",
        "revision-conflict" => "The task changed since it was loaded. Reload it and try again.",
        "idempotency-conflict" => "That operation ID was already used with a different request.",
        "terminal" => "This task has already reached a final state.",
        "transition-in-progress" => "This task is already finishing. Reload it to see the result.",
        "not-claimable" => "This task is no longer available to claim.",
        "invalid-action" => "That action is not configured for this task.",
        "reserved-action" => "That action is reserved and cannot be selected.",
        "form-required" => "This task does not accept a response payload.",
        "form-invalid" => "The response did not pass validation.",
        "form-resolution-failed" or "form-provider-missing" => "This task's form is unavailable. Ask an operator to retry resolution.",
        "payload-too-large" => "The response is too large.",
        "cancellation-disabled" => "Cancellation is not enabled for this task.",
        "reason-required" => "A reason is required.",
        "invalid-priority" => "Priority must be between 0 and 100.",
        "excluded-assignee" => "That participant is excluded from this task.",
        "cross-tenant-assignee" => "That participant belongs to a different tenant.",
        "timeout-not-due" => "This task is not due for timeout.",
        _ => "The request could not be completed."
    });
}
