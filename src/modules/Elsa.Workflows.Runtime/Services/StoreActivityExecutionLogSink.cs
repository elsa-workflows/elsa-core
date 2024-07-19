using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// This implementation saves <see cref="ActivityExecutionRecord"/> directly through the store.
/// </summary>
public class StoreActivityExecutionLogSink(IActivityExecutionStore activityExecutionStore, IActivityExecutionRecordExtractor extractor, INotificationSender notificationSender) 
    : IActivityExecutionLogSink
{
    /// <inheritdoc />
    public async Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var records = extractor.ExtractWorkflowExecutionLogs(context).ToList();
        await activityExecutionStore.SaveManyAsync(records, cancellationToken);
        await notificationSender.SendAsync(new ActivityExecutionLogUpdated(context, records), cancellationToken);
    }
}