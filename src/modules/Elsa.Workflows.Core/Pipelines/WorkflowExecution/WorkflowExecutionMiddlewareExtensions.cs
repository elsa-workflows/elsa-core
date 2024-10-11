using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Pipelines.WorkflowExecution;

/// Provides extensions to <see cref="IWorkflowExecutionPipelineBuilder"/> that adds support for installing <see cref="IWorkflowExecutionMiddleware"/> components.
public static class WorkflowExecutionMiddlewareExtensions
{
    /// Installs the specified middleware component into the pipeline being built.
    public static IWorkflowExecutionPipelineBuilder UseMiddleware<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(
        this IWorkflowExecutionPipelineBuilder pipelineBuilder, params object[] args) where TMiddleware : IWorkflowExecutionMiddleware
    {
        var delegateFactory = CreateMiddlewareDelegateFactory<TMiddleware>(pipelineBuilder, args);
        return pipelineBuilder.Use(delegateFactory);
    }
    
    /// Installs the specified middleware component into the pipeline being built.
    public static IWorkflowExecutionPipelineBuilder Insert<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(
        this IWorkflowExecutionPipelineBuilder pipelineBuilder, int index, params object[] args) where TMiddleware : IWorkflowExecutionMiddleware
    {
        var delegateFactory = CreateMiddlewareDelegateFactory<TMiddleware>(pipelineBuilder, args);
        return pipelineBuilder.Insert(index, delegateFactory);
    }

    /// Replaces the terminal middleware component with the specified middleware component.
    public static IWorkflowExecutionPipelineBuilder ReplaceTerminal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(
        this IWorkflowExecutionPipelineBuilder pipelineBuilder, params object[] args) where TMiddleware : IWorkflowExecutionMiddleware
    {
        var index = pipelineBuilder.Components.Count() - 1;
        return pipelineBuilder.Replace<TMiddleware>(index, args);
    }
    
    /// Replaces the middleware component at the specified index with the specified middleware component.
    public static IWorkflowExecutionPipelineBuilder Replace<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(
        this IWorkflowExecutionPipelineBuilder pipelineBuilder, int index, params object[] args) where TMiddleware : IWorkflowExecutionMiddleware
    {
        var delegateFactory = CreateMiddlewareDelegateFactory<TMiddleware>(pipelineBuilder, args);
        return pipelineBuilder.Replace(index, delegateFactory);
    }
    
    /// Creates a middleware delegate for the specified middleware component.
    public static Func<WorkflowMiddlewareDelegate, WorkflowMiddlewareDelegate> CreateMiddlewareDelegateFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TMiddleware>(
        this IWorkflowExecutionPipelineBuilder pipelineBuilder, params object[] args) where TMiddleware : IWorkflowExecutionMiddleware
    {
        var middleware = typeof(TMiddleware);

        return next =>
        {
            var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
            var ctorParams = new[]
            {
                next
            }.Concat(args).Select(x => x).ToArray();
            var instance = ActivatorUtilities.CreateInstance(pipelineBuilder.ServiceProvider, middleware, ctorParams);
            return (WorkflowMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(WorkflowMiddlewareDelegate), instance);
        };
    }
}