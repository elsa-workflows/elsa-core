using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.PublishingStrategies;

/// <summary>
/// Invokes event handlers in parallel and does not wait for the result.
/// </summary>
public class FireAndForgetStrategy : IEventPublishingStrategy
{
    /// <inheritdoc />
    public Task PublishAsync(PublishContext context)
    {
        var notification = context.Notification;
        var notificationType = notification.GetType();
        var handlers = context.Handlers;
        var logger = context.Logger;
        var cancellationToken = context.CancellationToken;

        foreach (var handler in handlers)
        {
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handleMethod = handlerType.GetMethod("HandleAsync")!;

            var task = (Task)handleMethod.Invoke(handler, new object?[] { notification, cancellationToken })!;
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