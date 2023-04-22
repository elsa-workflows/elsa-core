using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Notifications;

namespace Elsa.Samples.RunTaskIntegration.Handlers;

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

        var args = (dynamic)notification.TaskPayload!;
        var foodName = args.Food;
        
        Console.WriteLine("Preparing {0}...", foodName);
        await Task.Delay(1000, cancellationToken);
        Console.WriteLine("Food is ready for delivery!");

        await _taskReporter.ReportCompletionAsync(notification.TaskId, foodName, cancellationToken);
    }
}