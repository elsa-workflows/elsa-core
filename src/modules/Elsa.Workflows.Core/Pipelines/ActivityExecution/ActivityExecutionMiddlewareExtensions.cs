using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Pipelines.ActivityExecution;

public static class ActivityExecutionMiddlewareExtensions
{
    public static IActivityExecutionPipelineBuilder UseMiddleware<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(this IActivityExecutionPipelineBuilder pipelineBuilder, params object[] args) where TMiddleware : IActivityExecutionMiddleware
    {
        var delegateFactory = CreateMiddlewareDelegateFactory<TMiddleware>(pipelineBuilder, args);
        return pipelineBuilder.Use(delegateFactory);
    }

    public static IActivityExecutionPipelineBuilder Insert<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TMiddleware>(this IActivityExecutionPipelineBuilder pipelineBuilder, int index, params object[] args) where TMiddleware : IActivityExecutionMiddleware
    {
        var delegateFactory = CreateMiddlewareDelegateFactory<TMiddleware>(pipelineBuilder, args);
        return pipelineBuilder.Insert(index, delegateFactory);
    }

    /// <summary>
    /// Creates a middleware delegate for the specified middleware component.
    /// </summary>
    public static Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> CreateMiddlewareDelegateFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(
        this IActivityExecutionPipelineBuilder pipelineBuilder, params object[] args) where TMiddleware : IActivityExecutionMiddleware
    {
        var middleware = typeof(TMiddleware);

        return next =>
        {
            var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
            var ctorArgs = new[]
            {
                next
            }.Concat(args).Select(x => x).ToArray();
            var instance = ActivatorUtilities.CreateInstance(pipelineBuilder.ServiceProvider, middleware, ctorArgs);
            return (ActivityMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(ActivityMiddlewareDelegate), instance);
        };
    }
}