using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Runtime.ActivationValidators;

/// <summary>
/// Only allow new workflow instances if a running one with the same correlation ID doesn't already exist.
/// Blank or missing correlation IDs are not unique and always allow activation.
/// </summary>
[Display(Name = "Correlation", Description = "Only allow new workflow instances of any workflow definition if a running one with the same non-blank correlation ID doesn't already exist.")]
public class CorrelationStrategy(IWorkflowInstanceStore workflowInstanceStore) : IWorkflowActivationStrategy
{
    /// <summary>
    /// Only allow a new instance if no running ones exist already. 
    /// </summary>
    public async ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context)
    {
        if (string.IsNullOrWhiteSpace(context.CorrelationId))
            return true;

        var filter = new WorkflowInstanceFilter
        {
            CorrelationId = context.CorrelationId,
            WorkflowStatus = WorkflowStatus.Running
        };

        var count = await workflowInstanceStore.CountAsync(filter, context.CancellationToken);
        return count == 0;
    }
}