using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.ActivationValidators;

/// <summary>
/// Only allow new workflow instances if a running one with the same correlation ID doesn't already exist.
/// </summary>
[Display(Name = "Correlation", Description = "Only allow new workflow instances of any workflow definition if a running one with the same correlation ID doesn't already exist.")]
public class CorrelationStrategy : IWorkflowActivationStrategy
{
    private readonly IWorkflowRuntime _workflowRuntime;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CorrelationStrategy(IWorkflowRuntime workflowRuntime)
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
            CorrelationId = context.CorrelationId,
        };

        var count = await _workflowRuntime.CountRunningWorkflowsAsync(countArgs);
        return count == 0;
    }
}