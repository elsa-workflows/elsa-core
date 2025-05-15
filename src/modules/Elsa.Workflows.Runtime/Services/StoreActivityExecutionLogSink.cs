using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// This implementation saves <see cref="ActivityExecutionRecord"/> directly through the store.
/// </summary>
public class StoreActivityExecutionLogSink(
    IActivityExecutionStore activityExecutionStore,
    IActivityExecutionMapper mapper,
    INotificationSender notificationSender,
    ILogger<StoreActivityExecutionLogSink> logger)
    : ILogRecordSink<ActivityExecutionRecord>
{
    /// <inheritdoc />
    public async Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        // Select tainted activity execution contexts to avoid saving untainted ones multiple times.
        var activityExecutionContexts = context.ActivityExecutionContexts.Where(x => x.IsDirty).ToList();

        if (activityExecutionContexts.Count == 0)
        {
            logger.LogDebug("No activity execution contexts to save.");
            return;
        }

        var records = activityExecutionContexts.Select(mapper.Map).ToList();
        
        logger.LogDebug("Saving {Count} activity execution records.", records.Count);
        await activityExecutionStore.SaveManyAsync(records, cancellationToken);
        logger.LogDebug("Saved {Count} activity execution records.", records.Count);

        // Untaint activity execution contexts.
        foreach (var activityExecutionContext in activityExecutionContexts)
            activityExecutionContext.ClearTaint();

        await notificationSender.SendAsync(new ActivityExecutionLogUpdated(context, records), cancellationToken);
    }
}