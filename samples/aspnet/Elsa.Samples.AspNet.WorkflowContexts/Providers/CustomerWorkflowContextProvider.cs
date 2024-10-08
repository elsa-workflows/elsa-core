using Elsa.Samples.AspNet.WorkflowContexts.Contracts;
using Elsa.Samples.AspNet.WorkflowContexts.Entities;
using Elsa.Samples.AspNet.WorkflowContexts.Extensions;
using Elsa.WorkflowContexts.Abstractions;
using Elsa.Workflows;

namespace Elsa.Samples.AspNet.WorkflowContexts.Providers;

public class CustomerWorkflowContextProvider(ICustomerStore customerStore) : WorkflowContextProvider<Customer>
{
    protected override async ValueTask<Customer?> LoadAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        var customerId = workflowExecutionContext.GetCustomerId();
        return customerId != null ? await customerStore.GetAsync(customerId) : null;
    }

    protected override async ValueTask SaveAsync(WorkflowExecutionContext workflowExecutionContext, Customer? context)
    {
        if (context != null)
        {
            await customerStore.SaveAsync(context);
            workflowExecutionContext.SetCustomerId(context.Id);
        }
    }
}