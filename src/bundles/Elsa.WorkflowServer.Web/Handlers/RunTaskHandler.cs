using Elsa.Mediator.Services;
using Elsa.Workflows.Runtime.Notifications;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.WorkflowServer.Web.Handlers;

public class RunTaskHandler : INotificationHandler<RunTaskRequest>
{
    private readonly ITaskReporter _taskReporter;

    public RunTaskHandler(ITaskReporter taskReporter)
    {
        _taskReporter = taskReporter;
    }
    
    public async Task HandleAsync(RunTaskRequest notification, CancellationToken cancellationToken)
    {
        await _taskReporter.ReportCompletionAsync(notification.TaskId, "Pizza", cancellationToken);
    }
}