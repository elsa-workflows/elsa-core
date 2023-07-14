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
    /// Sends a notification using the default notification strategy configured via <see cref="MediatorOptions"/>.
    /// </summary>
    /// <param name="mediator">The mediator.</param>
    /// <param name="notification">The notification to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task SendAsync(this IMediator mediator, INotification notification, CancellationToken cancellationToken = default)
    {
        await mediator.SendAsync(notification, default, cancellationToken);
    }
    
    /// <summary>
    /// Sends a notification using the default notification strategy configured via <see cref="MediatorOptions"/>.
    /// </summary>
    /// <param name="publisher">The publisher.</param>
    /// <param name="notification">The notification to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task SendAsync(this INotificationSender publisher, INotification notification, CancellationToken cancellationToken = default)
    {
        await publisher.SendAsync(notification, default, cancellationToken);
    }
    
    /// <summary>
    /// Sends a command using the default command strategy configured via <see cref="MediatorOptions"/>.
    /// </summary>
    /// <param name="mediator">The mediator.</param>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task SendAsync(this IMediator mediator, ICommand command, CancellationToken cancellationToken = default)
    {
        await mediator.SendAsync(command, default, cancellationToken);
    }
    
    /// <summary>
    /// Sends a command using the default command strategy configured via <see cref="MediatorOptions"/>.
    /// </summary>
    /// <param name="commandSender">The command sender.</param>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public static async Task SendAsync(this ICommandSender commandSender, ICommand command, CancellationToken cancellationToken = default)
    {
        await commandSender.SendAsync(command, default, cancellationToken);
    }
}