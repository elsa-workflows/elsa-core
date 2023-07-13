namespace Elsa.Workflows.Core.Contracts;

public interface IWorkflowExecutionMiddleware
{
    ValueTask InvokeAsync(WorkflowExecutionContext context);
}