using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// This implementation saves <see cref="ActivityExecutionRecord"/> directly through the store.
/// </summary>
public class StoreActivityExecutionLogSink(
    IActivityExecutionStore activityExecutionStore,
    IActivityExecutionMapper mapper,
    INotificationSender notificationSender)
    : ILogRecordSink<ActivityExecutionRecord>
{
    /// <inheritdoc />
    public async Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        // Select tainted activity execution contexts to avoid saving untainted ones multiple times.
        var activityExecutionContexts = context.ActivityExecutionContexts.Where(x => x.IsDirty).ToList();

        if (activityExecutionContexts.Count == 0)
            return;

        var tasks = activityExecutionContexts.Select(mapper.MapAsync).ToList();
        var records = await Task.WhenAll(tasks);
        await activityExecutionStore.SaveManyAsync(records, cancellationToken);

        // Untaint activity execution contexts.
        foreach (var activityExecutionContext in activityExecutionContexts)
            activityExecutionContext.ClearTaint();

        await notificationSender.SendAsync(new ActivityExecutionLogUpdated(context, records), cancellationToken);
    }
}