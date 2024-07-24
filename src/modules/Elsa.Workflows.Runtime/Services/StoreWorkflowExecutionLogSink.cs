using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// This implementation saves <see cref="WorkflowExecutionLogRecord"/> directly through the store.
/// </summary>
public class StoreWorkflowExecutionLogSink(IWorkflowExecutionLogStore store, ILogRecordExtractor<WorkflowExecutionLogRecord> extractor, INotificationSender notificationSender) : ILogRecordSink<WorkflowExecutionLogRecord>
{
    /// <inheritdoc />
    public async Task PersistExecutionLogsAsync(WorkflowExecutionContext context, CancellationToken cancellationToken)
    {
        var records = extractor.ExtractLogRecords(context).ToList();
        await store.AddManyAsync(records, context.CancellationTokens.SystemCancellationToken);
        await notificationSender.SendAsync(new WorkflowExecutionLogUpdated(context), context.CancellationTokens.SystemCancellationToken);
    }
}