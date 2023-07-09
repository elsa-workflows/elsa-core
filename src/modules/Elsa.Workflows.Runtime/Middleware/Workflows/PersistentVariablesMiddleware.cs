using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;

namespace Elsa.Workflows.Runtime.Middleware.Workflows;

/// <summary>
/// Takes care of loading and persisting workflow variables.
/// </summary>
public class PersistentVariablesMiddleware : WorkflowExecutionMiddleware
{
    private readonly IVariablePersistenceManager _variablePersistenceManager;

    /// <summary>
    /// Constructor.
    /// </summary>
    public PersistentVariablesMiddleware(WorkflowMiddlewareDelegate next, IVariablePersistenceManager variablePersistenceManager) : base(next)
    {
        _variablePersistenceManager = variablePersistenceManager;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Load variables into the workflow execution context.
        await _variablePersistenceManager.LoadVariablesAsync(context);
        
        // Invoke next middleware.
        await Next(context);
        
        // Persist variables.
        await _variablePersistenceManager.SaveVariablesAsync(context);
    }
}