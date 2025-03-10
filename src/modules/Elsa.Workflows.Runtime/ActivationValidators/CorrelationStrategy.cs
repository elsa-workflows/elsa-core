using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Runtime.ActivationValidators;

/// <summary>
/// Only allow new workflow instances if a running one with the same correlation ID doesn't already exist.
/// </summary>
[Display(Name = "Correlation", Description = "Only allow new workflow instances of any workflow definition if a running one with the same correlation ID doesn't already exist.")]
public class CorrelationStrategy(IWorkflowInstanceStore workflowInstanceStore) : IWorkflowActivationStrategy
{
    /// <summary>
    /// Only allow a new instance if no running ones exist already. 
    /// </summary>
    public async ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context)
    {
        var filter = new WorkflowInstanceFilter
        {
            CorrelationId = context.CorrelationId,
            WorkflowStatus = WorkflowStatus.Running
        };

        var count = await workflowInstanceStore.CountAsync(filter);
        return count == 0;
    }
}