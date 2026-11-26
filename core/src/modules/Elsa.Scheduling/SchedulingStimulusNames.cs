namespace Elsa.Scheduling;

public static class SchedulingStimulusNames
{
    public const string Cron = "Elsa.Cron";
    public const string StartAt = "Elsa.StartAt";
    public const string Timer = "Elsa.Timer";
    public const string Delay = "Elsa.Delay";

    /// <summary>
    /// The names of the triggers that the default trigger scheduler schedules. <see cref="Delay"/> is a bookmark, not a trigger.
    /// </summary>
    internal static readonly IReadOnlyCollection<string> ScheduledTriggerNames = [Cron, StartAt, Timer];
}