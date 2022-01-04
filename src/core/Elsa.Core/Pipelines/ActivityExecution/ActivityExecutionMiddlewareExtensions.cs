using Elsa.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Pipelines.ActivityExecution;

public static class ActivityExecutionMiddlewareExtensions
{
    public static IActivityExecutionBuilder UseMiddleware<TMiddleware>(this IActivityExecutionBuilder builder, params object[] args) where TMiddleware : IActivityExecutionMiddleware
    {
        var middleware = typeof(TMiddleware);

        return builder.Use(next =>
        {
            var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
            var ctorArgs = new[] { next }.Concat(args).Select(x => x!).ToArray();
            var instance = ActivatorUtilities.CreateInstance(builder.ServiceProvider, middleware, ctorArgs);
            return (ActivityMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(ActivityMiddlewareDelegate), instance);
        });
    }
}