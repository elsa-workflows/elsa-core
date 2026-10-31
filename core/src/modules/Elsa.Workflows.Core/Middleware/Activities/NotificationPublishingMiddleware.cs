using Elsa.Mediator.Contracts;
using Elsa.Workflows.Notifications;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Workflows.Middleware.Activities;

/// <summary>
/// An activity execution middleware component that publishes activity notifications:
/// <list type="bullet">
/// <item><see cref="ActivityExecuting"/> when an activity starts executing</item>
/// <item><see cref="ActivityExecuted"/> when an activity has executed</item>
/// <item><see cref="ActivityFaulted"/> when an activity throws or is marked faulted via <c>Fault()</c></item>
/// <item><see cref="Notifications.ActivityCompleted"/> when an activity transitions from Running to Completed status</item>
/// </list>
/// </summary>
public class NotificationPublishingMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;
    private readonly INotificationSender _notificationSender;
    private readonly ILogger<NotificationPublishingMiddleware> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public NotificationPublishingMiddleware(ActivityMiddlewareDelegate next, INotificationSender notificationSender, ILogger<NotificationPublishingMiddleware>? logger = null)
    {
        _next = next;
        _notificationSender = notificationSender;
        _logger = logger ?? NullLogger<NotificationPublishingMiddleware>.Instance;
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
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // ExceptionHandlingMiddleware has not called Fault() yet, so the context is still Running.
            // ActivityFaulted carries the exception so handlers can see that the activity faulted and why.
            await PublishThrowPathNotificationsAsync(context, exception);
            throw;
        }

        // Send notification that activity has executed
        await _notificationSender.SendAsync(new ActivityExecuted(context));

        // Fault() returns normally with Status = Faulted. Publish ActivityFaulted so the Fault() path
        // raises the same dedicated notification as the throw path.
        if (context.Status == ActivityStatus.Faulted && context.Exception is { } fault)
            await _notificationSender.SendAsync(new ActivityFaulted(context, fault));

        // Send notification that activity has completed if status transitioned from Running to Completed
        if (statusBeforeExecution == ActivityStatus.Running && context.Status == ActivityStatus.Completed)
            await _notificationSender.SendAsync(new Notifications.ActivityCompleted(context));
    }

    /// <summary>
    /// Publishes the after-execution notifications for a thrown exception, then lets the original exception continue.
    /// </summary>
    /// <remarks>
    /// <see cref="OperationCanceledException"/> is filtered out of the caller so this method is never used for
    /// cancellation. Cancellation is not a fault: ExceptionHandlingMiddleware and the workflow-level middleware
    /// keep cancellation and faults apart on purpose, so this path stays the same as before — no
    /// <see cref="ActivityExecuted"/> and no <see cref="ActivityFaulted"/> while the run is being torn down.
    /// </remarks>
    private async Task PublishThrowPathNotificationsAsync(ActivityExecutionContext context, Exception exception)
    {
        try
        {
            await _notificationSender.SendAsync(new ActivityExecuted(context));
            await _notificationSender.SendAsync(new ActivityFaulted(context, exception));
        }
        catch (Exception handlerException)
        {
            _logger.LogError(
                handlerException,
                "An activity notification handler threw while publishing fault notifications for activity {ActivityId} of type {ActivityType}. The original activity exception is preserved",
                context.Activity.Id,
                context.Activity.Type);
        }
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
