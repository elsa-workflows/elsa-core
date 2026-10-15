using Elsa.Mediator.Contexts;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.PublishingStrategies;

/// <summary>
/// Invokes event handlers in parallel and does not wait for the result.
/// </summary>
public class BackgroundProcessingStrategy : IEventPublishingStrategy
{
    /// <inheritdoc />
    public async Task PublishAsync(NotificationStrategyContext context)
    {
        var notificationsChannel = context.ServiceProvider.GetRequiredService<INotificationsChannel>();
        var notificationContext = context.NotificationContext;
        var queuedContext = new NotificationContext(
            notificationContext.Notification,
            NotificationStrategy.Sequential,
            notificationContext.ServiceProvider,
            notificationContext.CancellationToken);

        await notificationsChannel.Writer.WriteAsync(queuedContext, context.CancellationToken);
    }
}
