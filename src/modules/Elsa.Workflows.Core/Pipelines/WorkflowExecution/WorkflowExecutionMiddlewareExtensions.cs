using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Pipelines.WorkflowExecution;

/// <summary>
/// Provides extensions to <see cref="IWorkflowExecutionPipelineBuilder"/> that adds support for installing <see cref="IWorkflowExecutionMiddleware"/> components.
/// </summary>
public static class WorkflowExecutionMiddlewareExtensions
{
    /// <summary>
    /// Installs the specified middleware component into the pipeline being built.
    /// </summary>
    public static IWorkflowExecutionPipelineBuilder UseMiddleware<TMiddleware>(this IWorkflowExecutionPipelineBuilder pipelineBuilder, params object[] args) where TMiddleware: IWorkflowExecutionMiddleware
    {
        var middleware = typeof(TMiddleware);

        return pipelineBuilder.Use(next =>
        {
            var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
            var ctorParams = new[] { next }.Concat(args).Select(x => x).ToArray();
            var instance = ActivatorUtilities.CreateInstance(pipelineBuilder.ServiceProvider, middleware, ctorParams);
            return (WorkflowMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(WorkflowMiddlewareDelegate), instance);
        });
    }
}