using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Models.Requests;

namespace Elsa.Workflows.Runtime.Services;

/// <inheritdoc />
public class TaskReporter : ITaskReporter
{
    private readonly IWorkflowDispatcher _workflowDispatcher;

    /// <summary>
    /// Constructor.
    /// </summary>
    public TaskReporter(IWorkflowDispatcher workflowDispatcher)
    {
        _workflowDispatcher = workflowDispatcher;
    }

    /// <inheritdoc />
    public async Task ReportCompletionAsync(string taskId, object? result = default, CancellationToken cancellationToken = default)
    {
        var bookmarkPayload = new RunTaskBookmarkPayload(taskId, default!);

        var input = new Dictionary<string, object>
        {
            [RunTask.InputKey] = result!
        };
        
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<RunTask>();
        var request = new DispatchResumeWorkflowsRequest(activityTypeName, bookmarkPayload, Input: input);
        await _workflowDispatcher.DispatchAsync(request, cancellationToken);
    }
}