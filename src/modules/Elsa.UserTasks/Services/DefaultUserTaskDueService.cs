using Elsa.Common;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.Workflows;

namespace Elsa.UserTasks.Services;

public sealed class DefaultUserTaskDueService(
    IUserTaskRepository repository,
    IUserTaskManager manager,
    IUserTaskNotificationSink notifications,
    IIdentityGenerator identityGenerator,
    ISystemClock clock) : IUserTaskDueService
{
    public async Task<int> MarkOverdueAsync(string tenantId, DateTimeOffset? now = null, CancellationToken cancellationToken = default)
    {
        var scanNow = now ?? clock.UtcNow;
        var cursor = (string?)null;
        var marked = 0;

        do
        {
            var due = await repository.QueryAsync(new UserTaskQuery
            {
                TenantId = tenantId,
                DueTo = scanNow,
                Cursor = cursor,
                Sort = "due",
                Limit = 200
            }, cancellationToken);

            foreach (var task in due.Items.Where(x => !x.IsTerminal && x.DueAt <= scanNow))
            {
                if (task.EnableTimeoutOutcome)
                {
                    try
                    {
                        var result = await manager.TimeoutAsync(tenantId, task.Id, task.Revision, scanNow, cancellationToken);
                        if (result.Accepted)
                            marked++;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        // The accepted transition remains durable and is repaired by reconciliation if
                        // bookmark delivery failed. Continue scanning other tasks.
                    }

                    continue;
                }

                if (task.IsOverdue || !await repository.TryMutateAsync(tenantId, task.Id, task.Revision, current =>
                    {
                        if (current.IsOverdue || current.IsTerminal)
                            return false;
                        current.IsOverdue = true;
                        current.Events.Add(new UserTaskEvent(identityGenerator.GenerateId(), tenantId, current.Id, current.Revision + 1,
                            "OverdueNotified", clock.UtcNow));
                        return true;
                    }, cancellationToken))
                    continue;

                marked++;
                var committed = await repository.GetAsync(tenantId, task.Id, cancellationToken);
                if (committed != null)
                    await notifications.PublishAsync(new UserTaskOverdue(tenantId, committed.Id, committed.Status, committed.Revision), cancellationToken);
            }

            cursor = due.NextCursor;
        }
        while (cursor != null);

        return marked;
    }
}
