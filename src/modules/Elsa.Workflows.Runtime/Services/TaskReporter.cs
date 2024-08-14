using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models;
using Elsa.Workflows.Runtime.Parameters;

namespace Elsa.Workflows.Runtime;

/// <inheritdoc />
public class TaskReporter(IWorkflowInbox workflowInbox) : ITaskReporter
{
    /// <inheritdoc />
    public async Task ReportCompletionAsync(string taskId, object? result = default, CancellationToken cancellationToken = default)
    {
        var bookmarkPayload = new RunTaskBookmarkPayload(taskId, default!);

        var input = new Dictionary<string, object>
        {
            [RunTask.InputKey] = result!
        };
        
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<RunTask>();
        var message = new NewWorkflowInboxMessage
        {
            ActivityTypeName = activityTypeName,
            BookmarkPayload = bookmarkPayload,
            Input = input,
        };
        var options = new WorkflowInboxMessageDeliveryParams
        {
            DispatchAsynchronously = true
        };
        await workflowInbox.SubmitAsync(message, options, cancellationToken);
    }
}