using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Resilience;
using Elsa.Scheduling.Quartz;
using Elsa.Scheduling.Quartz.ComponentTests.Abstractions;
using Elsa.Scheduling.Quartz.ComponentTests.Fixtures;
using Elsa.Scheduling.Quartz.ComponentTests.Helpers;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.Options;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.ComponentTests;

/// <summary>
/// Component tests for Quartz job transient retry behavior.
/// These tests validate that jobs properly retry on transient exceptions, count their attempts across retry triggers,
/// stop once the configured maximum is reached, leave recurring schedules intact, and give up immediately on
/// non-transient exceptions.
/// </summary>
public class QuartzJobTransientRetryTests(SchedulingApp app) : AppComponentTest(app)
{
    private const string DefinitionVersionIdKey = "DefinitionVersionId";
    private const string DefinitionVersionId = "test-workflow-def";

    [Theory]
    [InlineData(2, typeof(TimeoutException), "Simulated transient timeout")]
    [InlineData(1, typeof(HttpRequestException), "Simulated network error")]
    public async Task RunWorkflowJob_TransientException_RetriesAndEventuallySucceeds(
        int failuresBeforeSuccess,
        Type exceptionType,
        string exceptionMessage)
    {
        var scenario = await CreateScenarioAsync(
            $"test-transient-{failuresBeforeSuccess}",
            failuresBeforeSuccess,
            (Exception)Activator.CreateInstance(exceptionType, exceptionMessage)!);

        // Every failing execution schedules the next retry and counts it on the derived retry trigger.
        for (var attempt = 1; attempt <= failuresBeforeSuccess; attempt++)
        {
            await scenario.ExecuteAsync();

            Assert.Equal(attempt, scenario.Starter.CallCount);
            Assert.Equal(attempt, await scenario.GetRetryAttemptAsync());

            // The workflow inputs carried by the original trigger must survive onto the retry trigger.
            Assert.Equal(DefinitionVersionId, (await scenario.GetRetryTriggerAsync())!.JobDataMap.GetString(DefinitionVersionIdKey));
        }

        // The next execution succeeds, so no further retry is scheduled and the attempt count stops growing.
        await scenario.ExecuteAsync();

        Assert.Equal(failuresBeforeSuccess + 1, scenario.Starter.CallCount);
        Assert.Equal(failuresBeforeSuccess, await scenario.GetRetryAttemptAsync());
    }

    [Fact]
    public async Task RunWorkflowJob_TransientException_StopsAfterMaxRetryAttempts()
    {
        const int maxRetryAttempts = 2;
        var scenario = await CreateScenarioAsync(
            "test-exhaustion",
            failuresBeforeSuccess: int.MaxValue,
            new TimeoutException("Simulated permanent outage"),
            options => options.MaxRetryAttempts = maxRetryAttempts);

        for (var attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            await scenario.ExecuteAsync();
            Assert.Equal(attempt, await scenario.GetRetryAttemptAsync());
        }

        // The retry budget is now exhausted: the job runs once more, but is not rescheduled again.
        await scenario.ExecuteAsync();

        Assert.Equal(maxRetryAttempts + 1, scenario.Starter.CallCount);
        Assert.Equal(maxRetryAttempts, await scenario.GetRetryAttemptAsync());

        // Exhausting the retries must not delete the job: that is reserved for non-transient failures.
        Assert.True(await scenario.Scheduler.CheckExists(scenario.Context.JobDetail.Key));
    }

    [Fact]
    public async Task RunWorkflowJob_RetriesDisabled_DoesNotReschedule()
    {
        var scenario = await CreateScenarioAsync(
            "test-retries-disabled",
            failuresBeforeSuccess: int.MaxValue,
            new TimeoutException("Simulated transient timeout"),
            options => options.RetryEnabled = false);

        await scenario.ExecuteAsync();

        Assert.Equal(1, scenario.Starter.CallCount);
        Assert.Equal(0, await scenario.GetRetryAttemptAsync());
        Assert.Null(await scenario.GetRetryTriggerAsync());
        Assert.True(await scenario.Scheduler.CheckExists(scenario.Context.JobDetail.Key));
    }

    [Fact]
    public async Task RunWorkflowJob_NonTransientException_DoesNotRetry()
    {
        var scenario = await CreateScenarioAsync(
            "test-nontransient",
            failuresBeforeSuccess: int.MaxValue,
            new InvalidOperationException("Non-transient error"));

        await scenario.ExecuteAsync();

        // Assert - Job should be called once, then deleted (not retried)
        Assert.Equal(1, scenario.Starter.CallCount);
        Assert.Equal(0, await scenario.GetRetryAttemptAsync());
        Assert.Null(await scenario.GetRetryTriggerAsync());
        Assert.False(await scenario.Scheduler.CheckExists(scenario.Context.JobDetail.Key));
    }

    [Fact]
    public async Task RunWorkflowJob_CronTrigger_TransientFailure_KeepsTheCronSchedule()
    {
        var scenario = await CreateScenarioAsync(
            "test-cron-survives-retry",
            failuresBeforeSuccess: 1,
            new TimeoutException("Simulated transient timeout"),
            triggerFactory: (identity, jobDetail) => TriggerBuilder.Create()
                .WithIdentity(identity)
                .ForJob(jobDetail)
                .UsingJobData(DefinitionVersionIdKey, DefinitionVersionId)
                .WithCronSchedule("0 0 12 1 1 ? 2099")
                .Build());

        await scenario.ExecuteAsync();

        Assert.Equal(1, scenario.Starter.CallCount);
        Assert.Equal(1, await scenario.GetRetryAttemptAsync());
        Assert.NotNull(await scenario.GetRetryTriggerAsync());

        var cronAfterFailure = await scenario.GetOriginalTriggerAsync();
        Assert.NotNull(cronAfterFailure);
        Assert.IsAssignableFrom<ICronTrigger>(cronAfterFailure);

        // The retry succeeds. The original cron trigger must still be the same schedule.
        await scenario.ExecuteAsync();

        Assert.Equal(2, scenario.Starter.CallCount);

        var cronAfterRetry = await scenario.GetOriginalTriggerAsync();
        Assert.NotNull(cronAfterRetry);
        Assert.IsAssignableFrom<ICronTrigger>(cronAfterRetry);
        Assert.Equal(cronAfterFailure!.Key, cronAfterRetry!.Key);

        // A later cron occurrence is a fresh attempt. The pending retry remains eligible; allowing it to coexist
        // avoids the race where its execution recreates the deterministic retry trigger after cancellation.
        scenario.Context.Trigger = cronAfterRetry;
        await scenario.ExecuteAsync();

        Assert.Equal(3, scenario.Starter.CallCount);
        Assert.NotNull(await scenario.GetRetryTriggerAsync());
        Assert.IsAssignableFrom<ICronTrigger>((await scenario.GetOriginalTriggerAsync())!);
    }

    [Fact]
    public async Task RunWorkflowJob_OriginalTriggerFiresWhileRetryPending_LeavesThePendingRetryInPlace()
    {
        var scenario = await CreateScenarioAsync(
            "test-cancel-pending-retry",
            failuresBeforeSuccess: 1,
            new TimeoutException("Simulated transient timeout"));

        await scenario.ExecuteAsync();
        Assert.NotNull(await scenario.GetRetryTriggerAsync());

        // Replay the original trigger as a later scheduled occurrence. The retry remains in place intentionally: the
        // original and retry executions may overlap, but cancellation must not race with retry-chain advancement.
        scenario.Context.Trigger = (await scenario.GetOriginalTriggerAsync())!;
        scenario.Starter.FailuresBeforeSuccess = 0;
        await scenario.ExecuteAsync();

        Assert.Equal(2, scenario.Starter.CallCount);
        Assert.NotNull(await scenario.GetRetryTriggerAsync());
    }

    /// <summary>
    /// Schedules a durable job with a trigger that carries the workflow payload, and wires a <see cref="RunWorkflowJob"/>
    /// around a workflow starter that fails the requested number of times. The trigger is scheduled far enough in the
    /// future for Quartz not to fire it on its own: the test drives execution explicitly.
    /// </summary>
    private async Task<RetryScenario> CreateScenarioAsync(
        string identifier,
        int failuresBeforeSuccess,
        Exception exception,
        Action<QuartzJobOptions>? configureOptions = null,
        Func<TriggerKey, IJobDetail, ITrigger>? triggerFactory = null)
    {
        var scheduler = await WorkflowServer.GetSchedulerAsync();

        var starter = new FailingWorkflowStarter(Scope.ServiceProvider.GetRequiredService<IWorkflowStarter>())
        {
            FailuresBeforeSuccess = failuresBeforeSuccess,
            ExceptionToThrow = exception,
            SuccessResponse = new() { WorkflowInstanceId = $"{identifier}-instance" }
        };

        var jobDetail = JobBuilder.Create<RunWorkflowJob>()
            .WithIdentity($"{identifier}-job", "test-group")
            .StoreDurably()
            .Build();

        var triggerIdentity = new TriggerKey($"{identifier}-trigger", "test-group");
        var trigger = triggerFactory != null
            ? triggerFactory(triggerIdentity, jobDetail)
            : TriggerBuilder.Create()
                .WithIdentity(triggerIdentity)
                .ForJob(jobDetail)
                .UsingJobData(DefinitionVersionIdKey, DefinitionVersionId)
                .StartAt(DateTimeOffset.UtcNow.AddHours(1))
                .Build();

        await scheduler.ScheduleJob(jobDetail, trigger);

        var job = new RunWorkflowJob(
            Scope.ServiceProvider.GetRequiredService<ITenantAccessor>(),
            Scope.ServiceProvider.GetRequiredService<ITenantFinder>(),
            starter,
            CreateRetryScheduler(configureOptions),
            Scope.ServiceProvider.GetRequiredService<ILogger<RunWorkflowJob>>());

        return new(job, starter, new(scheduler, jobDetail, trigger), scheduler, trigger);
    }

    /// <summary>
    /// Creates a retry scheduler backed by the application's services, but with test-specific retry options. Delays are
    /// long enough that a scheduled retry never fires by itself during the test.
    /// </summary>
    private IQuartzJobRetryScheduler CreateRetryScheduler(Action<QuartzJobOptions>? configureOptions)
    {
        var options = new QuartzJobOptions
        {
            InitialRetryDelay = TimeSpan.FromSeconds(30),
            MaxRetryDelay = TimeSpan.FromMinutes(5),
            UseJitter = false
        };

        configureOptions?.Invoke(options);

        return new QuartzJobRetryScheduler(
            Scope.ServiceProvider.GetRequiredService<ISystemClock>(),
            Microsoft.Extensions.Options.Options.Create(options),
            Scope.ServiceProvider.GetRequiredService<IQuartzRetryDelayCalculator>(),
            Scope.ServiceProvider.GetRequiredService<ITransientExceptionDetector>(),
            Scope.ServiceProvider.GetRequiredService<ILogger<QuartzJobRetryScheduler>>());
    }

    private record RetryScenario(RunWorkflowJob Job, FailingWorkflowStarter Starter, TestJobExecutionContext Context, QuartzScheduler Scheduler, ITrigger OriginalTrigger)
    {
        /// <summary>
        /// Executes the job and then points the execution context at the derived retry trigger when one was scheduled,
        /// the way Quartz would when it fires that retry.
        /// </summary>
        public async Task ExecuteAsync()
        {
            await Job.Execute(Context);
            var retryTrigger = await GetRetryTriggerAsync();

            if (retryTrigger != null)
                Context.Trigger = retryTrigger;
        }

        public Task<ITrigger?> GetOriginalTriggerAsync() => Scheduler.GetTrigger(OriginalTrigger.Key);

        public Task<ITrigger?> GetRetryTriggerAsync() => Scheduler.GetTrigger(QuartzTriggerKeys.GetRetryTriggerKey(OriginalTrigger.Key));

        /// <summary>
        /// Gets the retry attempt persisted on the derived retry trigger, or 0 when no retry was scheduled.
        /// </summary>
        public async Task<int> GetRetryAttemptAsync()
        {
            var trigger = await GetRetryTriggerAsync();

            if (trigger == null || !trigger.JobDataMap.TryGetString(QuartzJobDataKeys.RetryAttempt, out var attempt) || attempt == null)
                return 0;

            return int.Parse(attempt);
        }
    }
}

/// <summary>
/// Test implementation of IJobExecutionContext for component testing.
/// </summary>
internal class TestJobExecutionContext(QuartzScheduler scheduler, IJobDetail jobDetail, ITrigger trigger) : IJobExecutionContext
{
    public QuartzScheduler Scheduler => scheduler;

    /// <summary>
    /// The trigger that fired this execution. Settable so that a test can replay an execution with the trigger a
    /// previous retry produced.
    /// </summary>
    public ITrigger Trigger { get; set; } = trigger;

    public IJobDetail JobDetail => jobDetail;
    public IJob JobInstance => null!;
    public bool Recovering => false;
    public TriggerKey RecoveringTriggerKey => Trigger.Key;
    public int RefireCount => 0;

    public JobDataMap MergedJobDataMap
    {
        get
        {
            var map = new JobDataMap();
            map.PutAll(jobDetail.JobDataMap);
            map.PutAll(Trigger.JobDataMap);
            return map;
        }
    }

    public ICalendar? Calendar => null;
    public DateTimeOffset FireTimeUtc => DateTimeOffset.UtcNow;
    public DateTimeOffset? ScheduledFireTimeUtc => DateTimeOffset.UtcNow;
    public DateTimeOffset? PreviousFireTimeUtc => null;
    public DateTimeOffset? NextFireTimeUtc => DateTimeOffset.UtcNow.AddSeconds(10);
    public TimeSpan JobRunTime => TimeSpan.Zero;
    public object? Result { get; set; }
    public CancellationToken CancellationToken => CancellationToken.None;
    public string FireInstanceId => Guid.NewGuid().ToString();

    public void Put(object key, object objectValue) { }
    public object? Get(object key) => null;
}
