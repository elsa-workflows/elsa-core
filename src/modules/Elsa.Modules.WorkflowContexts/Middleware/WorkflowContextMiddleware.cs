using Elsa.Extensions;
using Elsa.Models;
using Elsa.Modules.WorkflowContexts.Contracts;
using Elsa.Modules.WorkflowContexts.Models;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Pipelines.WorkflowExecution.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Modules.WorkflowContexts.Middleware;

/// <summary>
/// Middleware that loads & save workflow context into the currently executing workflow using installed workflow context providers. 
/// </summary>
public class WorkflowContextMiddleware : WorkflowExecutionMiddleware
{
    private readonly IServiceProvider _serviceProvider;

    public WorkflowContextMiddleware(WorkflowMiddlewareDelegate next, IServiceProvider serviceProvider) : base(next)
    {
        _serviceProvider = serviceProvider;
    }

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Check if the workflow contains any workflow contexts.
        if (!context.Workflow.ApplicationProperties!.TryGetValue<ICollection<WorkflowContext>>("Elsa:WorkflowContexts", out var workflowContexts))
        {
            await Next(context);
            return;
        }

        // For each workflow context, invoke its provider.
        foreach (var workflowContext in workflowContexts!)
        {
            var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, workflowContext.ProviderType);
            var value = await provider.LoadAsync(context);

            // Store the loaded value into the workflow execution context.
            var contextDictionary = context.TransientProperties.GetOrAdd("WorkflowContexts", () => new Dictionary<WorkflowContext, object?>());
            contextDictionary.Add(workflowContext, value);
        }

        // Invoke the next middleware.
        await Next(context);

        // For each workflow context, invoke its provider to update the context.
        foreach (var workflowContext in workflowContexts!)
        {
            // Get the loaded value from the workflow execution context.
            var contextDictionary = context.TransientProperties.GetOrAdd("WorkflowContexts", () => new Dictionary<WorkflowContext, object?>());
            var value = contextDictionary[workflowContext];

            var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, workflowContext.ProviderType);
            await provider.SaveAsync(context, value);
        }
    }
}