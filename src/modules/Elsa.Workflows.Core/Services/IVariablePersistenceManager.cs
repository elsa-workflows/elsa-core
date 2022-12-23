using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <summary>
/// Helps managing the persistence of variables.
/// </summary>
public interface IVariablePersistenceManager
{
    /// <summary>
    /// Returns a list of all persistable variables at the root level of the workflow. 
    /// </summary>
    IEnumerable<Variable> GetPersistentVariables(WorkflowExecutionContext context);
    
    /// <summary>
    /// Returns a list of all persistable variables in scope of the specified <see cref="ActivityExecutionContext"/>. 
    /// </summary>
    IEnumerable<Variable> GetPersistentVariablesInScope(ActivityExecutionContext context);

    /// <summary>
    /// Loads the variables into the specified <see cref="WorkflowExecutionContext"/>. 
    /// </summary>
    Task LoadVariablesAsync(WorkflowExecutionContext context, IEnumerable<Variable> variables);

    /// <summary>
    /// Persists all persistable variables from the specified <see cref="WorkflowExecutionContext"/>. 
    /// </summary>
    Task SaveVariablesAsync(WorkflowExecutionContext context);

    /// <summary>
    /// Ensures that the specified variables are declared in the specified <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    void EnsureVariablesAreDeclared(WorkflowExecutionContext context, IEnumerable<Variable> variables);
}