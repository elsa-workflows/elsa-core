namespace Elsa.Mediator.Middleware.Notification.Contracts;

/// <summary>
/// Represents a builder for building a notification pipeline.
/// </summary>
public interface INotificationPipelineBuilder
{
    /// <summary>
    /// Gets the properties associated with the notification pipeline.
    /// </summary>
    public IDictionary<string, object?> Properties { get; }
    
    /// <summary>
    /// Gets the service provider.
    /// </summary>
    IServiceProvider ApplicationServices { get; }
    
    /// <summary>
    /// Adds a middleware to the notification pipeline.
    /// </summary>
    /// <param name="middleware">The middleware to add.</param>
    INotificationPipelineBuilder Use(Func<NotificationMiddlewareDelegate, NotificationMiddlewareDelegate> middleware);
    
    /// <summary>
    /// Builds the notification pipeline.
    /// </summary>
    /// <returns></returns>
    public NotificationMiddlewareDelegate Build();
}