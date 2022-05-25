using Elsa.Mediator.Middleware.Request.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.Middleware.Request;

public static class MiddlewareExtensions
{
    public static IRequestPipelineBuilder UseMiddleware<TMiddleware>(this IRequestPipelineBuilder builder, params object[] args) where TMiddleware: IRequestMiddleware
    {
        var middleware = typeof(TMiddleware);

        return builder.Use(next =>
        {
            var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
            var ctorParams = new[] { next }.Concat(args).Select(x => x!).ToArray();
            var instance = ActivatorUtilities.CreateInstance(builder.ApplicationServices, middleware, ctorParams);
            return (RequestMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(RequestMiddlewareDelegate), instance);
        });
    }
}