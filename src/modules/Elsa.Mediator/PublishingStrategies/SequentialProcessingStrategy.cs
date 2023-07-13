using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.PublishingStrategies;

/// <summary>
/// Invokes event handlers in sequence and waits for the result.
/// </summary>
public class SequentialProcessingStrategy : IEventPublishingStrategy
{
    /// <inheritdoc />
    public async Task PublishAsync(PublishContext context)
    {
        var notification = context.Notification;
        var cancellationToken = context.CancellationToken;

        foreach (var handler in context.Handlers)
        {
            var notificationType = notification.GetType();
            var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
            var handleMethod = handlerType.GetMethod("HandleAsync")!;

            var task = (Task)handleMethod.Invoke(handler, new object?[] { notification, cancellationToken })!;
            await task;
        }
    }
}