using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Makes the current workflow execution context available to the workflow dispatch outbox.
/// </summary>
public class WorkflowDispatchOutboxMiddleware(
    WorkflowMiddlewareDelegate next,
    IWorkflowDispatchOutboxAccessor accessor,
    IOptions<WorkflowDispatcherOptions> options) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        if (!options.Value.UseTransactionalOutbox)
        {
            await Next(context);
            return;
        }

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
