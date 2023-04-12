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
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="args">Any arguments to pass to the middleware constructor.</param>
    /// <typeparam name="TMiddleware">The middleware type.</typeparam>
    /// <returns>The pipeline builder.</returns>
    public static ICommandPipelineBuilder UseMiddleware<TMiddleware>(this ICommandPipelineBuilder builder, params object[] args) where TMiddleware : ICommandMiddleware
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