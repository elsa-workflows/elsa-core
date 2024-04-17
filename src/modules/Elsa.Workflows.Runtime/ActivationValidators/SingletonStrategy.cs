using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.ActivationValidators;

/// <summary>
/// Only allow new workflow instances if a running one of the same workflow definition doesn't already exist.
/// </summary>
[Display(Name = "Singleton", Description = "Only allow new workflow instances if a running one of the same workflow definition doesn't already exist.")]
public class SingletonStrategy(IWorkflowInstanceStore workflowInstanceStore) : IWorkflowActivationStrategy
{
    /// <summary>
    /// Only allow a new instance if no running ones exist already. 
    /// </summary>
    public async ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context)
    {
        var filter = new WorkflowInstanceFilter
        {
            DefinitionId = context.Workflow.Identity.DefinitionId
        };

        var count = await workflowInstanceStore.CountAsync(filter);
        return count == 0;
    }
}