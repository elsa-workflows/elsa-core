using Elsa.Models;

namespace Elsa.Contracts;

public interface IWorkflowExecutionMiddleware
{
    ValueTask InvokeAsync(WorkflowExecutionContext context);
}