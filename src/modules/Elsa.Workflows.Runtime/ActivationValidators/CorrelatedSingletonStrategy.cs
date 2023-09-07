using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.ActivationValidators;

/// <summary>
/// Only allow new workflow instances if a running one of the same workflow definition and correlation ID doesn't already exist.
/// </summary>
[Display(Name = "Correlated singleton", Description = "Only allow new workflow instances if a running one of the same workflow definition and correlation ID doesn't already exist.")]
public class CorrelatedSingletonStrategy : IWorkflowActivationStrategy
{
    private readonly IWorkflowRuntime _workflowRuntime;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CorrelatedSingletonStrategy(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }
    
    /// <summary>
    /// Only allow a new instance if no running ones exists already. 
    /// </summary>
    public async ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context)
    {
        var countArgs = new CountRunningWorkflowsRequest
        {
            DefinitionId = context.Workflow.Identity.DefinitionId,
            CorrelationId = context.CorrelationId,
        };

        var count = await _workflowRuntime.CountRunningWorkflowsAsync(countArgs);
        return count == 0;
    }
}