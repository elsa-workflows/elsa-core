using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.PublishingStrategies;

/// <summary>
/// Invokes event handlers in parallel and does not wait for the result.
/// </summary>
public class FireAndForgetStrategy : IEventPublishingStrategy
{
    public Task PublishAsync(INotification notification, INotificationHandler[] handlers, ILogger logger, CancellationToken cancellationToken = default)
    {
        var notificationType = notification.GetType();

        foreach (var handler in handlers)
        {
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handleMethod = handlerType.GetMethod("HandleAsync")!;
            
            var task = (Task)handleMethod.Invoke(handler, new object?[] {notification, cancellationToken})!;
            var _ = task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    logger.LogError(t.Exception, "An error occurred while handling notification {NotificationType}", notificationType);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        return Task.CompletedTask;
    }
}