using Elsa.Studio.Contracts;
using JetBrains.Annotations;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Contains extension methods for <see cref="IMediator"/>.
/// </summary>
[UsedImplicitly]
public static class MediatorExtensions
{
    /// <summary>
    /// Subscribes the given handler to the given notification.
    /// Use this from components' Initialize methods.
    /// The component itself can be the handler
    /// to subscribe itself if it implements <see cref="INotificationHandler{T}"/>.
    /// </summary>
    public static void Subscribe<TNotification>(this IMediator mediator, INotificationHandler handler) where TNotification : INotification
    {
        var handlerType = handler.GetType();
        var subscribeMethod = typeof(IMediator).GetMethod(nameof(IMediator.Subscribe))!;
        var genericSubscribeMethod = subscribeMethod.MakeGenericMethod(typeof(TNotification), handlerType);
        genericSubscribeMethod.Invoke(mediator, [handler]);
    }
    
    /// <summary>
    /// Unsubscribes the given handler from the given notification.
    /// Use this from components' Dispose methods.
    /// The component itself can be the handler
    /// to unsubscribe itself if it implements <see cref="INotificationHandler{T}"/>.
    /// </summary>
    public static void Unsubscribe<TNotification>(this IMediator mediator, INotificationHandler handler) where TNotification : INotification
    {
        var handlerType = handler.GetType();
        var unsubscribeMethod = typeof(IMediator).GetMethod(nameof(IMediator.Unsubscribe))!;
        var genericUnsubscribeMethod = unsubscribeMethod.MakeGenericMethod(typeof(TNotification), handlerType);
        genericUnsubscribeMethod.Invoke(mediator, [handler]);
    }
}
