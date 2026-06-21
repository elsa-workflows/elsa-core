using Elsa.Mediator.Contracts;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

public class DefaultCommitStateHandler(
    IWorkflowInstanceManager workflowInstanceManager,
    IBookmarksPersister bookmarkPersister,
    IVariablePersistenceManager variablePersistenceManager,
    IWorkflowCommitTransaction workflowCommitTransaction,
    IWorkflowCommitNotificationBuffer workflowCommitNotificationBuffer,
    INotificationSender notificationSender,
    ILogRecordSink<ActivityExecutionRecord> activityExecutionLogRecordSink,
    ILogRecordSink<WorkflowExecutionLogRecord> workflowExecutionLogRecordSink) : ICommitStateHandler
{
    public async Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        var workflowState = workflowInstanceManager.ExtractWorkflowState(workflowExecutionContext);
        await CommitAsync(workflowExecutionContext, workflowState, cancellationToken);
    }

    public async Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        WorkflowInstance? workflowInstance = null;
        using var notificationScope = workflowCommitNotificationBuffer.Begin();
        await workflowCommitTransaction.ExecuteAsync(async ct =>
        {
            var updateBookmarksRequest = new UpdateBookmarksRequest(workflowExecutionContext, workflowExecutionContext.BookmarksDiff, workflowExecutionContext.CorrelationId);
            await bookmarkPersister.PersistBookmarksAsync(updateBookmarksRequest);
            await activityExecutionLogRecordSink.PersistExecutionLogsAsync(workflowExecutionContext, ct);
            await workflowExecutionLogRecordSink.PersistExecutionLogsAsync(workflowExecutionContext, ct);
            await variablePersistenceManager.SaveVariablesAsync(workflowExecutionContext);
            workflowInstance = await workflowInstanceManager.SaveAsync(workflowState, ct);
        }, cancellationToken);

        ClearActivityExecutionContextTaint(workflowExecutionContext);
        workflowExecutionContext.ExecutionLog.Clear();
        workflowExecutionContext.ClearCompletedActivityExecutionContexts();
        await notificationScope.FlushAsync(cancellationToken);
        await workflowExecutionContext.ExecuteDeferredTasksAsync();
        await notificationSender.SendAsync(new WorkflowStateCommitted(workflowExecutionContext, workflowState, workflowInstance!), cancellationToken);
    }

    private static void ClearActivityExecutionContextTaint(WorkflowExecutionContext workflowExecutionContext)
    {
        foreach (var activityExecutionContext in workflowExecutionContext.ActivityExecutionContexts.Where(x => x.IsDirty))
            activityExecutionContext.ClearTaint();
    }
}