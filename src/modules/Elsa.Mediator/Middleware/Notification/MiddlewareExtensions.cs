using Elsa.Mediator.Middleware.Notification.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.Middleware.Notification;

/// <summary>
/// Contains extension methods for <see cref="INotificationPipelineBuilder"/>.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds a middleware to the notification pipeline.
    /// </summary>
    /// <param name="builder">The notification pipeline builder.</param>
    /// <param name="args">The arguments to pass to the middleware constructor.</param>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    public static INotificationPipelineBuilder UseMiddleware<TMiddleware>(this INotificationPipelineBuilder builder, params object[] args) where TMiddleware : INotificationMiddleware
    {
        var middleware = typeof(TMiddleware);

        return builder.Use(next =>
        {
            var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
            var ctorParams = new[] { next }.Concat(args).Select(x => x!).ToArray();
            var instance = ActivatorUtilities.CreateInstance(builder.ApplicationServices, middleware, ctorParams);
            return (NotificationMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(NotificationMiddlewareDelegate), instance);
        });
    }
}