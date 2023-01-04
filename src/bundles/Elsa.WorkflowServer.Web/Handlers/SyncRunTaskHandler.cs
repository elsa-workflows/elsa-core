using System.Dynamic;
using Elsa.Extensions;
using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Bookmarks;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.WorkflowServer.Web.Handlers;

public class OrderFoodTaskHandler : INotificationHandler<RunTaskRequest>
{
    private readonly IWorkflowRuntime _workflowRuntime;

    public OrderFoodTaskHandler(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }
    
    public async Task HandleAsync(RunTaskRequest notification, CancellationToken cancellationToken)
    {
        if (notification.TaskName != "OrderFood")
            return;

        var args = (IDictionary<string, object>)notification.TaskParams!;
        var foodName = args["Food"];
        
        Console.WriteLine("Preparing {0}...", foodName);
        await Task.Delay(1000, cancellationToken);
        Console.WriteLine("Food is ready for delivery!");

        var bookmarkPayload = new RunTaskBookmarkPayload(notification.TaskId, notification.TaskName);
        
        var input = new Dictionary<string, object>
        {
            [RunTask.InputKey] = foodName
        };

        var options = new TriggerWorkflowsRuntimeOptions(Input: input);
        await _workflowRuntime.TriggerWorkflowsAsync<RunTask>(bookmarkPayload, options, cancellationToken);
    }
}