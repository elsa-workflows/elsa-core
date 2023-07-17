namespace Elsa.Mediator.Middleware.Notification.Contracts;

/// <summary>
/// Represents a notification request pipeline. A notification pipeline is a chain of middleware that can be used to process a notification.
/// </summary>
public interface INotificationPipeline
{
    /// <summary>
    /// Sets up the notification pipeline.
    /// </summary>
    /// <param name="setup">The setup action.</param>
    /// <returns>A delegate representing the notification pipeline.</returns>
    NotificationMiddlewareDelegate Setup(Action<INotificationPipelineBuilder> setup);
    
    /// <summary>
    /// Gets the delegate representing the notification pipeline.
    /// </summary>
    NotificationMiddlewareDelegate Pipeline { get; }
    
    /// <summary>
    /// Executes the notification pipeline.
    /// </summary>
    /// <param name="context">The notification context.</param>
    Task ExecuteAsync(NotificationContext context);
}