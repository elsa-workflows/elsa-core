using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// Creates and updates activity execution records from activity execution contexts.
public class PersistActivityExecutionLogMiddleware(WorkflowMiddlewareDelegate next, ILogRecordSink<ActivityExecutionRecord> sink) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        await Next(context);
        await sink.PersistExecutionLogsAsync(context);
    }
}