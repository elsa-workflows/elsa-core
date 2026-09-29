namespace Elsa.Studio.Contracts;

/// <summary>
/// Represents a notification handler.
/// </summary>
public interface INotificationHandler
{
}

/// <summary>
/// Represents a notification handler.
/// </summary>
/// <typeparam name="T">The type of the notification.</typeparam>
public interface INotificationHandler<in T> : INotificationHandler where T : INotification
{
    /// <summary>
    /// Handles the given notification.
    /// </summary>
    /// <param name="notification">The notification to handle.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task HandleAsync(T notification, CancellationToken cancellationToken);
}