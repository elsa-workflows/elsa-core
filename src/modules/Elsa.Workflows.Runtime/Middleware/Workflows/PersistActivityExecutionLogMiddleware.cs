using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Creates and updates activity execution records from activity execution contexts.
/// </summary>
[Obsolete("This middleware is no longer used and will be removed in a future version. Activity state is now persisted through the commit state handler")]
public class PersistActivityExecutionLogMiddleware(WorkflowMiddlewareDelegate next) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        await Next(context);
        
        // Not used anymore.
        //await sink.PersistExecutionLogsAsync(context);
    }
}