using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Quartz;

namespace Elsa.Scheduling.Quartz;

/// <summary>
/// Conventions for Quartz trigger keys used by this module. Retry triggers keep the original schedule intact by using
/// a derived key rather than replacing the firing trigger. A reserved group and hash-based name keep retry identities
/// separate from caller-controlled task names, while the trigger data marker identifies retry triggers.
/// </summary>
public static class QuartzTriggerKeys
{
    /// <summary>
    /// Reserved Quartz group for one-shot retry triggers.
    /// </summary>
    public const string RetryGroup = "Elsa.Scheduling.Quartz:Retries";

    /// <summary>
    /// Reserved Quartz group for durable cancellation markers. A marker survives removal of acquired triggers so a
    /// late failure cannot recreate a retry for an explicitly unscheduled schedule. Markers are retained and updated
    /// across reschedules because an acquired execution can fail after an arbitrary delay. Scheduling intentionally
    /// does not delete the marker, leaving one durable row for each unique schedule key that was unscheduled.
    /// </summary>
    public const string CancellationGroup = "Elsa.Scheduling.Quartz:Cancellations";

    /// <summary>
    /// Returns the stable key of the one-shot retry trigger that belongs to the original <paramref name="triggerKey"/>.
    /// The key does not expose caller-controlled names and is distinct from every ordinary trigger in its tenant group.
    /// Schedule generations are persisted in trigger data and used as a fence; they do not change the retry identity.
    /// </summary>
    public static TriggerKey GetRetryTriggerKey(TriggerKey triggerKey)
    {
        var hash = GetIdentityHash(triggerKey);
        return new TriggerKey($"retry-{hash}", RetryGroup);
    }

    /// <summary>
    /// Returns the durable job key used as a cancellation marker for an original schedule. The marker is retained after
    /// unscheduling and records the one replacement generation allowed to schedule retries.
    /// </summary>
    public static JobKey GetCancellationMarkerJobKey(TriggerKey triggerKey)
    {
        return new JobKey($"cancellation-{GetIdentityHash(triggerKey)}", CancellationGroup);
    }

    /// <summary>
    /// Returns whether <paramref name="trigger"/> is a derived retry trigger rather than the original schedule.
    /// The explicit marker avoids confusing an ordinary trigger with a retry trigger.
    /// </summary>
    public static bool IsRetryTrigger(ITrigger trigger)
    {
        if (!trigger.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryTrigger, out var value))
            return false;

        return value switch
        {
            bool boolValue => boolValue,
            string stringValue => bool.TryParse(stringValue, out var parsedValue) && parsedValue,
            _ => false
        };
    }

    /// <summary>
    /// Gets the schedule generation carried by a trigger. Triggers created before generation metadata was added
    /// use a stable sentinel so their retry and cleanup behavior remains compatible.
    /// </summary>
    internal static string GetScheduleGeneration(ITrigger? trigger)
    {
        if (trigger?.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryScheduleGeneration, out var value) == true && value != null)
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? QuartzJobDataKeys.LegacyScheduleGeneration;

        return QuartzJobDataKeys.LegacyScheduleGeneration;
    }

    /// <summary>
    /// Gets a bounded distributed lock name for an original schedule. The group is part of the hashed identity so
    /// tenant schedules with the same task name do not block one another, while caller-controlled Quartz names do not
    /// become provider resource names.
    /// </summary>
    internal static string GetScheduleLockKey(TriggerKey triggerKey)
    {
        var hash = GetIdentityHash(triggerKey);
        return $"Elsa.Scheduling.Quartz:Schedule:{hash}";
    }

    private static string GetIdentityHash(TriggerKey triggerKey)
    {
        var identity = $"{triggerKey.Group}\u001F{triggerKey.Name}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant();
    }

    /// <summary>
    /// Returns the original schedule's key for <paramref name="trigger"/> using the original name and group persisted
    /// in the retry trigger's data.
    /// </summary>
    public static TriggerKey GetOriginalTriggerKey(ITrigger trigger)
    {
        var triggerKey = trigger.Key;

        if (!IsRetryTrigger(trigger))
            return triggerKey;

        if (!trigger.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryOriginalTriggerName, out var name) || name is not string triggerName)
            return triggerKey;

        if (!trigger.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryOriginalTriggerGroup, out var group) || group is not string triggerGroup)
            return triggerKey;

        return new TriggerKey(triggerName, triggerGroup);
    }
}
