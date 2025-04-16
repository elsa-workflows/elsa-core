using Elsa.Workflows.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Runtime.Middleware;

public class EvaluateLogPersistenceModesMiddleware(ActivityMiddlewareDelegate next, IActivityPropertyLogPersistenceEvaluator persistenceEvaluator) : IActivityExecutionMiddleware
{
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        await next(context);
        var persistenceLogMap = await persistenceEvaluator.EvaluateLogPersistenceModesAsync(context);
        context.SetLogPersistenceModeMap(persistenceLogMap);
    }
}