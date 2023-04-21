using Elsa.Samples.WorkflowContexts.Contracts;
using Elsa.Samples.WorkflowContexts.Extensions;
using Elsa.WorkflowContexts.Abstractions;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.WorkflowContexts.Providers;

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