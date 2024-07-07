using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Contracts;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Takes care of persisting workflow execution log entries.
/// </summary>
public class PersistWorkflowExecutionLogMiddleware(WorkflowMiddlewareDelegate next, IWorkflowExecutionLogSink sink) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        await sink.PersistExecutionLogsAsync(context);
    }
}