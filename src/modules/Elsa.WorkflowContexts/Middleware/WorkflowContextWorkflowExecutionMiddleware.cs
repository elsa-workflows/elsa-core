using Elsa.Extensions;
using Elsa.WorkflowContexts.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowContexts.Middleware;

/// <summary>
/// Middleware that loads and save workflow context into the currently executing workflow using installed workflow context providers. 
/// </summary>
public class WorkflowContextWorkflowExecutionMiddleware : WorkflowExecutionMiddleware
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <inheritdoc />
    public WorkflowContextWorkflowExecutionMiddleware(WorkflowMiddlewareDelegate next, IServiceScopeFactory serviceScopeFactory) : base(next)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Check if the workflow contains any workflow context providers.
        if (!context.Workflow.CustomProperties.TryGetValue<ICollection<Type>>(Constants.WorkflowContextProviderTypesKey, out var providerTypes))
        {
            await Next(context);
            return;
        }

        // Invoke each workflow context provider.
        using (var scope = _serviceScopeFactory.CreateScope())
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
        using (var scope = _serviceScopeFactory.CreateScope())
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