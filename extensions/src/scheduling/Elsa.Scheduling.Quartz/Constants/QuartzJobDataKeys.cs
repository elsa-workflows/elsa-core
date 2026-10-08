namespace Elsa.Scheduling.Quartz;

/// <summary>
/// Well-known keys written by this module into a Quartz <see cref="global::Quartz.JobDataMap"/>.
/// </summary>
public static class QuartzJobDataKeys
{
    /// <summary>
    /// The key used to mark a derived retry trigger. This marker is required because a caller may legitimately choose
    /// an original task name that ends with the retry suffix.
    /// </summary>
    public const string RetryTrigger = "Elsa.Scheduling.Quartz:RetryTrigger";

    /// <summary>
    /// The original trigger name carried by a retry trigger so later retries can reuse the same derived identity.
    /// </summary>
    public const string RetryOriginalTriggerName = "Elsa.Scheduling.Quartz:RetryOriginalTriggerName";

    /// <summary>
    /// The original trigger group carried by a retry trigger so later retries can reuse the same derived identity.
    /// </summary>
    public const string RetryOriginalTriggerGroup = "Elsa.Scheduling.Quartz:RetryOriginalTriggerGroup";

    /// <summary>
    /// The key under which the one-based retry attempt number is stored on a retry trigger. The value is stored
    /// as a string so that job stores configured with <c>quartz.jobStore.useProperties</c> can persist it.
    /// </summary>
    public const string RetryAttempt = "Elsa.Scheduling.Quartz:RetryAttempt";

    /// <summary>
    /// The value stored on a retry trigger indicating whether its original trigger is a recurring schedule. This lets
    /// the retry scheduler perform a post-schedule existence check without treating a naturally completed one-shot
    /// trigger as an externally unscheduled recurring chain.
    /// </summary>
    public const string RetryOriginalIsRecurring = "Elsa.Scheduling.Quartz:RetryOriginalIsRecurring";

    /// <summary>
    /// The value stored on a retry trigger indicating whether the original execution had another scheduled fire. This
    /// distinguishes a final finite recurring occurrence from a recurring schedule that was unexpectedly removed.
    /// </summary>
    public const string RetryOriginalHasNextFireTime = "Elsa.Scheduling.Quartz:RetryOriginalHasNextFireTime";

    /// <summary>
    /// The token identifying the schedule generation that produced a retry. New schedules get a fresh token; older
    /// triggers without this value use the stable legacy sentinel when compared with a retry.
    /// </summary>
    public const string RetryScheduleGeneration = "Elsa.Scheduling.Quartz:RetryScheduleGeneration";

    /// <summary>
    /// The schedule generation allowed by a persisted cancellation marker. When absent, the marker denies all retry
    /// generations; when present, only the matching generation may continue after a reschedule.
    /// </summary>
    public const string CancellationAllowedScheduleGeneration = "Elsa.Scheduling.Quartz:CancellationAllowedScheduleGeneration";

    /// <summary>
    /// Stable compatibility value for original triggers created before schedule-generation metadata was introduced.
    /// </summary>
    public const string LegacyScheduleGeneration = "legacy";
}
