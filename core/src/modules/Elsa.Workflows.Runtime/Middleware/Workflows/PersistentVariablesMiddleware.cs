using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Takes care of loading and persisting workflow variables.
/// </summary>
public class PersistentVariablesMiddleware(WorkflowMiddlewareDelegate next, IVariablePersistenceManager variablePersistenceManager) : WorkflowExecutionMiddleware(next)
{
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Load variables into the workflow execution context.
        await variablePersistenceManager.LoadVariablesAsync(context);
        
        // Invoke next middleware.
        await Next(context);
        
        // Variables are persisted through the commit state handler.
    }
}