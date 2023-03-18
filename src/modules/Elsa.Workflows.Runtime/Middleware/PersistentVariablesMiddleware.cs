using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;

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
        // Get all persistable variables.
        var variables = _variablePersistenceManager.GetAllVariables(context).ToList();
        
        // Load variables into the workflow execution context.
        await _variablePersistenceManager.LoadVariablesAsync(context, variables);
        
        // Invoke next middleware.
        await Next(context);
        
        // Get a fresh list of all persistable variables.
        variables = _variablePersistenceManager.GetAllVariables(context).ToList();
        
        // Persist variables.
        await _variablePersistenceManager.SaveVariablesAsync(context, variables);
    }
}