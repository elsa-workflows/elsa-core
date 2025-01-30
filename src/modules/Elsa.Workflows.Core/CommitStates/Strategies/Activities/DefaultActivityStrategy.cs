using System.ComponentModel;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// Represents the default activity commit strategy for workflow activities.
/// </summary>
/// <remarks>
/// This strategy determines whether a workflow should commit changes during the execution of an activity.
/// By default, it delegates the commit behavior to the workflow's global commit options or other overriding strategies.
/// </remarks>
[DisplayName("Default")]
[Description("The default activity commit strategy.")]
public class DefaultActivityStrategy : IActivityCommitStrategy
{
    public CommitAction ShouldCommit(ActivityCommitStateStrategyContext context)
    {
        return CommitAction.Default;
    }
}