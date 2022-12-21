using System.ComponentModel;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.ActivationValidators;

/// <summary>
/// Only allow new workflow instances if a running one with the same correlation ID doesn't already exist.
/// </summary>
[DisplayName("Uniquely correlated")]
public class UniqueCorrelationStrategy : IWorkflowActivationStrategy
{
    private readonly IWorkflowRuntime _workflowRuntime;

    /// <summary>
    /// Constructor.
    /// </summary>
    public UniqueCorrelationStrategy(IWorkflowRuntime workflowRuntime)
    {
        _workflowRuntime = workflowRuntime;
    }
    
    /// <summary>
    /// Only allow a new instance if no running ones exists already. 
    /// </summary>
    public async ValueTask<bool> GetAllowActivationAsync(WorkflowInstantiationStrategyContext context)
    {
        var countArgs = new CountRunningWorkflowsArgs
        {
            CorrelationId = context.CorrelationId,
        };

        var count = await _workflowRuntime.CountRunningWorkflowsAsync(countArgs);
        return count == 0;
    }
}