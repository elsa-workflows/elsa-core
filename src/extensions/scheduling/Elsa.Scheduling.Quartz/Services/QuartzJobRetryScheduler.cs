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

        var originalTriggerKey = QuartzTriggerKeys.GetOriginalTriggerKey(retryTrigger);
        await ExecuteCoordinatedAsync(originalTriggerKey, async token =>
        {
            // The original trigger may already have been removed by explicit unscheduling. Check the durable marker
            // before touching the retry key so an acquired one-shot/final-occurrence execution cannot resurrect it.
            if (await IsCancellationMarkerBlockingAsync(context.Scheduler, retryTrigger, token))
            {
                logger.LogDebug("Schedule {OriginalTriggerKey} was explicitly cancelled; not scheduling retry {RetryTriggerKey}", originalTriggerKey, retryTrigger.Key);
                return;
            }

            logger.LogWarning(
                exception,
                "Job {JobKey} failed with a retryable error. Scheduling retry {AttemptNumber} of {MaxRetryAttempts} in {RetryDelay}",
                jobKey,
                attemptNumber,
                jobOptions.MaxRetryAttempts,
                delay);

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

                // Preserve the stable retry occupant seen immediately before scheduling. If Quartz reports a
                // duplicate but the occupant disappears before reconciliation, this snapshot lets recovery
                // distinguish a stale generation from an idempotent same-generation disappearance.
                var preScheduleRetryTrigger = await context.Scheduler.GetTrigger(retryTrigger.Key, token);

                try
                {
                    await context.Scheduler.ScheduleJob(retryTrigger, token);
                }
                catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
                {
                    await ReconcileDuplicateRetryAsync(context.Scheduler, retryTrigger, preScheduleRetryTrigger, token, jobKey);
                }
                catch (ObjectAlreadyExistsException)
                {
                    // See the wrapped exception case above. Quartz may expose the duplicate directly depending on the store.
                    await ReconcileDuplicateRetryAsync(context.Scheduler, retryTrigger, preScheduleRetryTrigger, token, jobKey);
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

    private async Task ReconcileDuplicateRetryAsync(
        QuartzScheduler scheduler,
        ITrigger proposedRetryTrigger,
        ITrigger? preScheduleRetryTrigger,
        CancellationToken cancellationToken,
        JobKey jobKey)
    {
        var storedRetryTrigger = await scheduler.GetTrigger(proposedRetryTrigger.Key, cancellationToken);

        if (storedRetryTrigger == null && preScheduleRetryTrigger != null && !HasSameScheduleGeneration(proposedRetryTrigger, preScheduleRetryTrigger))
        {
            // The duplicate proved that a stale retry occupied the stable key, but that retry disappeared before
            // reconciliation could read it. Make one bounded recovery attempt instead of losing the new generation.
            await ScheduleMissingRetryAsync(scheduler, proposedRetryTrigger, cancellationToken, jobKey);
            return;
        }

        if (storedRetryTrigger != null && !HasSameScheduleGeneration(proposedRetryTrigger, storedRetryTrigger))
        {
            // A prior generation can occupy the stable retry key after its original execution was explicitly
            // unscheduled. Replace only that stale generation; a same-generation duplicate remains idempotent.
            var nextFireTime = await scheduler.RescheduleJob(proposedRetryTrigger.Key, proposedRetryTrigger, cancellationToken);

            if (nextFireTime != null)
            {
                logger.LogDebug("Retry trigger {RetryTriggerKey} for job {JobKey} was replaced with the newer schedule generation", proposedRetryTrigger.Key, jobKey);
                return;
            }

            // RescheduleJob reports absence when the stale trigger disappears after the duplicate was observed. Make
            // one bounded ScheduleJob attempt so the proposed generation is not lost in that window.
            await ScheduleMissingRetryAsync(scheduler, proposedRetryTrigger, cancellationToken, jobKey);
            return;
        }

        // Another concurrent execution won the race to create the same generation, or the stale trigger disappeared
        // before it could be replaced. In either case, do not turn an idempotent retry request into a failed attempt.
        logger.LogDebug("Retry trigger {RetryTriggerKey} already exists for job {JobKey}; keeping the existing retry", proposedRetryTrigger.Key, jobKey);
    }

    private async Task ScheduleMissingRetryAsync(QuartzScheduler scheduler, ITrigger proposedRetryTrigger, CancellationToken cancellationToken, JobKey jobKey)
    {
        try
        {
            await scheduler.ScheduleJob(proposedRetryTrigger, cancellationToken);
            logger.LogDebug("Retry trigger {RetryTriggerKey} for job {JobKey} was scheduled after its stale predecessor disappeared", proposedRetryTrigger.Key, jobKey);
            return;
        }
        catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
        {
            // A competing execution may have recreated the stable key while the fallback was in flight.
        }
        catch (ObjectAlreadyExistsException)
        {
            // See the wrapped exception case above.
        }

        var competingRetryTrigger = await scheduler.GetTrigger(proposedRetryTrigger.Key, cancellationToken);

        if (competingRetryTrigger != null && HasSameScheduleGeneration(proposedRetryTrigger, competingRetryTrigger))
        {
            // Another execution already owns the proposed generation. Do not recurse or replace its idempotent retry.
            logger.LogDebug("Retry trigger {RetryTriggerKey} already exists for job {JobKey}; keeping the existing retry", proposedRetryTrigger.Key, jobKey);
            return;
        }

        if (competingRetryTrigger != null)
        {
            var nextFireTime = await scheduler.RescheduleJob(proposedRetryTrigger.Key, proposedRetryTrigger, cancellationToken);

            if (nextFireTime != null)
            {
                logger.LogDebug("Retry trigger {RetryTriggerKey} for job {JobKey} was replaced with the newer schedule generation", proposedRetryTrigger.Key, jobKey);
                return;
            }
        }

        // A competing stale trigger disappeared during the bounded recovery. One final direct scheduling attempt
        // closes that window without unbounded recursion; a duplicate at this point is left for the owning execution.
        try
        {
            await scheduler.ScheduleJob(proposedRetryTrigger, cancellationToken);
            logger.LogDebug("Retry trigger {RetryTriggerKey} for job {JobKey} was scheduled after competing stale retry disappeared", proposedRetryTrigger.Key, jobKey);
        }
        catch (JobPersistenceException e) when (e.InnerException is ObjectAlreadyExistsException)
        {
            logger.LogDebug("Retry trigger {RetryTriggerKey} already exists for job {JobKey}; keeping the existing retry", proposedRetryTrigger.Key, jobKey);
        }
        catch (ObjectAlreadyExistsException)
        {
            logger.LogDebug("Retry trigger {RetryTriggerKey} already exists for job {JobKey}; keeping the existing retry", proposedRetryTrigger.Key, jobKey);
        }
    }

    private static async Task<bool> IsCurrentRetryScheduleAsync(QuartzScheduler scheduler, ITrigger retryTrigger, CancellationToken cancellationToken)
    {
        if (await IsCancellationMarkerBlockingAsync(scheduler, retryTrigger, cancellationToken))
            return false;

        var originalTrigger = await scheduler.GetTrigger(QuartzTriggerKeys.GetOriginalTriggerKey(retryTrigger), cancellationToken);

        if (originalTrigger == null)
            return !IsOriginalExpectedToRemain(retryTrigger);

        return string.Equals(
            QuartzTriggerKeys.GetScheduleGeneration(retryTrigger),
            QuartzTriggerKeys.GetScheduleGeneration(originalTrigger),
            StringComparison.Ordinal);
    }

    private static async Task<bool> IsCancellationMarkerBlockingAsync(QuartzScheduler scheduler, ITrigger retryTrigger, CancellationToken cancellationToken)
    {
        var originalTriggerKey = QuartzTriggerKeys.GetOriginalTriggerKey(retryTrigger);
        var marker = await scheduler.GetJobDetail(QuartzTriggerKeys.GetCancellationMarkerJobKey(originalTriggerKey), cancellationToken);

        // No marker means this schedule has never been explicitly cancelled. Preserve the existing retry behavior for
        // ordinary schedules and for natural one-shot/final-recurrence completion.
        if (marker == null)
            return false;

        // A deny-all marker has no allowed generation. A reschedule writes exactly one allowed generation before its
        // replacement trigger is persisted, so a crash before or after trigger persistence cannot revive an old retry.
        var allowedGeneration = marker.JobDataMap.TryGetValue(QuartzJobDataKeys.CancellationAllowedScheduleGeneration, out var value) && value != null
            ? Convert.ToString(value, CultureInfo.InvariantCulture)
            : null;

        return !string.Equals(
            allowedGeneration,
            QuartzTriggerKeys.GetScheduleGeneration(retryTrigger),
            StringComparison.Ordinal);
    }

    private Task ExecuteCoordinatedAsync(TriggerKey originalTriggerKey, Func<CancellationToken, Task> action, CancellationToken cancellationToken) =>
        scheduleCoordinator?.ExecuteAsync(originalTriggerKey, action, cancellationToken) ?? action(cancellationToken);
}
