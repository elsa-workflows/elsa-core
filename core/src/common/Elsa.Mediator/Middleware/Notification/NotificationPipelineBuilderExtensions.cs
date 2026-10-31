using Elsa.Mediator.Middleware.Notification.Components;
using Elsa.Mediator.Middleware.Notification.Contracts;

namespace Elsa.Mediator.Middleware.Notification;

/// <summary>
/// Provides a set of static methods for building a notification pipeline.
/// </summary>
public static class NotificationPipelineBuilderExtensions
{
    /// <summary>
    /// Adds a notification handler invoker middleware to the notification pipeline.
    /// </summary>
    /// <param name="builder">The notification pipeline builder.</param>
    public static INotificationPipelineBuilder UseNotificationHandlers(this INotificationPipelineBuilder builder) => builder.UseMiddleware<NotificationHandlerInvokerMiddleware>();
    
    
    /// <summary>
    /// Adds a notification logging middleware to the notification pipeline.
    /// </summary>
    /// <param name="builder">The notification pipeline builder.</param>
    public static INotificationPipelineBuilder UseNotificationLogging(this INotificationPipelineBuilder builder) => builder.UseMiddleware<NotificationLoggingMiddleware>();
}