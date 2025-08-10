using Elsa.Extensions;
using Elsa.Testing.Framework.Services;
using Elsa.Workflows;
using Elsa.Workflows.Pipelines.ActivityExecution;

namespace Elsa.Testing.Framework.Middleware.Activities;

public class ActivityExecutionTracerMiddleware(ActivityMiddlewareDelegate next) : IActivityExecutionMiddleware
{
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        await next(context);
        var workflowExecutionContext = context.WorkflowExecutionContext;
        var tracer = workflowExecutionContext.TransientProperties.GetOrAdd("ActivityTracer", () => new ActivityTracer());
        tracer.TraceActivityExecution(context);
    }
}