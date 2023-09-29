using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Results;
using Elsa.Workflows.Management.Contracts;

namespace Elsa.Alterations.Core.Services;

/// <inheritdoc />
public class DefaultAlterationPlanResultCommitter : IAlterationPlanResultCommitter
{
    private readonly IWorkflowInstanceManager _workflowInstanceManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAlterationPlanResultCommitter"/> class.
    /// </summary>
    public DefaultAlterationPlanResultCommitter(IWorkflowInstanceManager workflowInstanceManager)
    {
        _workflowInstanceManager = workflowInstanceManager;
    }

    /// <inheritdoc />
    public async Task CommitAsync(AlterationPlanExecutionResult result, CancellationToken cancellationToken = default)
    {
        if (!result.HasSucceeded)
            return;

        foreach (var workflowExecutionContext in result.ModifiedWorkflowExecutionContexts)
            await _workflowInstanceManager.SaveAsync(workflowExecutionContext, cancellationToken);
    }
}