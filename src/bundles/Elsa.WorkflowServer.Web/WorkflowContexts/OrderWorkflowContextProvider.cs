using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows.Core;

namespace Elsa.WorkflowServer.Web.WorkflowContexts;

public class OrderWorkflowContextProvider : IWorkflowContextProvider
{
    public async ValueTask<object?> LoadAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        throw new NotImplementedException();
    }

    public async ValueTask SaveAsync(WorkflowExecutionContext workflowExecutionContext, object? context)
    {
        throw new NotImplementedException();
    }
}