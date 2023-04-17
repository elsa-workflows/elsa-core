using Elsa.Workflows.Core.Models;

namespace Elsa.WorkflowContexts.Contracts;

/// <summary>
/// Represents a workflow context provider that loads application-specific data and makes it available to the workflow.
/// </summary>
public interface IWorkflowContextProvider
{
    /// <summary>
    /// Load data that will be available to the workflow. 
    /// </summary>
    ValueTask<object?> LoadAsync(WorkflowExecutionContext workflowExecutionContext);

    /// <summary>
    /// Save data. 
    /// </summary>
    ValueTask SaveAsync(WorkflowExecutionContext workflowExecutionContext, object? context);
}