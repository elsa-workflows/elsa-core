using Elsa.Workflows.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Runtime.Middleware.Activities;

public class EvaluateLogPersistenceModesMiddleware(ActivityMiddlewareDelegate next, IActivityPropertyLogPersistenceEvaluator persistenceEvaluator) : IActivityExecutionMiddleware
{
    internal static object LogPersistenceMapKey { get; } = new();
    
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        await next(context);
        var persistenceLogMap = await persistenceEvaluator.EvaluateLogPersistenceModesAsync(context);
        context.TransientProperties[LogPersistenceMapKey] = persistenceLogMap;
    }
}