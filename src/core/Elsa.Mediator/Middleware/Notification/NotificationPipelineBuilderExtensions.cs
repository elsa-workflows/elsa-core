using Elsa.Mediator.Middleware.Notification.Components;
using Elsa.Mediator.Middleware.Notification.Contracts;

namespace Elsa.Mediator.Middleware.Notification;

public static class NotificationPipelineBuilderExtensions
{
    public static INotificationPipelineBuilder UseNotificationHandlers(this INotificationPipelineBuilder builder) => builder.UseMiddleware<NotificationHandlerInvokerMiddleware>();
    public static INotificationPipelineBuilder UseNotificationLogging(this INotificationPipelineBuilder builder) => builder.UseMiddleware<NotificationLoggingMiddleware>();
}