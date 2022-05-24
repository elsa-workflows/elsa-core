using Elsa.Services;
using Elsa.WorkflowContexts.Models;
using Elsa.WorkflowContexts.Services;

namespace Elsa.WorkflowContexts.Extensions;

public static class WorkflowExtensions
{
    /// <summary>
    /// Installs the specified workflow context provider type into the specified workflow.
    /// </summary>
    public static WorkflowContext<T, TProvider> CreateWorkflowContext<T, TProvider>(this IWorkflowDefinitionBuilder workflow) where TProvider : IWorkflowContextProvider
    {
        var workflowContext = new WorkflowContext<T, TProvider>();
        workflow.AddWorkflowContext(workflowContext);
        return workflowContext;
    }

    /// <summary>
    /// Installs the specified workflow context provider type into the specified workflow.
    /// </summary>
    public static IWorkflowDefinitionBuilder AddWorkflowContext(this IWorkflowDefinitionBuilder workflow, WorkflowContext workflowContext)
    {
        var providerTypes = workflow.ApplicationProperties!.GetOrAdd("Elsa:WorkflowContexts", () => new List<WorkflowContext>())!;

        providerTypes.Add(workflowContext);
        return workflow;
    }

    /// <summary>
    /// Installs the specified workflow context provider type into the specified workflow.
    /// </summary>
    public static IWorkflowDefinitionBuilder AddWorkflowContexts(this IWorkflowDefinitionBuilder workflow, params WorkflowContext[] workflowContexts)
    {
        foreach (var workflowContext in workflowContexts) workflow.AddWorkflowContext(workflowContext);
        return workflow;
    }
}