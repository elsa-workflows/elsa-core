using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Pipelines.ActivityExecution;

public static class ActivityExecutionMiddlewareExtensions
{
    public static IActivityExecutionPipelineBuilder UseMiddleware<TMiddleware>(this IActivityExecutionPipelineBuilder pipelineBuilder, params object[] args) where TMiddleware : IActivityExecutionMiddleware
    {
        var middleware = typeof(TMiddleware);

        return pipelineBuilder.Use(next =>
        {
            var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
            var ctorArgs = new[] { next }.Concat(args).Select(x => x!).ToArray();
            var instance = ActivatorUtilities.CreateInstance(pipelineBuilder.ServiceProvider, middleware, ctorArgs);
            return (ActivityMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(ActivityMiddlewareDelegate), instance);
        });
    }
}