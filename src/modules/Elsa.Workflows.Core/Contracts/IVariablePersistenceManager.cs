using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Helps managing the persistence of variables.
/// </summary>
public interface IVariablePersistenceManager
{
    /// <summary>
    /// Returns a list of all persistable variables at the root level of the workflow. 
    /// </summary>
    IEnumerable<Variable> GetAllVariables(WorkflowExecutionContext context);
    
    /// <summary>
    /// Returns a list of all persistable variables directly scoped to the specified <see cref="ActivityExecutionContext"/>, if it's a <see cref="IVariableContainer"/>. 
    /// </summary>
    IEnumerable<Variable> GetLocalVariables(ActivityExecutionContext context);
    
    /// <summary>
    /// Returns a list of all persistable variables in scope of the specified <see cref="ActivityExecutionContext"/>. 
    /// </summary>
    IEnumerable<Variable> GetVariablesInScope(ActivityExecutionContext context);

    /// <summary>
    /// Loads the variables into the specified <see cref="WorkflowExecutionContext"/>. 
    /// </summary>
    Task LoadVariablesAsync(WorkflowExecutionContext context, IEnumerable<Variable> variables);

    /// <summary>
    /// Persists all persistable variables from the specified <see cref="WorkflowExecutionContext"/>. 
    /// </summary>
    Task SaveVariablesAsync(WorkflowExecutionContext context, IEnumerable<Variable> variables);

    /// <summary>
    /// Ensures that the specified variables are declared in the specified <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    void EnsureVariables(WorkflowExecutionContext context, IEnumerable<Variable> variables);

    /// <summary>
    /// Deletes the specified variables from the <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    Task DeleteVariablesAsync(WorkflowExecutionContext context, IEnumerable<Variable> variables);
}