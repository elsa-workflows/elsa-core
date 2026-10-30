using Elsa.Mediator.Middleware.Request.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.Middleware.Request;

/// <summary>
/// Provides a set of static methods for building a request pipeline.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds a request handler invoker middleware to the request pipeline.
    /// </summary>
    /// <param name="builder">The request pipeline builder.</param>
    /// <param name="args">The arguments to pass to the middleware constructor.</param>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    public static IRequestPipelineBuilder UseMiddleware<TMiddleware>(this IRequestPipelineBuilder builder, params object[] args) where TMiddleware : IRequestMiddleware
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