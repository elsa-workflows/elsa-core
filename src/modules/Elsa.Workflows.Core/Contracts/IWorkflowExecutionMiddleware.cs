using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

public interface IWorkflowExecutionMiddleware
{
    ValueTask InvokeAsync(WorkflowExecutionContext context);
}