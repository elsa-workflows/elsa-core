using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Pipelines.ActivityExecution;
using JetBrains.Annotations;

namespace Elsa.WorkflowContexts.Middleware;

/// Middleware that loads and save workflow context into the currently executing workflow using installed workflow context providers. 
[UsedImplicitly]
public class WorkflowContextActivityExecutionMiddleware(ActivityMiddlewareDelegate next) : IActivityExecutionMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        // Check if the workflow contains any workflow context providers.
        if (!context.WorkflowExecutionContext.Workflow.CustomProperties.TryGetValue<ICollection<object>>(Constants.WorkflowContextProviderTypesKey, out var providerTypeNodes))
        {
            await next(context);
            return;
        }
        
        var providerTypes = providerTypeNodes.Select(x => Type.GetType(x.ToString()!)).Where(x => x != null).ToList();

        // Check if this is a background execution.
        var isBackgroundExecution = context.GetIsBackgroundExecution();

        // Is the activity configured to load the context?
        foreach (var providerType in providerTypes)
        {
            // Is the activity configured to load the context or is this a background execution?
            var load = isBackgroundExecution || context.Activity.GetActivityWorkflowContextSettings(providerType).Load;
            if (!load) continue;

            // Load the context.
            await context.WorkflowExecutionContext.LoadWorkflowContextAsync(providerType);
        }

        // Invoke the next middleware.
        await next(context);

        // Invoke each workflow context provider to persist the context.
        foreach (var providerType in providerTypes)
        {
            // Is the activity configured to save the context, or is this a background execution?
            var save = isBackgroundExecution || context.Activity.GetActivityWorkflowContextSettings(providerType).Load;
            if (!save) continue;

            // Save the context.
            await context.WorkflowExecutionContext.SaveWorkflowContextAsync(providerType);
        }
    }
}