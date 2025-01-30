using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

public class DefaultCommitStateHandler(
    IWorkflowInstanceManager workflowInstanceManager,
    IBookmarksPersister bookmarkPersister,
    IVariablePersistenceManager variablePersistenceManager,
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
        var updateBookmarksRequest = new UpdateBookmarksRequest(workflowExecutionContext, workflowExecutionContext.BookmarksDiff, workflowExecutionContext.CorrelationId);
        await bookmarkPersister.PersistBookmarksAsync(updateBookmarksRequest);
        await activityExecutionLogRecordSink.PersistExecutionLogsAsync(workflowExecutionContext, cancellationToken);
        await workflowExecutionLogRecordSink.PersistExecutionLogsAsync(workflowExecutionContext, cancellationToken);
        await variablePersistenceManager.SaveVariablesAsync(workflowExecutionContext);
        await workflowInstanceManager.SaveAsync(workflowState, cancellationToken);
        workflowExecutionContext.ExecutionLog.Clear();
        workflowExecutionContext.ClearCompletedActivityExecutionContexts();
        await workflowExecutionContext.ExecuteDeferredTasksAsync();
    }
}