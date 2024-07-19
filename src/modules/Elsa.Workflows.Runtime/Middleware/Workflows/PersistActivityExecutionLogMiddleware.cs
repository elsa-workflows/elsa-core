using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Creates and updates activity execution records from activity execution contexts.
/// </summary>
public class PersistActivityExecutionLogMiddleware(WorkflowMiddlewareDelegate next, IActivityExecutionLogSink sink) : WorkflowExecutionMiddleware(next)
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