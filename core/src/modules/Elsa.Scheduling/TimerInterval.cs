namespace Elsa.Scheduling.ScheduledTasks;

/// <summary>
/// Caps delays for <see cref="System.Timers.Timer"/>, which rejects intervals above <see cref="int.MaxValue"/> milliseconds.
/// </summary>
internal static class TimerInterval
{
    internal static readonly TimeSpan Max = TimeSpan.FromMilliseconds(int.MaxValue);

    internal static TimeSpan Clamp(TimeSpan delay) => delay > Max ? Max : delay;
}
