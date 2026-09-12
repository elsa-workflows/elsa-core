namespace Elsa.Scheduling.Options;

/// <summary>
/// Configures local scheduling behavior.
/// </summary>
public class SchedulingOptions
{
    /// <summary>
    /// The number of stored triggers and bookmarks to load per batch when rebuilding local schedules on startup.
    /// </summary>
    public int StartupSchedulePageSize { get; set; } = 1000;

    /// <summary>
    /// The minimum delay used when a specific-instant schedule is already due.
    /// </summary>
    public TimeSpan MinimumPastDueScheduleDelay { get; set; } = TimeSpan.FromMilliseconds(1);

    /// <summary>
    /// The spacing between past-due schedules during catch-up.
    /// </summary>
    public TimeSpan PastDueScheduleStaggerInterval { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// The bounded window over which past-due schedules are distributed.
    /// </summary>
    public TimeSpan PastDueScheduleStaggerWindow { get; set; } = TimeSpan.FromMinutes(5);
}
