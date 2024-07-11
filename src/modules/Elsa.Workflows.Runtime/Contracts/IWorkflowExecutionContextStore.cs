namespace Elsa.Workflows.Runtime;

/// <summary>
/// Stores <see cref="WorkflowExecutionContext"/> records.
/// </summary>
public interface IWorkflowExecutionContextStore
{
    /// <summary>
    /// Saves a record of the <see cref="WorkflowExecutionContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="WorkflowExecutionContext"/> to save.</param>
    Task SaveAsync(WorkflowExecutionContext context);

    /// <summary>
    /// Finds a <see cref="WorkflowExecutionContext"/> with the specified ID.
    /// </summary>
    /// <returns>The matching entity or null if no match was found.</returns>
    Task<WorkflowExecutionContext?> FindAsync(string workflowExecutionContextId);

    /// <summary>
    /// Deletes the record of the <see cref="WorkflowExecutionContext"/> with the specified ID if it exists.
    /// </summary>
    Task DeleteAsync(string workflowExecutionContextId);
}