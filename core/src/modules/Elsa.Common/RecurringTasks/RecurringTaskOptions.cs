namespace Elsa.Common.RecurringTasks;

public class RecurringTaskOptions
{
    public RecurringTaskSchedule Schedule { get; } = new();

    /// <summary>
    /// The maximum number of tenants a recurring task type runs for at the same time, per task type.
    /// Values below 1 run one tenant at a time. Defaults to 4.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 4;
}