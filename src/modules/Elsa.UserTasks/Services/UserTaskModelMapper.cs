using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Services;

public static class UserTaskModelMapper
{
    public static async Task<UserTaskSummary> ToSummaryAsync(UserTask task, UserTaskActor actor, IUserTaskAccessPolicy policy, CancellationToken cancellationToken = default)
    {
        var operations = Enum.GetValues<UserTaskAccessOperation>()
            .Where(x => x is UserTaskAccessOperation.Claim or UserTaskAccessOperation.Release or UserTaskAccessOperation.Assign
                or UserTaskAccessOperation.UpdateScheduling or UserTaskAccessOperation.Complete or UserTaskAccessOperation.Cancel
                or UserTaskAccessOperation.IssueInvitation or UserTaskAccessOperation.RetryResolution)
            .ToArray();
        var allowed = new List<string>();
        foreach (var operation in operations)
        {
            if (!await policy.AuthorizeAsync(task, actor, operation, cancellationToken))
                continue;
            allowed.Add(operation switch
            {
                UserTaskAccessOperation.UpdateScheduling => "updateScheduling",
                UserTaskAccessOperation.IssueInvitation => "invite",
                UserTaskAccessOperation.RetryResolution => "retryResolution",
                _ => char.ToLowerInvariant(operation.ToString()[0]) + operation.ToString()[1..]
            });
        }
        var dataAccess = task.IsTerminal
            ? task.CompletedBy?.Matches(actor.Subject) == true || actor.IsManager ? "protected" : "summary"
            : task.Assignee?.Matches(actor.Subject) == true || actor.IsManager ? "protected" : "summary";
        var healthVisible = actor.IsManager || task.HealthSeverity != UserTaskHealthSeverity.Blocking;
        return new UserTaskSummary(task.Id, task.TenantId, task.Title, task.Summary, task.Reference, task.Tags.ToArray(), task.TaskType,
            task.Status, task.Assignee, task.Priority, task.DueAt, task.IsOverdue, task.CreatedAt, task.AssignedAt, task.CompletedAt,
            task.Revision, healthVisible ? task.HealthSeverity : null, healthVisible ? task.HealthCode : null, allowed, dataAccess);
    }

    public static UserTaskDetail ToDetail(UserTask task, UserTaskSummary summary, bool protectedData, bool includeActionsAndEvents) => new(
        summary,
        protectedData ? task.Instructions : null,
        protectedData ? task.TaskData : null,
        protectedData ? task.RequestedForm : null,
        protectedData ? task.PinnedForm : null,
        includeActionsAndEvents ? task.Actions.ToArray() : [],
        protectedData ? task.CompletionData : null,
        protectedData ? task.CompletedBy : null,
        includeActionsAndEvents ? task.Events.ToArray() : []);

    public static async Task<UserTaskCapabilities> ToCapabilitiesAsync(UserTask task, UserTaskActor actor, IUserTaskAccessPolicy policy, CancellationToken cancellationToken = default)
    {
        var summary = await ToSummaryAsync(task, actor, policy, cancellationToken);
        return new UserTaskCapabilities(task.Id, task.Revision, summary.AllowedActions,
            await policy.AuthorizeAsync(task, actor, UserTaskAccessOperation.ReadProtected, cancellationToken), actor.IsManager);
    }
}
