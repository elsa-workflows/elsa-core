using Elsa.Workflows.Pipelines.ActivityExecution;

namespace Elsa.Workflows.Runtime.Middleware;

public class EvaluateLogPersistenceModesMiddleware(ActivityMiddlewareDelegate next, IActivityPropertyLogPersistenceEvaluator persistenceEvaluator) : IActivityExecutionMiddleware
{
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        // Evaluate log persistence mode before executing the next middleware to ensure the mode is known before any potential commit action.
        var persistenceLogMap = await persistenceEvaluator.EvaluateLogPersistenceModesAsync(context);
        context.SetLogPersistenceModeMap(persistenceLogMap);
        
        // Execute next middleware.
        await next(context);
    }
}