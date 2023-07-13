using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Pipelines.WorkflowExecution;

public abstract class WorkflowExecutionMiddleware : IWorkflowExecutionMiddleware
{
    protected WorkflowExecutionMiddleware(WorkflowMiddlewareDelegate next) => Next = next;
    protected WorkflowMiddlewareDelegate Next { get; }
    public abstract ValueTask InvokeAsync(WorkflowExecutionContext context);
}