using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Takes care of persisting workflow execution log entries.
/// </summary>
[Obsolete("This middleware is no longer used and will be removed in a future version. Execution logs are now persisted through the commit state handler.")]
public class PersistWorkflowExecutionLogMiddleware(WorkflowMiddlewareDelegate next) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        // Not used anymore.
        //await sink.PersistExecutionLogsAsync(context);
    }
}