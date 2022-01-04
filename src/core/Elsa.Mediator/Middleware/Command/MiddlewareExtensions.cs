using Elsa.Mediator.Middleware.Command.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.Middleware.Command;

public static class MiddlewareExtensions
{
    public static ICommandPipelineBuilder UseMiddleware<TMiddleware>(this ICommandPipelineBuilder builder, params object[] args) where TMiddleware: ICommandMiddleware
    {
        var middleware = typeof(TMiddleware);

        return builder.Use(next =>
        {
            var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
            var ctorParams = new[] { next }.Concat(args).Select(x => x!).ToArray();
            var instance = ActivatorUtilities.CreateInstance(builder.ApplicationServices, middleware, ctorParams);
            return (CommandMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(CommandMiddlewareDelegate), instance);
        });
    }
}