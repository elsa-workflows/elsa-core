namespace Elsa.Workflows;

/// <summary>
/// Shared checks for the cancellation interim sub-status.
/// </summary>
public static class WorkflowSubStatusExtensions
{
    /// <summary>
    /// Sub-statuses a non-executing running instance may leave for <see cref="WorkflowSubStatus.Cancelling"/>.
    /// </summary>
    public static bool IsCancellableSubStatus(this WorkflowSubStatus subStatus) =>
        subStatus is WorkflowSubStatus.Pending or WorkflowSubStatus.Suspended or WorkflowSubStatus.Interrupted;

    /// <summary>
    /// Drops sub-millisecond ticks so stamps survive providers that round timestamps to microseconds.
    /// </summary>
    public static DateTimeOffset TruncateToMilliseconds(this DateTimeOffset value)
    {
        var ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTimeOffset(ticks, value.Offset);
    }
}
