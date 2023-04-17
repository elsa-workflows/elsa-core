using Elsa.Extensions;
using Elsa.WorkflowContexts.Contracts;
using Elsa.WorkflowContexts.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowContexts.Middleware;

/// <summary>
/// Middleware that loads and save workflow context into the currently executing workflow using installed workflow context providers. 
/// </summary>
public class WorkflowContextActivityExecutionMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowContextActivityExecutionMiddleware(ActivityMiddlewareDelegate next, IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        // Check if the workflow contains any workflow context providers.
        if (!context.WorkflowExecutionContext.Workflow.CustomProperties.TryGetValue<ICollection<Type>>(Constants.WorkflowContextProviderTypesKey, out var providerTypes))
        {
            await _next(context);
            return;
        }

        // Invoke each workflow context provider.
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            foreach (var providerType in providerTypes)
            {
                // Is the activity configured to load the context?
                var load = context.Activity.GetActivityWorkflowContextSettings(providerType).Load;
                if (!load) continue;

                // Load the context.
                var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
                var value = await provider.LoadAsync(context.WorkflowExecutionContext);

                // Store the loaded value into the workflow execution context.
                context.WorkflowExecutionContext.SetWorkflowContext(providerType, value!);
            }
        }

        // Invoke the next middleware.
        await _next(context);

        // Invoke each workflow context provider to persists the context.
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            foreach (var providerType in providerTypes)
            {
                // Is the activity configured to save the context?
                var save = context.Activity.GetActivityWorkflowContextSettings(providerType).Load;
                if (!save) continue;

                // Get the loaded value from the workflow execution context.
                var value = context.WorkflowExecutionContext.GetWorkflowContext(providerType);

                // Save the context.
                var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
                await provider.SaveAsync(context.WorkflowExecutionContext, value);
            }
        }
    }
}