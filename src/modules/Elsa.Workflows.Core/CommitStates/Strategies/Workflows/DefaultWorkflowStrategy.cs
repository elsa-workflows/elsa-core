using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// Represents the default strategy for determining whether a workflow should commit its state.
/// </summary>
/// <remarks>
/// This strategy always returns the default commit action as defined by the <see cref="CommitAction"/> enum,
/// ensuring that workflows adhere to the standard behavior unless overridden by custom strategies.
/// </remarks>
[DisplayName("Default")]
[Description("The default workflow commit strategy.")]
public class DefaultWorkflowStrategy : IWorkflowCommitStrategy
{
    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        return CommitAction.Default;
    }
}