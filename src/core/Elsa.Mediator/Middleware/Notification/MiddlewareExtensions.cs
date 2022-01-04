using Elsa.Mediator.Middleware.Notification.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.Middleware.Notification;

public static class MiddlewareExtensions
{
    public static INotificationPipelineBuilder UseMiddleware<TMiddleware>(this INotificationPipelineBuilder builder, params object[] args) where TMiddleware: INotificationMiddleware
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