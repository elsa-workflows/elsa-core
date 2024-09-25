using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.WorkflowContexts.Middleware;

/// Middleware that loads and save workflow context into the currently executing workflow using installed workflow context providers. 
public class WorkflowContextWorkflowExecutionMiddleware : WorkflowExecutionMiddleware
{ 
    /// <inheritdoc />
    public WorkflowContextWorkflowExecutionMiddleware(WorkflowMiddlewareDelegate next) : base(next)
    {
    }
    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        // Check if the workflow contains any workflow context providers.
        if (!context.Workflow.CustomProperties.TryGetValue<ICollection<object>>(Constants.WorkflowContextProviderTypesKey, out var providerTypeObjects))
        {
            await Next(context);
            return;
        }

        var providerTypes = providerTypeObjects.Select(x => Type.GetType(x.ToString()!)).Where(x => x != null).Select(x => x!).ToList();

        // Invoke each workflow context provider.
        foreach (var providerType in providerTypes) 
            await context.LoadWorkflowContextAsync(providerType);

        // Invoke the next middleware.
        await Next(context);

        // Invoke each workflow context provider to persist the context.
        foreach (var providerType in providerTypes) 
            await context.SaveWorkflowContextAsync(providerType);
    }
}