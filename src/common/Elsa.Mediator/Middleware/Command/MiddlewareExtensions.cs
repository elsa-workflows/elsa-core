using Elsa.Mediator.Middleware.Command.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.Middleware.Command;

/// <summary>
/// Provides extension methods for <see cref="ICommandPipelineBuilder"/>.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Adds middleware to the pipeline.
    /// </summary>
    public static ICommandPipelineBuilder UseMiddleware<TMiddleware>(this ICommandPipelineBuilder builder, params object[] args) where TMiddleware : ICommandMiddleware
    {
        return builder.Use(next => BuildMiddlewareDelegate<TMiddleware>(builder, next, args));
    }

    /// <summary>
    /// Inserts middleware at a specific index in the pipeline.
    /// </summary>
    public static ICommandPipelineBuilder UseMiddleware<TMiddleware>(this ICommandPipelineBuilder builder, int index, params object[] args) where TMiddleware : ICommandMiddleware
    {
        return builder.Use(index, next => BuildMiddlewareDelegate<TMiddleware>(builder, next, args));
    }

    /// <summary>
    /// Builds a delegate for the middleware type.
    /// </summary>
    private static CommandMiddlewareDelegate BuildMiddlewareDelegate<TMiddleware>(
        ICommandPipelineBuilder builder,
        CommandMiddlewareDelegate next,
        object[] args
    ) where TMiddleware : ICommandMiddleware
    {
        var middleware = typeof(TMiddleware);
        var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
        var ctorParams = new[]
        {
            next
        }.Concat(args).Select(x => x!).ToArray();
        var instance = ActivatorUtilities.CreateInstance(builder.ApplicationServices, middleware, ctorParams);
        return (CommandMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(CommandMiddlewareDelegate), instance);
    }
}