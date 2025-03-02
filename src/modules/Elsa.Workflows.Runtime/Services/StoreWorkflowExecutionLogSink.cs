using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;
using Open.Linq.AsyncExtensions;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// This implementation saves <see cref="WorkflowExecutionLogRecord"/> directly through the store.
/// </summary>
public class StoreWorkflowExecutionLogSink(IWorkflowExecutionLogStore store, ILogRecordExtractor<WorkflowExecutionLogRecord> extractor, INotificationSender notificationSender) : ILogRecordSink<WorkflowExecutionLogRecord>
{
    /// <inheritdoc />
    public async Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
    {
        var records = await extractor.ExtractLogRecordsAsync(context).ToList();
        
        if(records.Count == 0)
            return;
        
        await store.AddManyAsync(records, context.CancellationToken);
        await notificationSender.SendAsync(new WorkflowExecutionLogUpdated(context), context.CancellationToken);
    }
}