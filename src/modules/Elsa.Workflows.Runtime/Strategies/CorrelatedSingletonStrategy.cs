using System.ComponentModel;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Runtime.Services;

namespace Elsa.Workflows.Runtime.Strategies;

/// <summary>
/// Only allow new workflow instances if a running one of the same workflow definition and correlation ID doesn't already exist.
/// </summary>
[DisplayName("Correlated singleton")]
public class CorrelatedSingletonStrategy : IWorkflowInstantiationStrategy
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
    public async ValueTask<bool> ShouldCreateInstanceAsync(WorkflowInstantiationStrategyContext context)
    {
        var countArgs = new CountRunningWorkflowsArgs
        {
            DefinitionId = context.Workflow.Identity.DefinitionId,
            CorrelationId = context.CorrelationId,
        };

        var count = await _workflowRuntime.CountRunningWorkflowsAsync(countArgs);
        return count == 0;
    }
}