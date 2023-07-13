namespace Elsa.Workflows.Core.Contracts;

/// <summary>
/// Helps managing the persistence of variables.
/// </summary>
public interface IVariablePersistenceManager
{
    /// <summary>
    /// Loads the variables into the specified <see cref="WorkflowExecutionContext"/>. 
    /// </summary>
    Task LoadVariablesAsync(WorkflowExecutionContext context);

    /// <summary>
    /// Persists all persistable variables from the specified <see cref="WorkflowExecutionContext"/>. 
    /// </summary>
    Task SaveVariablesAsync(WorkflowExecutionContext context);

    /// <summary>
    /// Deletes the specified variables from the <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    Task DeleteVariablesAsync(ActivityExecutionContext context);
}