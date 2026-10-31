

using Elsa.WorkflowContexts.Contracts;
using Microsoft.Extensions.DependencyInjection;
// ReSharper disable once CheckNamespace
using Elsa.Workflows;

namespace Elsa.Extensions;

public static class WorkflowExecutionContextExtensions
{
    public static async Task LoadWorkflowContextAsync(this WorkflowExecutionContext workflowExecutionContext, Type providerType)
    {
        // Load the context.
        var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(workflowExecutionContext.ServiceProvider, providerType);
        var value = await provider.LoadAsync(workflowExecutionContext);

        // Store the loaded value into the workflow execution context.
        workflowExecutionContext.SetWorkflowContext(providerType, value!);
    }
    
    public static async Task SaveWorkflowContextAsync(this WorkflowExecutionContext workflowExecutionContext, Type providerType)
    {
        var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(workflowExecutionContext.ServiceProvider, providerType);
        var value = workflowExecutionContext.GetWorkflowContext(providerType);
        await provider.SaveAsync(workflowExecutionContext, value);
    }
}