using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Samples.AspNet.RunTaskIntegration.Handlers;

public class OrderFoodTaskHandler : INotificationHandler<RunTaskRequest>
{
    private readonly ITaskReporter _taskReporter;

    public OrderFoodTaskHandler(ITaskReporter taskReporter)
    {
        _taskReporter = taskReporter;
    }

    public async Task HandleAsync(RunTaskRequest notification, CancellationToken cancellationToken)
    {
        if (notification.TaskName != "OrderFood")
            return;

        var args = notification.TaskPayload!;
        var foodName = args.GetValue<string>("Food");

        Console.WriteLine("Preparing {0}...", foodName);
        await Task.Delay(1000, cancellationToken);
        Console.WriteLine("Food is ready for delivery!");

        await _taskReporter.ReportCompletionAsync(notification.TaskId, foodName, cancellationToken);
    }
}