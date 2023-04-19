using Elsa.Extensions;
using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Runtime.Middleware.Activities;
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

        // Check if this is a background execution.
        var isBackgroundExecution = context.TransientProperties.GetValueOrDefault<object, bool>(BackgroundActivityInvokerMiddleware.IsBackgroundExecution);

        // Is the activity configured to load the context?
        foreach (var providerType in providerTypes)
        {
            // Is the activity configured to load the context or is this a background execution?
            var load = isBackgroundExecution || context.Activity.GetActivityWorkflowContextSettings(providerType).Load;
            if (!load) continue;

            // Load the context.
            using var scope = _serviceScopeFactory.CreateScope();
            var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            var value = await provider.LoadAsync(context.WorkflowExecutionContext);

            // Store the loaded value into the workflow execution context.
            context.WorkflowExecutionContext.SetWorkflowContext(providerType, value!);
        }

        // Invoke the next middleware.
        await _next(context);

        // Invoke each workflow context provider to persists the context.
        foreach (var providerType in providerTypes)
        {
            // Is the activity configured to save the context or is this a background execution?
            var save = isBackgroundExecution || context.Activity.GetActivityWorkflowContextSettings(providerType).Load;
            if (!save) continue;

            // Get the loaded value from the workflow execution context.
            using var scope = _serviceScopeFactory.CreateScope();
            var value = context.WorkflowExecutionContext.GetWorkflowContext(providerType);

            // Save the context.
            var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            await provider.SaveAsync(context.WorkflowExecutionContext, value);
        }
    }
}