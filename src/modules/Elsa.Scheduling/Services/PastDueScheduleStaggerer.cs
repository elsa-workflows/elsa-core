using Elsa.Scheduling.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Scheduling.Services;

/// <summary>
/// Distributes already-due schedules over a bounded window to avoid dispatch storms during startup catch-up.
/// </summary>
public class PastDueScheduleStaggerer(IOptions<SchedulingOptions> options)
{
    private long _sequence;

    public TimeSpan GetDelay(TimeSpan calculatedDelay)
    {
        if (calculatedDelay > TimeSpan.Zero)
            return calculatedDelay;

        var currentOptions = options.Value;
        var minimumDelay = GetPositiveOrDefault(currentOptions.MinimumPastDueScheduleDelay, TimeSpan.FromMilliseconds(1));
        var staggerInterval = currentOptions.PastDueScheduleStaggerInterval;
        var staggerWindow = currentOptions.PastDueScheduleStaggerWindow;

        if (staggerInterval <= TimeSpan.Zero || staggerWindow <= TimeSpan.Zero)
            return minimumDelay;

        var slotCount = Math.Max(1, staggerWindow.Ticks / staggerInterval.Ticks);
        var sequence = Interlocked.Increment(ref _sequence) - 1;
        var slot = (sequence & long.MaxValue) % slotCount;

        return minimumDelay + TimeSpan.FromTicks(staggerInterval.Ticks * slot);
    }

    private static TimeSpan GetPositiveOrDefault(TimeSpan value, TimeSpan defaultValue) => value > TimeSpan.Zero ? value : defaultValue;
}
