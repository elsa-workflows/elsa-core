using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.PublishingStrategies;

/// <summary>
/// Invokes event handlers in sequence and waits for the result.
/// </summary>
public class SequentialProcessingStrategy : IEventPublishingStrategy
{
    public async Task PublishAsync(INotification notification, INotificationHandler[] handlers, ILogger logger, CancellationToken cancellationToken = default)
    {
        foreach (var handler in handlers)
        {
            var notificationType = notification.GetType();
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handleMethod = handlerType.GetMethod("HandleAsync")!;
            
            var task = (Task)handleMethod.Invoke(handler, new object?[] {notification, cancellationToken})!;
            await task;
        }
    }
}