using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Runtime.ActivationValidators;

/// <summary>
/// Only allow new workflow instances if a running one of the same workflow definition and correlation ID doesn't already exist.
/// </summary>
[Display(Name = "Correlated singleton", Description = "Only allow new workflow instances if a running one of the same workflow definition and correlation ID doesn't already exist.")]
public class CorrelatedSingletonStrategy(IWorkflowInstanceStore workflowInstanceStore) : IWorkflowActivationStrategy
{
    /// <summary>
    /// Only allow a new instance if no running ones exist already. 
    /// </summary>
    public async ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context)
    {
        var filter = new WorkflowInstanceFilter
        {
            DefinitionId = context.Workflow.Identity.DefinitionId,
            CorrelationId = context.CorrelationId,
            WorkflowStatus = WorkflowStatus.Running
        };

        var count = await workflowInstanceStore.CountAsync(filter, context.CancellationToken);
        return count == 0;
    }
}