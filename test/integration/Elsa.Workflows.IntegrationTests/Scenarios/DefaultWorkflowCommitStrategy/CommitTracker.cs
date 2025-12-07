using Elsa.Workflows.CommitStates;
using Elsa.Workflows.State;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DefaultWorkflowCommitStrategy;

/// <summary>
/// Tracks commit operations for testing purposes.
/// </summary>
public class CommitTracker : ICommitStateHandler
{
    public int CommitCount { get; private set; }

    public Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, CancellationToken cancellationToken = default)
    {
        CommitCount++;
        return Task.CompletedTask;
    }

    public Task CommitAsync(WorkflowExecutionContext workflowExecutionContext, WorkflowState workflowState, CancellationToken cancellationToken = default)
    {
        CommitCount++;
        return Task.CompletedTask;
    }
}
