using Elsa.Workflows.Core.Models;

namespace Elsa.WorkflowContexts.Services;

/// <summary>
/// Implement this interface to implement a workflow context provider that loads application-specific objects into the workflow.
/// These providers can then be configured on a given workflow.
/// </summary>
public interface IWorkflowContextProvider
{
    /// <summary>
    /// Implement this method to load an object into memory that is accessible throughout the lifetime of the workflow's current execution. 
    /// </summary>
    ValueTask<object?> LoadAsync(WorkflowExecutionContext workflowExecutionContext);

    /// <summary>
    /// Implement this method to save an object that was loaded previously 
    /// </summary>
    ValueTask SaveAsync(WorkflowExecutionContext workflowExecutionContext, object? context);
}