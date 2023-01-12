using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Runtime.Middleware;

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
        var variables = _variablePersistenceManager.GetVariables(context);
        await _variablePersistenceManager.LoadVariablesAsync(context, variables);
        
        // Invoke next middleware.
        await Next(context);
        
        // Persist variables.
        await _variablePersistenceManager.SaveVariablesAsync(context);
    }
}