using Elsa.Common;

namespace Elsa.Workflows.CommitStates.Strategies.Workflows;

public class PeriodicWorkflowStrategy : IWorkflowCommitStrategy
{
    private static readonly object LastCommitPropertyKey = new();
    
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