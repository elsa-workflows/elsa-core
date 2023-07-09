using Elsa.Samples.AspNet.WorkflowContexts.Entities;
using Elsa.WorkflowContexts.Abstractions;
using Elsa.Workflows.Core;

namespace Elsa.Samples.AspNet.WorkflowContexts.Providers;

public class OrderWorkflowContextProvider : WorkflowContextProvider<Order>
{
    protected override ValueTask<Order?> LoadAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        return new(default(Order));
    }

    protected override ValueTask SaveAsync(WorkflowExecutionContext workflowExecutionContext, Order? context)
    {
        return ValueTask.CompletedTask;
    }
}