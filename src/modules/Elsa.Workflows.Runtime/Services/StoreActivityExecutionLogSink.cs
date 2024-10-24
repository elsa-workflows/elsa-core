using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// This implementation saves <see cref="ActivityExecutionRecord"/> directly through the store.
/// </summary>
public class StoreActivityExecutionLogSink(IActivityExecutionStore activityExecutionStore, ILogRecordExtractor<ActivityExecutionRecord> extractor, INotificationSender notificationSender) 
    : ILogRecordSink<ActivityExecutionRecord>
{
    /// <inheritdoc />
    public async Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default)
    {
        var records = await extractor.ExtractLogRecordsAsync(context).ToList();
        await activityExecutionStore.SaveManyAsync(records, cancellationToken);
        await notificationSender.SendAsync(new ActivityExecutionLogUpdated(context, records), cancellationToken);
    }
}