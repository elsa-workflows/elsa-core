using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows.Core.Models;
using JetBrains.Annotations;

namespace Elsa.WorkflowContexts.Abstractions;

/// <summary>
/// A base class for workflow context providers.
/// </summary>
/// <typeparam name="T">The type of the workflow context value.</typeparam>
[PublicAPI]
public abstract class WorkflowContextProvider<T> : IWorkflowContextProvider
{
    async ValueTask<object?> IWorkflowContextProvider.LoadAsync(WorkflowExecutionContext workflowExecutionContext) => await LoadAsync(workflowExecutionContext);

    async ValueTask IWorkflowContextProvider.SaveAsync(WorkflowExecutionContext workflowExecutionContext, object? context) => await SaveAsync(workflowExecutionContext, (T?)context);

    /// <summary>
    /// Load data that will be available to the workflow.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context.</param>
    /// <returns>The workflow context value.</returns>
    protected virtual ValueTask<T?> LoadAsync(WorkflowExecutionContext workflowExecutionContext) => new(Load(workflowExecutionContext));
    
    /// <summary>
    /// Load data that will be available to the workflow.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context.</param>
    /// <returns>The workflow context value.</returns>
    protected virtual T? Load(WorkflowExecutionContext workflowExecutionContext) => default;

    /// <summary>
    /// Save data.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context.</param>
    /// <param name="context">The workflow context value.</param>
    protected virtual ValueTask SaveAsync(WorkflowExecutionContext workflowExecutionContext, T? context)
    {
        Save(workflowExecutionContext, context);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Save data.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context.</param>
    /// <param name="context">The workflow context value.</param>
    protected virtual void Save(WorkflowExecutionContext workflowExecutionContext, T? context)
    {
    }
}