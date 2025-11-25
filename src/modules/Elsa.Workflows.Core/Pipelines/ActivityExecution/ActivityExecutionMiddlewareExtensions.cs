using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Pipelines.ActivityExecution;

public static class ActivityExecutionMiddlewareExtensions
{
    extension(IActivityExecutionPipelineBuilder pipelineBuilder)
    {
        public IActivityExecutionPipelineBuilder UseMiddleware<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(params object[] args) where TMiddleware : IActivityExecutionMiddleware
        {
            var delegateFactory = CreateMiddlewareDelegateFactory<TMiddleware>(pipelineBuilder, args);
            return pipelineBuilder.Use(delegateFactory);
        }

        public IActivityExecutionPipelineBuilder Insert<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]TMiddleware>(int index, params object[] args) where TMiddleware : IActivityExecutionMiddleware
        {
            var delegateFactory = CreateMiddlewareDelegateFactory<TMiddleware>(pipelineBuilder, args);
            return pipelineBuilder.Insert(index, delegateFactory);
        }

        /// <summary>
        /// Creates a middleware delegate for the specified middleware component.
        /// </summary>
        public Func<ActivityMiddlewareDelegate, ActivityMiddlewareDelegate> CreateMiddlewareDelegateFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(params object[] args) where TMiddleware : IActivityExecutionMiddleware
        {
            var middleware = typeof(TMiddleware);

            return next =>
            {
                var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
                var ctorArgs = new[]
                {
                    next
                }.Concat(args).ToArray();
                var instance = ActivatorUtilities.CreateInstance(pipelineBuilder.ServiceProvider, middleware, ctorArgs);
                return (ActivityMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(ActivityMiddlewareDelegate), instance);
            };
        }
    }
}