using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.PublishingStrategies;

/// <summary>
/// Invokes event handlers in parallel and waits for the result.
/// </summary>
public class ParallelProcessingStrategy : IEventPublishingStrategy
{
    public async Task PublishAsync(INotification notification, INotificationHandler[] handlers, ILogger logger, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            var notificationType = notification.GetType();
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handleMethod = handlerType.GetMethod("HandleAsync")!;
            
            var task = (Task)handleMethod.Invoke(handler, new object?[] {notification, cancellationToken})!;
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }
}