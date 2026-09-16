using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;

namespace Elsa.Workflows.Runtime.ActivationValidators;

/// <summary>
/// Only allow new workflow instances if a running one of the same workflow definition and correlation ID doesn't already exist.
/// A non-blank correlation ID is required for this strategy.
/// </summary>
[Display(Name = "Correlated singleton", Description = "Only allow new workflow instances if a running one of the same workflow definition and non-blank correlation ID doesn't already exist.")]
public class CorrelatedSingletonStrategy(IWorkflowInstanceStore workflowInstanceStore) : IWorkflowActivationStrategy
{
    /// <summary>
    /// Only allow a new instance if no running ones exist already. 
    /// </summary>
    public async ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context)
    {
        if (string.IsNullOrWhiteSpace(context.CorrelationId))
            throw new ArgumentException("A non-blank correlation ID is required when using CorrelatedSingletonStrategy.", nameof(context.CorrelationId));

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
