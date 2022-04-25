using Elsa.Models;
using Elsa.Services;

namespace Elsa.Pipelines.WorkflowExecution.Components;

public abstract class WorkflowExecutionMiddleware : IWorkflowExecutionMiddleware
{
    protected WorkflowExecutionMiddleware(WorkflowMiddlewareDelegate next) => Next = next;
    protected WorkflowMiddlewareDelegate Next { get; }
    public abstract ValueTask InvokeAsync(WorkflowExecutionContext context);
}