using Elsa.Samples.WorkflowContexts.Contracts;
using Elsa.Samples.WorkflowContexts.Extensions;
using Elsa.WorkflowContexts.Abstractions;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.WorkflowContexts.Providers;

public class CustomerWorkflowContextProvider : WorkflowContextProvider<Customer>
{
    private readonly ICustomerStore _customerStore;

    public CustomerWorkflowContextProvider(ICustomerStore customerStore)
    {
        _customerStore = customerStore;
    }

    protected override async ValueTask<Customer?> LoadAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        var customerId = workflowExecutionContext.GetCustomerId();
        return customerId != null ? await _customerStore.GetAsync(customerId) : null;
    }

    protected override async ValueTask SaveAsync(WorkflowExecutionContext workflowExecutionContext, Customer? context)
    {
        if (context != null)
        {
            await _customerStore.SaveAsync(context);
            workflowExecutionContext.SetCustomerId(context.Id);
        }
    }
}