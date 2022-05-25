using Elsa.Models;
using Elsa.WorkflowContexts.Extensions;
using Elsa.Pipelines.ActivityExecution;
using Elsa.Services;
using Elsa.WorkflowContexts.Models;
using Elsa.WorkflowContexts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowContexts.Middleware;

/// <summary>
/// Middleware that loads & save workflow context into the currently executing workflow using installed workflow context providers. 
/// </summary>
public class WorkflowContextActivityExecutionMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public WorkflowContextActivityExecutionMiddleware(ActivityMiddlewareDelegate next, IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        // Check if the workflow contains any workflow contexts.
        if (!context.WorkflowExecutionContext.Workflow.ApplicationProperties!.TryGetValue<ICollection<WorkflowContext>>("Elsa:WorkflowContexts", out var workflowContexts))
        {
            await _next(context);
            return;
        }

        // For each workflow context, invoke its provider.
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            foreach (var workflowContext in workflowContexts!)
            {
                var load = context.Activity.GetActivityWorkflowContextSettings(workflowContext).Load;
                if (!load) continue;

                var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, workflowContext.ProviderType);
                var value = await provider.LoadAsync(context.WorkflowExecutionContext);

                // Store the loaded value into the workflow execution context.
                context.WorkflowExecutionContext.SetWorkflowContext(workflowContext, value!);
            }
        }

        // Invoke the next middleware.
        await _next(context);

        // For each workflow context, invoke its provider to update the context.
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            foreach (var workflowContext in workflowContexts!)
            {
                var save = context.Activity.GetActivityWorkflowContextSettings(workflowContext).Load;
                if (!save) continue;

                // Get the loaded value from the workflow execution context.
                var value = context.WorkflowExecutionContext.GetWorkflowContext(workflowContext);

                var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, workflowContext.ProviderType);
                await provider.SaveAsync(context.WorkflowExecutionContext, value);
            }
        }
    }
}