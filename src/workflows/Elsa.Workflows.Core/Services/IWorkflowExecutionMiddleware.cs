using Elsa.Models;

namespace Elsa.Services;

public interface IWorkflowExecutionMiddleware
{
    ValueTask InvokeAsync(WorkflowExecutionContext context);
}