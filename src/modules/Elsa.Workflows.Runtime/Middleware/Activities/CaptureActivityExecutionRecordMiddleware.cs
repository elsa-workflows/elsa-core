using Elsa.Extensions;
using Elsa.Workflows.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Runtime.Middleware.Activities;

public class CaptureActivityExecutionRecordMiddleware(ActivityMiddlewareDelegate next, IActivityExecutionMapper mapper) : IActivityExecutionMiddleware
{
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        await next(context);
        await context.CaptureActivityExecutionRecordAsync();
    }
}