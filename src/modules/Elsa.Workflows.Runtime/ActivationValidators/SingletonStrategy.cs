using System.ComponentModel.DataAnnotations;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Runtime.Contracts;

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
        var countArgs = new CountRunningWorkflowsArgs
        {
            DefinitionId = context.Workflow.Identity.DefinitionId
        };

        var count = await _workflowRuntime.CountRunningWorkflowsAsync(countArgs);
        return count == 0;
    }
}