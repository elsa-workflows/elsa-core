using Elsa.WorkflowContexts.Services;
using Elsa.Workflows.Core.Models;

namespace Elsa.WorkflowContexts.Abstractions;

public abstract class WorkflowContextProvider<T> : IWorkflowContextProvider
{
    async ValueTask<object?> IWorkflowContextProvider.LoadAsync(WorkflowExecutionContext workflowExecutionContext) => await LoadAsync(workflowExecutionContext);

    async ValueTask IWorkflowContextProvider.SaveAsync(WorkflowExecutionContext workflowExecutionContext, object? context) => await SaveAsync(workflowExecutionContext, (T?)context);

    protected virtual ValueTask<T?> LoadAsync(WorkflowExecutionContext workflowExecutionContext) => new(Load(workflowExecutionContext));
    protected virtual T? Load(WorkflowExecutionContext workflowExecutionContext) => default;

    protected virtual ValueTask SaveAsync(WorkflowExecutionContext workflowExecutionContext, T? context)
    {
        Save(workflowExecutionContext, context);
        return ValueTask.CompletedTask;
    }

    protected virtual void Save(WorkflowExecutionContext workflowExecutionContext, T? context)
    {
    }
}