using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.Workflows;
using Elsa.Common;

namespace Elsa.UserTasks.Services;

public sealed class DefaultUserTaskProjectionService(IUserTaskManager manager, IUserTaskRepository repository, IUserTaskNotificationSink notifications, IIdentityGenerator identityGenerator, ISystemClock clock) : IUserTaskProjectionService
{
    public async Task ProjectCommittedBookmarksAsync(IReadOnlyCollection<UserTaskMaterialization> materializations, CancellationToken cancellationToken = default)
    {
        foreach (var materialization in materializations)
            await manager.ProjectAsync(materialization, cancellationToken);
    }

    public async Task FinalizeBookmarkRemovalAsync(UserTaskBookmarkRemoval removal, CancellationToken cancellationToken = default)
    {
        var task = await repository.GetAsync(removal.TenantId, removal.TaskId, cancellationToken);
        if (task == null || task.BookmarkId != removal.BookmarkId)
            return;
        var expectedRevision = task.Revision;
        var terminalStatus = task.Status switch
        {
            UserTaskStatus.Completing => UserTaskStatus.Completed,
            UserTaskStatus.TimingOut => UserTaskStatus.TimedOut,
            UserTaskStatus.Cancelling => UserTaskStatus.Cancelled,
            _ when !task.IsTerminal => UserTaskStatus.Cancelled,
            _ => (UserTaskStatus?)null
        };
        if (terminalStatus == null)
            return;
        var expectedOperationKind = task.Status switch
        {
            UserTaskStatus.Completing => UserTaskOperationKind.Complete,
            UserTaskStatus.TimingOut => UserTaskOperationKind.Timeout,
            UserTaskStatus.Cancelling => UserTaskOperationKind.Cancel,
            _ => (UserTaskOperationKind?)null
        };
        var operation = expectedOperationKind == null
            ? null
            : task.Operations.LastOrDefault(x => x.Status == UserTaskOperationStatus.Accepted && x.Kind == expectedOperationKind.Value);
        if (task.Status is (UserTaskStatus.Completing or UserTaskStatus.TimingOut or UserTaskStatus.Cancelling) && operation == null)
            return;
        if (operation != null)
        {
            var index = task.Operations.IndexOf(operation);
            task.Operations[index] = operation with { Status = UserTaskOperationStatus.Completed, UpdatedAt = clock.UtcNow };
        }
        task.Status = terminalStatus.Value;
        task.CompletedAt ??= clock.UtcNow;
        task.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), task.TenantId, task.Id, expectedRevision + 1,
            terminalStatus.Value.ToString(), clock.UtcNow, operation == null ? null : task.CompletedBy, operation?.OperationId,
            Metadata: operation?.ActionKey == null ? null : new Dictionary<string, object?> { ["actionKey"] = operation.ActionKey }));
        try
        {
            await repository.SaveAsync(task, expectedRevision, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return;
        }
        var committed = await repository.GetAsync(task.TenantId, task.Id, cancellationToken) ?? task;
        await notifications.PublishAsync(terminalStatus.Value switch
        {
            UserTaskStatus.Completed => new UserTaskCompleted(committed.TenantId, committed.Id, committed.Status, committed.Revision),
            UserTaskStatus.TimedOut => new UserTaskTimedOut(committed.TenantId, committed.Id, committed.Status, committed.Revision),
            _ => new UserTaskCancelled(committed.TenantId, committed.Id, committed.Status, committed.Revision)
        }, cancellationToken);
    }
}
