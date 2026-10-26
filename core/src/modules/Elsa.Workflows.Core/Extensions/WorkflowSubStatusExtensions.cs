namespace Elsa.Workflows;

/// <summary>
/// Shared checks for the cancellation interim sub-status.
/// </summary>
public static class WorkflowSubStatusExtensions
{
    /// <summary>
    /// The sub-status Phase A will leave for <see cref="WorkflowSubStatus.Cancelling"/>.
    /// Pending stays on the pending run path and Interrupted stays on the interrupted-workflow scan,
    /// so a lost dispatch cannot take either of them out of recovery.
    /// </summary>
    public static bool IsCancellableSubStatus(this WorkflowSubStatus subStatus) =>
        subStatus == WorkflowSubStatus.Suspended;

    /// <summary>
    /// A transition into <see cref="WorkflowSubStatus.Cancelling"/> starts from <see cref="IsCancellableSubStatus"/>.
    /// A transition out of <see cref="WorkflowSubStatus.Cancelling"/> returns to that same sub-status.
    /// Any other pair is not a cancellation transition.
    /// </summary>
    public static bool IsPermittedSubStatusTransition(this WorkflowSubStatus expected, WorkflowSubStatus next)
    {
        if (next == WorkflowSubStatus.Cancelling)
            return expected.IsCancellableSubStatus();

        if (expected == WorkflowSubStatus.Cancelling)
            return next.IsCancellableSubStatus();

        return true;
    }

    /// <summary>
    /// Drops sub-millisecond ticks so stamps survive providers that round timestamps to microseconds.
    /// </summary>
    public static DateTimeOffset TruncateToMilliseconds(this DateTimeOffset value)
    {
        var ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTimeOffset(ticks, value.Offset);
    }
}
