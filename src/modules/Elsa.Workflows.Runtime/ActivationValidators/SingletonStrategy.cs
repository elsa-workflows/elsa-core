using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Requests;

namespace Elsa.Workflows.Runtime.ActivationValidators;

/// <summary>
/// Only allow new workflow instances if a running one of the same workflow definition doesn't already exist.
/// </summary>
[Display(Name = "Singleton", Description = "Only allow new workflow instances if a running one of the same workflow definition doesn't already exist.")]
public class SingletonStrategy : IWorkflowActivationStrategy
{
    private readonly IWorkflowRuntime _workflowRuntime;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SingletonStrategy(IWorkflowRuntime workflowRuntime)
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
            DefinitionId = context.Workflow.Identity.DefinitionId
        };

        var count = await _workflowRuntime.CountRunningWorkflowsAsync(countArgs);
        return count == 0;
    }
}