using Elsa.Mediator.Contracts;
using Elsa.Mediator.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides a set of extension methods for <see cref="IMediator"/>.
/// </summary>
public static class MediatorExtensions
{
    /// <summary>
    /// Publishes a notification using the default publishing strategy configured via <see cref="MediatorOptions"/>.
    /// </summary>
    /// <param name="mediator">The mediator.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task PublishAsync(this IMediator mediator, INotification notification, CancellationToken cancellationToken = default)
    {
        await mediator.PublishAsync(notification, default, cancellationToken);
    }
    
    /// <summary>
    /// Publishes a notification using the default publishing strategy configured via <see cref="MediatorOptions"/>.
    /// </summary>
    /// <param name="publisher">The publisher.</param>
    /// <param name="notification">The notification to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task PublishAsync(this IEventPublisher publisher, INotification notification, CancellationToken cancellationToken = default)
    {
        await publisher.PublishAsync(notification, default, cancellationToken);
    }
}