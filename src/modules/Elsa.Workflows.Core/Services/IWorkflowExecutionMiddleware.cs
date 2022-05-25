using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

public interface IWorkflowExecutionMiddleware
{
    ValueTask InvokeAsync(WorkflowExecutionContext context);
}