using System.Text.Json;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Serialization.Converters;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowContexts.Middleware;

/// <summary>
/// Middleware that loads and save workflow context into the currently executing workflow using installed workflow context providers. 
/// </summary>
/// <inheritdoc />
public class WorkflowContextWorkflowExecutionMiddleware(WorkflowMiddlewareDelegate next, IServiceScopeFactory serviceScopeFactory, IWellKnownTypeRegistry wellKnownTypeRegistry) : WorkflowExecutionMiddleware(next)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    }.WithConverters(new TypeJsonConverter(wellKnownTypeRegistry));

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Check if the workflow contains any workflow context providers.
        if (!context.Workflow.PropertyBag.TryGetValue<ICollection<Type>>(Constants.WorkflowContextProviderTypesKey, out var providerTypes, _jsonSerializerOptions))
        {
            await Next(context);
            return;
        }

        // Invoke each workflow context provider.
        using (var scope = serviceScopeFactory.CreateScope())
        {
            foreach (var providerType in providerTypes)
            {
                var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
                var value = await provider.LoadAsync(context);

                // Store the loaded value into the workflow execution context.
                context.SetWorkflowContext(providerType, value!);
            }
        }

        // Invoke the next middleware.
        await Next(context);

        // Invoke each workflow context provider to persists the context.
        using (var scope = serviceScopeFactory.CreateScope())
        {
            foreach (var providerType in providerTypes)
            {
                // Get the loaded value from the workflow execution context.
                var value = context.GetWorkflowContext(providerType);

                var provider = (IWorkflowContextProvider)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
                await provider.SaveAsync(context, value);
            }
        }
    }
}