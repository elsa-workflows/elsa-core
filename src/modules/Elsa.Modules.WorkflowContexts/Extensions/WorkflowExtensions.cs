using Elsa.Contracts;
using Elsa.Extensions;
using Elsa.Modules.WorkflowContexts.Contracts;
using Elsa.Modules.WorkflowContexts.Models;

namespace Elsa.Modules.WorkflowContexts.Extensions;

public static class WorkflowExtensions
{
    /// <summary>
    /// Installs the specified workflow context provider type into the specified workflow.
    /// </summary>
    public static IWorkflowDefinitionBuilder AddWorkflowContext<T, TProvider>(this IWorkflowDefinitionBuilder workflow, WorkflowContext<T, TProvider> workflowContext) where TProvider : IWorkflowContextProvider
    {
        var providerTypes = workflow.ApplicationProperties!.GetOrAdd("Elsa:WorkflowContexts", () => new List<WorkflowContext>());

        providerTypes.Add(workflowContext);
        return workflow;
    }
    
    
}