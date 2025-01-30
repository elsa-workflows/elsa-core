using System.ComponentModel;
using Elsa.Common;

namespace Elsa.Workflows.CommitStates.Strategies;

/// <summary>
/// Implements a periodic workflow commit strategy based on a specified time interval.
/// This strategy determines if a workflow should commit by comparing the elapsed time
/// since the last commit with the configured interval.
/// </summary>
[DisplayName("Periodic")]
[Description("Determines whether a workflow state should be committed based on a specified time interval.")]
public class PeriodicWorkflowStrategy : IWorkflowCommitStrategy
{
    private static readonly object LastCommitPropertyKey = new();
    
    public static PeriodicWorkflowStrategy Create(TimeSpan interval) => new() { Interval = interval };
    
    public TimeSpan Interval { get; set; }

    public CommitAction ShouldCommit(WorkflowCommitStateStrategyContext context)
    {
        var lastCommit = context.WorkflowExecutionContext.TransientProperties.TryGetValue(LastCommitPropertyKey, out var value) ? (DateTimeOffset?)value : null;
        var now = context.WorkflowExecutionContext.GetRequiredService<ISystemClock>().UtcNow;
        var shouldCommit = lastCommit == null || (now - lastCommit.Value).TotalMilliseconds > Interval.TotalMilliseconds;

        if (shouldCommit)
            context.WorkflowExecutionContext.TransientProperties[LastCommitPropertyKey] = now;

        return shouldCommit ? CommitAction.Commit : CommitAction.Default;
    }
}