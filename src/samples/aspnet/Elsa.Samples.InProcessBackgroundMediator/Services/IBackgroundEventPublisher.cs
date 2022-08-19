using MediatR;

namespace Elsa.Samples.InProcessBackgroundMediator.Services;

/// <summary>
/// Publish notifications to be processed asynchronously in the background.
/// </summary>
public interface IBackgroundEventPublisher
{
    /// <summary>
    /// Publish the specified notification using from a background service.
    /// </summary>
    Task PublishAsync(INotification notification, CancellationToken cancellationToken = default);
}