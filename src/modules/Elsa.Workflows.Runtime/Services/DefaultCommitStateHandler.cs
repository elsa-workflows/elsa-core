using System.Runtime.ExceptionServices;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.State;

namespace Elsa.Workflows.Runtime;

public class DefaultCommitStateHandler(
    IWorkflowInstanceManager workflowInstanceManager,
    IBookmarksPersister bookmarkPersister,
    IVariablePersistenceManager variablePersistenceManager,
    IMediator mediator,
    ILogRecordSink<ActivityExecutionRecord> activityExecutionLogRecordSink,
    ILogRecordSink<WorkflowExecutionLogRecord> workflowExecutionLogRecordSink) : ICommitStateHandler
{
    public async Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        var workflowState = workflowInstanceManager.ExtractWorkflowState(workflowExecutionContext);
        await CommitAsync(workflowExecutionContext, workflowState, cancellationToken);
    }

    /// <summary>
    /// Persists workflow state, then attempts every post-save step even if a preceding notification or deferred task fails.
    /// </summary>
    /// <remarks>
    /// A single nonfatal error is rethrown unchanged; multiple errors are aggregated in execution order.
    /// After a post-save failure, the final commit notification is attempted without caller cancellation.
    /// Out-of-memory and stack-overflow failures propagate immediately.
    /// </remarks>
    public async Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        var updateBookmarksRequest = new UpdateBookmarksRequest(workflowExecutionContext, workflowExecutionContext.BookmarksDiff, workflowExecutionContext.CorrelationId);
        await bookmarkPersister.PersistBookmarksAsync(updateBookmarksRequest);
        await activityExecutionLogRecordSink.PersistExecutionLogsAsync(workflowExecutionContext, cancellationToken);
        await workflowExecutionLogRecordSink.PersistExecutionLogsAsync(workflowExecutionContext, cancellationToken);
        await variablePersistenceManager.SaveVariablesAsync(workflowExecutionContext);
        var workflowInstance = await workflowInstanceManager.SaveAsync(workflowState, cancellationToken);
        var failures = new List<ExceptionDispatchInfo>();
        await RunPostSaveStepAsync(() => mediator.SendAsync(new WorkflowBookmarksCommitted(updateBookmarksRequest.Diff), cancellationToken), failures);
        await RunPostSaveStepAsync(() =>
        {
            workflowExecutionContext.ExecutionLog.Clear();
            workflowExecutionContext.ClearCompletedActivityExecutionContexts();
            return Task.CompletedTask;
        }, failures);
        await RunPostSaveStepAsync(() => workflowExecutionContext.ExecuteDeferredTasksAsync(), failures);
        var completionToken = failures.Count > 0 ? CancellationToken.None : cancellationToken;
        await RunPostSaveStepAsync(() => mediator.SendAsync(new WorkflowStateCommitted(workflowExecutionContext, workflowState, workflowInstance), completionToken), failures);

        if (failures.Count == 1)
            failures[0].Throw();
        if (failures.Count > 1)
            throw new AggregateException("Workflow state was saved, but post-save work failed.", failures.Select(x => x.SourceException));
    }

    /// <summary>
    /// Records a recoverable post-save error so later completion steps still run, preserving the original exception and stack.
    /// </summary>
    private static async Task RunPostSaveStepAsync(Func<Task> step, ICollection<ExceptionDispatchInfo> failures)
    {
        try
        {
            await step();
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            failures.Add(ExceptionDispatchInfo.Capture(exception));
        }
    }
}