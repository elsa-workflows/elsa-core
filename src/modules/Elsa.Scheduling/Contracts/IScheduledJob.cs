namespace Elsa.Scheduling.Contracts;

/// <summary>
/// Represents a scheduled task.
/// </summary>
public interface IScheduledTask
{
    /// <summary>
    /// Cancels the scheduled task.
    /// </summary>
    void Cancel();
}