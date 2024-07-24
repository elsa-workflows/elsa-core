using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// Creates and updates activity execution records from activity execution contexts.
public class PersistActivityExecutionLogMiddleware(WorkflowMiddlewareDelegate next, ILogRecordSink<ActivityExecutionRecord> sink) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Invoke next middleware.
        await Next(context);

        // Get the managed cancellation token.
        var cancellationToken = context.CancellationTokens.SystemCancellationToken;

        await sink.PersistExecutionLogsAsync(context, cancellationToken);
    }
}