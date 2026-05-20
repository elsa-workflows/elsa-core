using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Makes the current workflow execution context available to the workflow dispatch outbox.
/// </summary>
public class WorkflowDispatchOutboxMiddleware(WorkflowMiddlewareDelegate next, IWorkflowDispatchOutboxAccessor accessor) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var previousContext = accessor.WorkflowExecutionContext;
        accessor.WorkflowExecutionContext = context;

        try
        {
            await Next(context);
        }
        finally
        {
            accessor.WorkflowExecutionContext = previousContext;
        }
    }
}
