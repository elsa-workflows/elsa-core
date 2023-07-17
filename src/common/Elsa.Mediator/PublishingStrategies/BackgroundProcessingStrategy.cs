using Elsa.Mediator.Contexts;
using Elsa.Mediator.Contracts;
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

        await notificationsChannel.Writer.WriteAsync(context.Notification, context.CancellationToken);
    }
}