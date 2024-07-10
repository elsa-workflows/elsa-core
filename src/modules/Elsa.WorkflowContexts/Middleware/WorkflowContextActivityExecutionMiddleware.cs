using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Serialization.Converters;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowContexts.Middleware;

/// Middleware that loads and save workflow context into the currently executing workflow using installed workflow context providers. 
[UsedImplicitly]
public class WorkflowContextActivityExecutionMiddleware(ActivityMiddlewareDelegate next, IServiceScopeFactory serviceScopeFactory, IWellKnownTypeRegistry wellKnownTypeRegistry) : IActivityExecutionMiddleware
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }.WithConverters(new TypeJsonConverter(wellKnownTypeRegistry));

    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        // Check if the workflow contains any workflow context providers.
        if (!context.WorkflowExecutionContext.Workflow.PropertyBag.TryGetValue<ICollection<Type>>(Constants.WorkflowContextProviderTypesKey, out var providerTypes, _jsonSerializerOptions))
        {
            await next(context);
            return;
        }

        if (providerTypes.Count == 0)
        {
            await next(context);
            return;
        }

        // Check if this is a background execution.
        var isBackgroundExecution = context.GetIsBackgroundExecution();

        // Is the activity configured to load the context?
        foreach (var providerType in providerTypes)
        {
            // Is the activity configured to load the context, or is this a background execution?
            var load = isBackgroundExecution || context.Activity.GetActivityWorkflowContextSettings(providerType).Load;
            if (!load) continue;

            // Load the context.
            using var scope = serviceScopeFactory.CreateScope();
            var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            var value = await provider.LoadAsync(context.WorkflowExecutionContext);

            // Store the loaded value into the workflow execution context.
            context.WorkflowExecutionContext.SetWorkflowContext(providerType, value!);
        }

        // Invoke the next middleware.
        await next(context);

        // Invoke each workflow context provider to persists the context.
        foreach (var providerType in providerTypes)
        {
            // Is the activity configured to save the context or is this a background execution?
            var save = isBackgroundExecution || context.Activity.GetActivityWorkflowContextSettings(providerType).Load;
            if (!save) continue;

            // Get the loaded value from the workflow execution context.
            using var scope = serviceScopeFactory.CreateScope();
            var value = context.WorkflowExecutionContext.GetWorkflowContext(providerType);

            // Save the context.
            var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
            await provider.SaveAsync(context.WorkflowExecutionContext, value);
        }
    }
}