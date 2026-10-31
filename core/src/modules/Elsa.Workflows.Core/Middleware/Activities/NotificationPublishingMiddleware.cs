using Elsa.Mediator.Contracts;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Middleware.Activities;

/// <summary>
/// An activity execution middleware component that publishes activity notifications:
/// <list type="bullet">
/// <item><see cref="ActivityExecuting"/> when an activity starts executing</item>
/// <item><see cref="ActivityExecuted"/> when an activity has executed</item>
/// <item><see cref="Notifications.ActivityCompleted"/> when an activity transitions from Running to Completed status</item>
/// </list>
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
        // Store the status before execution
        var statusBeforeExecution = context.Status;
        
        // Send notification that activity is executing
        await _notificationSender.SendAsync(new ActivityExecuting(context));

        try
        {
            await _next(context);
        }
        finally
        {
            // Publish after both a normal return and a thrown exception so throw and Fault()
            // produce the same executed notification. ExceptionHandlingMiddleware still
            // converts the exception into a fault after this middleware returns.
            await _notificationSender.SendAsync(new ActivityExecuted(context));
        }

        // Send notification that activity has completed if status transitioned from Running to Completed
        if (statusBeforeExecution == ActivityStatus.Running && context.Status == ActivityStatus.Completed)
            await _notificationSender.SendAsync(new Notifications.ActivityCompleted(context));
    }
}

/// <summary>
/// Extends <see cref="IActivityExecutionPipelineBuilder"/> to install the <see cref="NotificationPublishingMiddleware"/> component.
/// </summary>
public static class NotificationPublishingMiddlewareExtensions
{
    /// <summary>
    /// Installs the <see cref="NotificationPublishingMiddleware"/> component.
    /// </summary>
    public static IActivityExecutionPipelineBuilder UseNotifications(this IActivityExecutionPipelineBuilder pipelineBuilder) => pipelineBuilder.UseMiddleware<NotificationPublishingMiddleware>();
}