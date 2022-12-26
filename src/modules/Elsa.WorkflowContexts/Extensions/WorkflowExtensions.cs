using Elsa.WorkflowContexts.Models;
using Elsa.WorkflowContexts.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Services;

namespace Elsa.WorkflowContexts.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IWorkflowBuilder"/>.
/// </summary>
public static class WorkflowExtensions
{
    /// <summary>
    /// Installs the specified workflow context provider type into the specified workflow.
    /// </summary>
    public static WorkflowContext<T, TProvider> CreateWorkflowContext<T, TProvider>(this IWorkflowBuilder workflow) where TProvider : IWorkflowContextProvider
    {
        var workflowContext = new WorkflowContext<T, TProvider>();
        workflow.AddWorkflowContext(workflowContext);
        return workflowContext;
    }

    /// <summary>
    /// Installs the specified workflow context provider type into the specified workflow.
    /// </summary>
    public static IWorkflowBuilder AddWorkflowContext(this IWorkflowBuilder workflow, WorkflowContext workflowContext)
    {
        var providerTypes = workflow.CustomProperties!.GetOrAdd("Elsa:WorkflowContexts", () => new List<WorkflowContext>())!;

        providerTypes.Add(workflowContext);
        return workflow;
    }

    /// <summary>
    /// Installs the specified workflow context provider type into the specified workflow.
    /// </summary>
    public static IWorkflowBuilder AddWorkflowContexts(this IWorkflowBuilder workflow, params WorkflowContext[] workflowContexts)
    {
        foreach (var workflowContext in workflowContexts) workflow.AddWorkflowContext(workflowContext);
        return workflow;
    }
}