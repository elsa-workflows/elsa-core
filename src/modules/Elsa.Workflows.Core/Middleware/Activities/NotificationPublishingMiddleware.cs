using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Notifications;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Core.Middleware.Activities;

/// <summary>
/// An activity execution middleware component that publishes <see cref="ActivityExecuting"/> and <see cref="ActivityExecuted"/> events as an activity executes.
/// </summary>
public class NotificationPublishingMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;
    private readonly INotificationSender _notificationSender;

    /// <summary>
    /// Constructor.
    /// </summary>
    public NotificationPublishingMiddleware(ActivityMiddlewareDelegate next, INotificationSender notificationSender)
    {
        _next = next;
        _notificationSender = notificationSender;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        await _notificationSender.SendAsync(new ActivityExecuting(context));
        await _next(context);
        await _notificationSender.SendAsync(new ActivityExecuted(context));
    }
}

/// <summary>
/// Extends <see cref="IActivityExecutionPipelineBuilder"/> to install the <see cref="LoggingMiddleware"/> component.
/// </summary>
public static class NotificationPublishingMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="LoggingMiddleware"/> component.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseNotifications(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<NotificationPublishingMiddleware>();
}