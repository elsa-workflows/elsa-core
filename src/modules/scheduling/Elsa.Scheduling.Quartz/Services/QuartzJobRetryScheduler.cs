using System.Globalization;
using Elsa.Common;
using Elsa.Resilience;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Models;
using Elsa.Scheduling.Quartz.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using QuartzScheduler = global::Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// Default implementation of <see cref="IQuartzJobRetryScheduler"/>. Rather than retrying in-process, each retry is
/// scheduled as a separate one-shot Quartz trigger for the same job, so that a pending retry does not occupy a Quartz
/// worker thread while waiting, the original schedule (cron or repeating) is left intact, and the retry survives an
/// application restart when Quartz is configured with a persistent job store (with the default in-memory store,
/// pending retries are lost on restart).
/// </summary>
public class QuartzJobRetryScheduler(
    ISystemClock systemClock,
    IOptions<QuartzJobOptions> options,
    IQuartzRetryDelayCalculator delayCalculator,
    ITransientExceptionDetector transientExceptionDetector,
    ILogger<QuartzJobRetryScheduler> logger,
    IQuartzScheduleCoordinator? scheduleCoordinator = null) : IQuartzJobRetryScheduler
{
    /// <inheritdoc />
    public bool IsRetryable(Exception exception)
    {
        var isRetryable = options.Value.IsRetryable;
        return isRetryable != null ? isRetryable(exception) : transientExceptionDetector.IsTransient(exception);
    }

    /// <inheritdoc />
    public async Task<bool> ScheduleRetryAsync(IJobExecutionContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value;
        var jobKey = context.JobDetail.Key;

        if (!jobOptions.RetryEnabled)
        {
            logger.LogDebug("Retries are disabled. Not scheduling a retry for job {JobKey}", jobKey);
            return false;
        }

        var attemptsMade = Math.Max(context.GetRetryAttempt(), 0);

        if (attemptsMade >= jobOptions.MaxRetryAttempts)
            return false;

        var attemptNumber = attemptsMade + 1;
        var delay = GetDelay(context, exception, attemptNumber, jobOptions);
        var retryTrigger = CreateRetryTrigger(context, attemptNumber, delay);

        logger.LogWarning(
            exception,
            "Job {JobKey} failed with a retryable error. Scheduling retry {AttemptNumber} of {MaxRetryAttempts} in {RetryDelay}",
            jobKey,
            attemptNumber,
            jobOptions.MaxRetryAttempts,
            delay);

        var originalTriggerKey = QuartzTriggerKeys.GetOriginalTriggerKey(retryTrigger);
        await ExecuteCoordinatedAsync(originalTriggerKey, async token =>
        {
            // A recurring original can fire again while its deterministic retry is pending. Leave that pending retry in
            // place; replacing it would reset the retry chain to attempt 1 on every recurrence. A firing retry, on the
            // other hand, atomically replaces its own key to advance the chain to the next attempt.
            if (QuartzTriggerKeys.IsRetryTrigger(context.Trigger))
            {
                // The retry identity is stable across generations. Fence both the stored retry and the original before
                // replacing it so an acquired stale retry cannot advance a replacement generation's retry chain.
                var currentRetryTrigger = await context.Scheduler.GetTrigger(retryTrigger.Key, token);
                if (currentRetryTrigger != null && !HasSameScheduleGeneration(context.Trigger, currentRetryTrigger))
                {
                    logger.LogDebug("Retry trigger {RetryTriggerKey} was replaced by a newer generation; treating the acquired retry as handled", retryTrigger.Key);
                    return;
                }

                var currentOriginalTrigger = await context.Scheduler.GetTrigger(originalTriggerKey, token);
                if (currentOriginalTrigger != null && !HasSameScheduleGeneration(context.Trigger, currentOriginalTrigger))
                {
                    logger.LogDebug("Original trigger {OriginalTriggerKey} was replaced while retry {RetryTriggerKey} was in flight; treating the acquired retry as handled", originalTriggerKey, retryTrigger.Key);
                    return;
                }

                var nextFireTime = await context.Scheduler.RescheduleJob(retryTrigger.Key, retryTrigger, token);

                if (nextFireTime == null)
                {
                    // The retry may have been explicitly unscheduled after Quartz acquired it. Do not recreate a stale
                    // retry with ScheduleJob; the atomic reschedule correctly leaves the chain absent.
                    logger.LogDebug("Retry trigger {RetryTriggerKey} was no longer present while advancing retry for job {JobKey}", retryTrigger.Key, jobKey);
                    return;
                }
            }
            else
            {
                // A one-shot original can remain acquired after an unschedule+reschedule of the same task key. Its
                // retry must not recreate an old generation while the replacement original is already present. A
                // missing original is the normal post-fire one-shot state, so that case still schedules the retry.
                if (!IsRecurringTrigger(context.Trigger))
                {
                    var currentOriginalTrigger = await context.Scheduler.GetTrigger(originalTriggerKey, token);

                    if (currentOriginalTrigger != null && !HasSameScheduleGeneration(context.Trigger, currentOriginalTrigger))
                    {
                        await UnscheduleRetryIfGenerationMatchesAsync(context.Scheduler, retryTrigger, token);
                        logger.LogDebug("Original trigger {OriginalTriggerKey} was replaced while one-shot execution was in flight; removing stale retry {RetryTriggerKey}", originalTriggerKey, retryTrigger.Key);
                        return;
                    }
                }

                try
                {
                    await context.Scheduler.ScheduleJob(retryTrigger, token);
                }
                catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
                {
                    // Another concurrent execution won the race to create the deterministic retry key. The retry is already
                    // scheduled, so report success to the job and avoid turning an idempotent operation into a failed attempt.
                    logger.LogDebug("Retry trigger {RetryTriggerKey} already exists for job {JobKey}; keeping the existing retry", retryTrigger.Key, jobKey);
                }
                catch (ObjectAlreadyExistsException)
                {
                    // See the wrapped exception case above. Quartz may expose the duplicate directly depending on the store.
                    logger.LogDebug("Retry trigger {RetryTriggerKey} already exists for job {JobKey}; keeping the existing retry", retryTrigger.Key, jobKey);
                }
            }

            // The per-original lock makes this check and removal one coordinated operation with explicit unscheduling
            // and rescheduling. The generation token also prevents a retry from an old schedule from surviving an
            // unschedule+reschedule of the same task key.
            if (!await IsCurrentRetryScheduleAsync(context.Scheduler, retryTrigger, token))
            {
                await UnscheduleRetryIfGenerationMatchesAsync(context.Scheduler, retryTrigger, token);
                logger.LogDebug("Original trigger {OriginalTriggerKey} was removed or replaced while retry {RetryTriggerKey} was being scheduled; removing the retry", originalTriggerKey, retryTrigger.Key);
            }
        }, cancellationToken);

        return true;
    }

    private static bool IsOriginalRecurring(ITrigger retryTrigger)
    {
        if (!retryTrigger.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryOriginalIsRecurring, out var value))
            return false;

        return value switch
        {
            bool boolValue => boolValue,
            string stringValue => bool.TryParse(stringValue, out var parsedValue) && parsedValue,
            _ => false
        };
    }

    private static bool IsOriginalExpectedToRemain(ITrigger retryTrigger)
    {
        if (!IsOriginalRecurring(retryTrigger))
            return false;

        if (!retryTrigger.JobDataMap.TryGetValue(QuartzJobDataKeys.RetryOriginalHasNextFireTime, out var value))
            return true;

        return value switch
        {
            bool boolValue => boolValue,
            string stringValue => bool.TryParse(stringValue, out var parsedValue) && parsedValue,
            _ => false
        };
    }

    private TimeSpan GetDelay(IJobExecutionContext context, Exception exception, int attemptNumber, QuartzJobOptions jobOptions)
    {
        var computedDelay = delayCalculator.CalculateDelay(attemptNumber, jobOptions);
        var delayGenerator = jobOptions.DelayGenerator;

        if (delayGenerator == null)
            return computedDelay;

        var retryContext = new QuartzJobRetryContext
        {
            JobExecutionContext = context,
            Exception = exception,
            AttemptNumber = attemptNumber,
            MaxRetryAttempts = jobOptions.MaxRetryAttempts,
            ComputedDelay = computedDelay
        };

        var customDelay = delayGenerator(retryContext);

        if (customDelay == null)
            return computedDelay;

        return customDelay.Value > TimeSpan.Zero ? customDelay.Value : TimeSpan.Zero;
    }

    private ITrigger CreateRetryTrigger(IJobExecutionContext context, int attemptNumber, TimeSpan delay)
    {
        // Carry over the job data of the failed trigger so that the workflow inputs it carries are not lost. Use a
        // derived key so the original trigger (and its cron / repeating schedule) is left in place.
        var jobDataMap = new JobDataMap();
        var triggerJobDataMap = context.Trigger.JobDataMap;

        if (triggerJobDataMap != null)
            jobDataMap.PutAll(triggerJobDataMap);

        var originalTriggerKey = QuartzTriggerKeys.GetOriginalTriggerKey(context.Trigger);
        var scheduleGeneration = QuartzTriggerKeys.GetScheduleGeneration(context.Trigger);
        jobDataMap[QuartzJobDataKeys.RetryAttempt] = attemptNumber.ToString(CultureInfo.InvariantCulture);
        jobDataMap[QuartzJobDataKeys.RetryTrigger] = bool.TrueString;
        jobDataMap[QuartzJobDataKeys.RetryOriginalTriggerName] = originalTriggerKey.Name;
        jobDataMap[QuartzJobDataKeys.RetryOriginalTriggerGroup] = originalTriggerKey.Group;
        jobDataMap[QuartzJobDataKeys.RetryScheduleGeneration] = scheduleGeneration;

        if (!jobDataMap.ContainsKey(QuartzJobDataKeys.RetryOriginalIsRecurring))
            jobDataMap[QuartzJobDataKeys.RetryOriginalIsRecurring] = IsRecurringTrigger(context.Trigger).ToString();

        if (!jobDataMap.ContainsKey(QuartzJobDataKeys.RetryOriginalHasNextFireTime) && !QuartzTriggerKeys.IsRetryTrigger(context.Trigger))
            jobDataMap[QuartzJobDataKeys.RetryOriginalHasNextFireTime] = context.NextFireTimeUtc.HasValue.ToString();

        var now = systemClock.UtcNow;
        var startAt = delay >= DateTimeOffset.MaxValue - now ? DateTimeOffset.MaxValue : now.Add(delay);

        return TriggerBuilder.Create()
            .ForJob(context.JobDetail.Key)
            .WithIdentity(QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey))
            .UsingJobData(jobDataMap)
            .StartAt(startAt)
            .Build();
    }

    private static bool IsRecurringTrigger(ITrigger trigger) => trigger switch
    {
        ICronTrigger => true,
        ISimpleTrigger simpleTrigger => simpleTrigger.RepeatCount != 0,
        _ => false
    };

    private static bool HasSameScheduleGeneration(ITrigger left, ITrigger right) =>
        string.Equals(
            QuartzTriggerKeys.GetScheduleGeneration(left),
            QuartzTriggerKeys.GetScheduleGeneration(right),
            StringComparison.Ordinal);

    private static async Task UnscheduleRetryIfGenerationMatchesAsync(QuartzScheduler scheduler, ITrigger expectedTrigger, CancellationToken cancellationToken)
    {
        var currentRetryTrigger = await scheduler.GetTrigger(expectedTrigger.Key, cancellationToken);

        if (currentRetryTrigger == null || !HasSameScheduleGeneration(expectedTrigger, currentRetryTrigger))
            return;

        await scheduler.UnscheduleJob(expectedTrigger.Key, cancellationToken);
    }

    private static async Task<bool> IsCurrentRetryScheduleAsync(QuartzScheduler scheduler, ITrigger retryTrigger, CancellationToken cancellationToken)
    {
        var originalTrigger = await scheduler.GetTrigger(QuartzTriggerKeys.GetOriginalTriggerKey(retryTrigger), cancellationToken);

        if (originalTrigger == null)
            return !IsOriginalExpectedToRemain(retryTrigger);

        return string.Equals(
            QuartzTriggerKeys.GetScheduleGeneration(retryTrigger),
            QuartzTriggerKeys.GetScheduleGeneration(originalTrigger),
            StringComparison.Ordinal);
    }

    private Task ExecuteCoordinatedAsync(TriggerKey originalTriggerKey, Func<CancellationToken, Task> action, CancellationToken cancellationToken) =>
        scheduleCoordinator?.ExecuteAsync(originalTriggerKey, action, cancellationToken) ?? action(cancellationToken);
}
