using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class UserTaskService(IWorkflowResumer workflowResumer, IBookmarkQueue bookmarkQueue) : IUserTaskService
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<RunTask>();

    /// <inheritdoc />
    public Task<IEnumerable<RunWorkflowInstanceResponse>> ExecuteUserActionAsync(
        TriggerUserAction taskAction,
        CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(taskAction);
        var options = BuildResumeOptions(taskAction);
        return workflowResumer.ResumeAsync(filter, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DispatchUserActionAsync(
        TriggerUserAction taskAction,
        CancellationToken cancellationToken = default)
    {
        var item = new NewBookmarkQueueItem
        {
            ActivityTypeName = ActivityTypeName,
            WorkflowInstanceId = taskAction.WorkflowInstanceId,
            ActivityInstanceId = taskAction.ActivityInstanceId,
            CorrelationId = taskAction.CorrelationId,
            Options = BuildResumeOptions(taskAction)
        };

        await bookmarkQueue.EnqueueAsync(item, cancellationToken);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static BookmarkFilter BuildFilter(TriggerUserAction taskAction) =>
        new()
        {
            Name = ActivityTypeName,
            WorkflowInstanceId = taskAction.WorkflowInstanceId,
            ActivityInstanceId = taskAction.ActivityInstanceId,
            CorrelationId = taskAction.CorrelationId,
        };

    private static ResumeBookmarkOptions BuildResumeOptions(TriggerUserAction taskAction) =>
        new()
        {
            Input = new Dictionary<string, object>
            {
                [RunTask.InputKey] = taskAction.Action
            }
        };
}
