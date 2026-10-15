using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Services;

/// <summary>
/// A default implementation of <see cref="IMediator"/>.
/// </summary>
public class DefaultMediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISet<INotificationHandler> _handlers = new HashSet<INotificationHandler>();

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMediator"/> class.
    /// </summary>
    public DefaultMediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    /// <inheritdoc />
    public void Subscribe<TNotification, THandler>(THandler handler) where TNotification : INotification where THandler : INotificationHandler<TNotification>
    {
        _handlers.Add(handler);
    }

    /// <inheritdoc />
    public void Unsubscribe<TNotification, THandler>(THandler handler) where TNotification : INotification where THandler : INotificationHandler<TNotification>
    {
        _handlers.Remove(handler);
    }

    /// <inheritdoc />
    public void Unsubscribe(INotificationHandler handler)
    {
        var handlers =_handlers.Where(x => x == handler).ToList();
        
        foreach (var h in handlers)
            _handlers.Remove(h);
    }

    /// <inheritdoc />
    public Task NotifyAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        // Collect handlers registered manually, and handlers registered via DI.
        var manualHandlers = _handlers.OfType<INotificationHandler<TNotification>>().ToList();
        var registeredHandlers = _serviceProvider.GetServices<INotificationHandler>().OfType<INotificationHandler<TNotification>>().ToList();
        var handlers = manualHandlers.Concat(registeredHandlers).ToList();
        var tasks = handlers.Select(x => x.HandleAsync(notification, cancellationToken));
        return Task.WhenAll(tasks);
    }
}