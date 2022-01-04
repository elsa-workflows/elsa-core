using Elsa.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Pipelines.WorkflowExecution;

public static class WorkflowExecutionMiddlewareExtensions
{
    public static IWorkflowExecutionBuilder UseMiddleware<TMiddleware>(this IWorkflowExecutionBuilder builder, params object[] args) where TMiddleware: IWorkflowExecutionMiddleware
    {
        var middleware = typeof(TMiddleware);

        return builder.Use(next =>
        {
            var invokeMethod = MiddlewareHelpers.GetInvokeMethod(middleware);
            var ctorParams = new[] { next }.Concat(args).Select(x => x!).ToArray();
            var instance = ActivatorUtilities.CreateInstance(builder.ApplicationServices, middleware, ctorParams);
            return (WorkflowMiddlewareDelegate)invokeMethod.CreateDelegate(typeof(WorkflowMiddlewareDelegate), instance);
        });
    }
}