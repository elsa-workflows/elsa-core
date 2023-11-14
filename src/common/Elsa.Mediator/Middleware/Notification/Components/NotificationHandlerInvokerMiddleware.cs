using Elsa.Mediator.Contexts;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Notification.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Mediator.Middleware.Notification.Components;

/// <inheritdoc />
public class NotificationHandlerInvokerMiddleware : INotificationMiddleware
{
    private readonly NotificationMiddlewareDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationHandlerInvokerMiddleware> _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationHandlerInvokerMiddleware"/> class.
    /// </summary>
    public NotificationHandlerInvokerMiddleware(
        NotificationMiddlewareDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationHandlerInvokerMiddleware> logger,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(NotificationContext context)
    {
        using var scope = _scopeFactory.CreateScope();
        var notificationHandlers = scope.ServiceProvider.GetServices<INotificationHandler>();

        // Find all handlers for the specified notification.
        var notification = context.Notification;
        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        var handlers = notificationHandlers.Where(x => handlerType.IsInstanceOfType(x)).DistinctBy(x => x.GetType()).ToArray();
        var strategyContext = new NotificationStrategyContext(notification, handlers, _logger, _serviceProvider, context.CancellationToken);

        await context.NotificationStrategy.PublishAsync(strategyContext);

        // Invoke next middleware.
        await _next(context);
    }
}