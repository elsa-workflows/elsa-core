using Elsa.Mediator.Contracts;

namespace Elsa.Mediator.PublishingStrategies;

/// <summary>
/// Invokes event handlers in parallel and waits for the result.
/// </summary>
public class ParallelProcessingStrategy : IEventPublishingStrategy
{
    /// <inheritdoc />
    public async Task PublishAsync(PublishContext context)
    {
        var tasks = new List<Task>();
        var notification = context.Notification;
        var cancellationToken = context.CancellationToken;

        foreach (var handler in context.Handlers)
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