using System.Globalization;
using Elsa.Common;
using Elsa.Resilience;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz;
using Elsa.Scheduling.Quartz.Models;
using Elsa.Scheduling.Quartz.Options;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Scheduling.Quartz.UnitTests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.UnitTests.Services;

public class QuartzJobRetrySchedulerTests
{
    private static readonly DateTimeOffset Now = new(2025, 01, 02, 03, 04, 05, TimeSpan.Zero);
    private readonly Mock<ISystemClock> _clock = new();
    private readonly Mock<ITransientExceptionDetector> _transientDetector = new();
    private readonly QuartzJobOptions _options = QuartzJobTestHelper.CreateQuartzJobOptions();
    private readonly Exception _exception = new TimeoutException("Transient");

    public QuartzJobRetrySchedulerTests()
    {
        _clock.SetupGet(x => x.UtcNow).Returns(Now);
        _transientDetector.SetupIsTransient(true);
    }

    [Fact]
    public async Task ScheduleRetryAsync_FirstRetry_StartsAtNowPlusTheInitialDelay()
    {
        _options.InitialRetryDelay = TimeSpan.FromSeconds(10);
        var (context, scheduler) = CreateContext();
        var capturedTrigger = CaptureTrigger(scheduler);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        Assert.Equal(Now.Add(TimeSpan.FromSeconds(10)), capturedTrigger()!.StartTimeUtc);
    }

    [Fact]
    public async Task ScheduleRetryAsync_SecondRetry_AppliesExponentialBackoff()
    {
        _options.InitialRetryDelay = TimeSpan.FromSeconds(10);
        var (context, scheduler) = CreateContext(retryAttempt: "1");
        var capturedTrigger = CaptureRescheduledTrigger(scheduler);

        await ScheduleRetryAsync(context);

        Assert.Equal(Now.Add(TimeSpan.FromSeconds(20)), capturedTrigger()!.StartTimeUtc);
    }

    [Fact]
    public async Task ScheduleRetryAsync_PersistsTheIncrementedAttemptAndPreservesTriggerData()
    {
        var (context, scheduler) = CreateContext(retryAttempt: "1");
        var capturedTrigger = CaptureRescheduledTrigger(scheduler);

        await ScheduleRetryAsync(context);

        var jobDataMap = capturedTrigger()!.JobDataMap;
        Assert.Equal("2", jobDataMap[QuartzJobDataKeys.RetryAttempt]);
        Assert.Equal("workflow-def-123", jobDataMap["DefinitionVersionId"]);
    }

    [Fact]
    public async Task ScheduleRetryAsync_UsesADerivedTriggerKeySoTheOriginalScheduleIsLeftInPlace()
    {
        var (context, scheduler) = CreateContext();
        var capturedTrigger = CaptureTrigger(scheduler);

        await ScheduleRetryAsync(context);

        Assert.Equal(QuartzTriggerKeys.GetRetryTriggerKey(context.Trigger.Key), capturedTrigger()!.Key);
        Assert.Equal(context.JobDetail.Key, capturedTrigger()!.JobKey);
        scheduler.Verify(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleRetryAsync_OriginalNameEndingWithRetry_UsesAnUnambiguousDerivedKey()
    {
        var (context, scheduler) = CreateContext(triggerName: "task-retry");
        var capturedTrigger = CaptureTrigger(scheduler);

        await ScheduleRetryAsync(context);

        var expectedRetryKey = QuartzTriggerKeys.GetRetryTriggerKey(context.Trigger.Key);
        Assert.Equal(expectedRetryKey, capturedTrigger()!.Key);
        scheduler.Verify(s => s.UnscheduleJob(expectedRetryKey, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleRetryAsync_WhenTheFiringTriggerIsAlreadyARetry_ReusesTheRetryKey()
    {
        var originalTriggerKey = new TriggerKey("test-trigger", "Default");
        var retryTriggerKey = QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey);
        var (context, scheduler) = CreateContext(retryAttempt: "1", triggerName: retryTriggerKey.Name, originalTriggerKey: originalTriggerKey);
        var capturedTrigger = CaptureRescheduledTrigger(scheduler);

        await ScheduleRetryAsync(context);

        Assert.Equal(retryTriggerKey, capturedTrigger()!.Key);
        Assert.Equal(QuartzTriggerKeys.RetryGroup, capturedTrigger()!.Key.Group);
        scheduler.Verify(s => s.RescheduleJob(retryTriggerKey, It.Is<ITrigger>(t => t.Key.Equals(retryTriggerKey)), It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleRetryAsync_RetriesDisabled_ReturnsFalseAndDoesNotReschedule()
    {
        _options.RetryEnabled = false;
        var (context, scheduler) = CreateContext();

        var scheduled = await ScheduleRetryAsync(context);

        Assert.False(scheduled);
        VerifyNotRescheduled(scheduler);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("1", true)]
    [InlineData("2", true)]
    [InlineData("3", false)]
    [InlineData("4", false)]
    public async Task ScheduleRetryAsync_StopsOnceTheMaximumNumberOfAttemptsIsReached(string? retryAttempt, bool expectedScheduled)
    {
        _options.MaxRetryAttempts = 3;
        var (context, scheduler) = CreateContext(retryAttempt);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.Equal(expectedScheduled, scheduled);

        if (!expectedScheduled)
            VerifyNotRescheduled(scheduler);
    }

    [Fact]
    public async Task ScheduleRetryAsync_ZeroMaximumAttempts_ReturnsFalse()
    {
        _options.MaxRetryAttempts = 0;
        var (context, scheduler) = CreateContext();

        var scheduled = await ScheduleRetryAsync(context);

        Assert.False(scheduled);
        VerifyNotRescheduled(scheduler);
    }

    [Fact]
    public async Task ScheduleRetryAsync_NegativeMaximumAttempts_ReturnsFalse()
    {
        _options.MaxRetryAttempts = -1;
        var (context, scheduler) = CreateContext();

        var scheduled = await ScheduleRetryAsync(context);

        Assert.False(scheduled);
        VerifyNotRescheduled(scheduler);
    }

    [Fact]
    public async Task ScheduleRetryAsync_NegativePersistedAttemptCount_IsClampedToZero()
    {
        _options.MaxRetryAttempts = 3;
        _options.InitialRetryDelay = TimeSpan.FromSeconds(10);
        var (context, scheduler) = CreateContext(retryAttempt: "-1");
        var capturedTrigger = CaptureRescheduledTrigger(scheduler);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        Assert.Equal("1", capturedTrigger()!.JobDataMap[QuartzJobDataKeys.RetryAttempt]);
    }

    [Fact]
    public async Task ScheduleRetryAsync_PersistedAttemptCountAtIntMaxValue_ReturnsFalseWithoutOverflow()
    {
        _options.MaxRetryAttempts = 3;
        var (context, scheduler) = CreateContext(retryAttempt: int.MaxValue.ToString(CultureInfo.InvariantCulture));

        var scheduled = await ScheduleRetryAsync(context);

        Assert.False(scheduled);
        VerifyNotRescheduled(scheduler);
    }

    [Fact]
    public async Task ScheduleRetryAsync_PersistedAttemptCountAtLongMaxValue_ReturnsFalseWithoutOverflow()
    {
        _options.MaxRetryAttempts = 3;
        var (context, scheduler) = CreateContext(retryAttempt: long.MaxValue);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.False(scheduled);
        VerifyNotRescheduled(scheduler);
    }

    [Fact]
    public async Task ScheduleRetryAsync_PersistedAttemptCountAsStringBeyondIntRange_ReturnsFalseWithoutOverflow()
    {
        _options.MaxRetryAttempts = 3;
        var (context, scheduler) = CreateContext(retryAttempt: "9999999999");

        var scheduled = await ScheduleRetryAsync(context);

        Assert.False(scheduled);
        VerifyNotRescheduled(scheduler);
    }

    [Fact]
    public async Task ScheduleRetryAsync_OriginalTrigger_DoesNotRemoveAnExistingPendingRetryTrigger()
    {
        var (context, scheduler) = CreateContext();
        var retryKey = QuartzTriggerKeys.GetRetryTriggerKey(context.Trigger.Key);
        scheduler
            .Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ObjectAlreadyExistsException("retry already exists"));

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        scheduler.Verify(s => s.UnscheduleJob(retryKey, It.IsAny<CancellationToken>()), Times.Never);
        scheduler.Verify(s => s.ScheduleJob(It.Is<ITrigger>(t => t.Key.Equals(retryKey)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleRetryAsync_FiringRetry_ReplacesItsOwnPendingRetryTrigger()
    {
        var originalTriggerKey = new TriggerKey("test-trigger", "Default");
        var retryKey = QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey);
        var (context, scheduler) = CreateContext(retryAttempt: "1", triggerName: retryKey.Name, originalTriggerKey: originalTriggerKey);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        scheduler.Verify(s => s.UnscheduleJob(retryKey, It.IsAny<CancellationToken>()), Times.Never);
        scheduler.Verify(s => s.RescheduleJob(retryKey, It.Is<ITrigger>(t => t.Key.Equals(retryKey)), It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleRetryAsync_FiringRetryMissingFromStore_DoesNotRecreateTheRetry()
    {
        var originalTriggerKey = new TriggerKey("test-trigger", "Default");
        var retryKey = QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey);
        var (context, scheduler) = CreateContext(retryAttempt: "1", triggerName: retryKey.Name, originalTriggerKey: originalTriggerKey);
        scheduler
            .Setup(s => s.RescheduleJob(retryKey, It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DateTimeOffset?)null);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        scheduler.Verify(s => s.RescheduleJob(retryKey, It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleRetryAsync_UsesTheOriginalScheduleCoordinator()
    {
        var originalTriggerKey = new TriggerKey("test-trigger", "Default");
        var retryKey = QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey);
        var (context, _) = CreateContext(retryAttempt: "1", triggerName: retryKey.Name, originalTriggerKey: originalTriggerKey);
        var coordinator = new Mock<IQuartzScheduleCoordinator>();
        coordinator
            .Setup(c => c.ExecuteAsync(It.IsAny<TriggerKey>(), It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<TriggerKey, Func<CancellationToken, Task>, CancellationToken>((_, action, token) => action(token));
        var sut = CreateSut(coordinator.Object);

        await sut.ScheduleRetryAsync(context, _exception);

        coordinator.Verify(c => c.ExecuteAsync(originalTriggerKey, It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleRetryAsync_WhenRetryTriggerIsCreatedConcurrently_TreatsDuplicateAsAlreadyScheduled()
    {
        var (context, scheduler) = CreateContext();
        scheduler
            .Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ObjectAlreadyExistsException("retry already exists"));

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
    }

    [Fact]
    public async Task ScheduleRetryAsync_WhenSqlStoreWrapsDuplicateRetryTrigger_TreatsItAsAlreadyScheduled()
    {
        var (context, scheduler) = CreateContext();
        scheduler
            .Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new JobPersistenceException("retry already exists", new ObjectAlreadyExistsException("duplicate")));

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
    }

    [Fact]
    public async Task ScheduleRetryAsync_NonFinalRecurringOriginalRemovedAfterScheduling_RemovesRetryAsHandled()
    {
        var (context, scheduler) = CreateContext(nextFireTimeUtc: Now.AddMinutes(1));
        var cronTrigger = CreateCronTrigger();
        Mock.Get(context).Setup(c => c.Trigger).Returns(cronTrigger);
        scheduler.Setup(s => s.GetTrigger(cronTrigger.Key, It.IsAny<CancellationToken>())).ReturnsAsync((ITrigger?)null);
        scheduler.Setup(s => s.GetTrigger(QuartzTriggerKeys.GetRetryTriggerKey(cronTrigger.Key), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TriggerBuilder.Create().WithIdentity(QuartzTriggerKeys.GetRetryTriggerKey(cronTrigger.Key)).Build());
        var capturedTrigger = CaptureTrigger(scheduler);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        Assert.NotNull(capturedTrigger());
        scheduler.Verify(s => s.UnscheduleJob(capturedTrigger()!.Key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleRetryAsync_FinalFiniteRecurringOccurrence_PreservesRetryAttemptsAfterOriginalDisappears()
    {
        _options.MaxRetryAttempts = 2;
        var originalTriggerKey = new TriggerKey("test-trigger", "Default");
        var (context, scheduler) = CreateContext(scheduleGeneration: "final-generation");
        var finalTrigger = CreateFiniteRecurringTrigger(originalTriggerKey, "final-generation");
        Mock.Get(context).Setup(c => c.Trigger).Returns(finalTrigger);
        var capturedRetry = CaptureTrigger(scheduler);

        Assert.True(await ScheduleRetryAsync(context));
        Assert.Equal(bool.FalseString, capturedRetry()!.JobDataMap[QuartzJobDataKeys.RetryOriginalHasNextFireTime]);

        var retryContext = CreateContext(
            retryAttempt: "1",
            triggerName: QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey).Name,
            originalTriggerKey: originalTriggerKey,
            scheduleGeneration: "final-generation",
            retryOriginalIsRecurring: true,
            retryOriginalHasNextFireTime: false);
        Mock.Get(retryContext.Context).Setup(c => c.Scheduler).Returns(scheduler.Object);
        var capturedNextRetry = CaptureRescheduledTrigger(scheduler);

        Assert.True(await ScheduleRetryAsync(retryContext.Context));
        Assert.Equal("2", capturedNextRetry()!.JobDataMap[QuartzJobDataKeys.RetryAttempt]);
        scheduler.Verify(s => s.RescheduleJob(QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.UnscheduleJob(QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleRetryAsync_StaleFiringRetry_DoesNotTouchNewerStableRetry()
    {
        var originalTriggerKey = new TriggerKey("test-trigger", "Default");
        var retryKey = QuartzTriggerKeys.GetRetryTriggerKey(originalTriggerKey);
        var (context, scheduler) = CreateContext(
            retryAttempt: "1",
            triggerName: retryKey.Name,
            originalTriggerKey: originalTriggerKey,
            scheduleGeneration: "old-generation");
        var newerRetryTrigger = TriggerBuilder.Create()
            .WithIdentity(retryKey)
            .UsingJobData(QuartzJobDataKeys.RetryScheduleGeneration, "new-generation")
            .Build();
        scheduler.Setup(s => s.GetTrigger(retryKey, It.IsAny<CancellationToken>())).ReturnsAsync(newerRetryTrigger);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        scheduler.Verify(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        scheduler.Verify(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleRetryAsync_OneShotOriginalMissingAfterScheduling_KeepsRetry()
    {
        var (context, scheduler) = CreateContext();
        var capturedTrigger = CaptureTrigger(scheduler);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        Assert.NotNull(capturedTrigger());
        scheduler.Verify(s => s.GetTrigger(context.Trigger.Key, It.IsAny<CancellationToken>()), Times.Exactly(2));
        scheduler.Verify(s => s.UnscheduleJob(capturedTrigger()!.Key, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ScheduleRetryAsync_OneShotOriginalReplaced_DiscardsStaleRetry()
    {
        var (context, scheduler) = CreateContext(scheduleGeneration: "old-generation");
        var replacementTrigger = TriggerBuilder.Create()
            .WithIdentity(context.Trigger.Key)
            .UsingJobData(QuartzJobDataKeys.RetryScheduleGeneration, "new-generation")
            .Build();
        var staleRetryTrigger = TriggerBuilder.Create()
            .WithIdentity(QuartzTriggerKeys.GetRetryTriggerKey(context.Trigger.Key))
            .UsingJobData(QuartzJobDataKeys.RetryScheduleGeneration, "old-generation")
            .Build();
        scheduler.Setup(s => s.GetTrigger(context.Trigger.Key, It.IsAny<CancellationToken>())).ReturnsAsync(replacementTrigger);
        scheduler.Setup(s => s.GetTrigger(QuartzTriggerKeys.GetRetryTriggerKey(context.Trigger.Key), It.IsAny<CancellationToken>())).ReturnsAsync(staleRetryTrigger);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        scheduler.Verify(s => s.GetTrigger(context.Trigger.Key, It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        scheduler.Verify(s => s.UnscheduleJob(QuartzTriggerKeys.GetRetryTriggerKey(context.Trigger.Key), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleRetryAsync_RescheduledRecurringOriginal_HasDifferentGeneration_RemovesStaleRetry()
    {
        var (context, scheduler) = CreateContext();
        var originalTrigger = CreateCronTrigger("old-generation");
        var rescheduledTrigger = CreateCronTrigger("new-generation");
        var staleRetryTrigger = TriggerBuilder.Create()
            .WithIdentity(QuartzTriggerKeys.GetRetryTriggerKey(originalTrigger.Key))
            .UsingJobData(QuartzJobDataKeys.RetryScheduleGeneration, "old-generation")
            .Build();
        Mock.Get(context).Setup(c => c.Trigger).Returns(originalTrigger);
        scheduler.Setup(s => s.GetTrigger(originalTrigger.Key, It.IsAny<CancellationToken>())).ReturnsAsync(rescheduledTrigger);
        scheduler.Setup(s => s.GetTrigger(QuartzTriggerKeys.GetRetryTriggerKey(originalTrigger.Key), It.IsAny<CancellationToken>())).ReturnsAsync(staleRetryTrigger);
        var capturedTrigger = CaptureTrigger(scheduler);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        Assert.NotNull(capturedTrigger());
        Assert.Equal("old-generation", capturedTrigger()!.JobDataMap[QuartzJobDataKeys.RetryScheduleGeneration]);
        Assert.Equal(QuartzTriggerKeys.GetRetryTriggerKey(originalTrigger.Key), capturedTrigger()!.Key);
        scheduler.Verify(s => s.UnscheduleJob(capturedTrigger()!.Key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScheduleRetryAsync_DelayIsTimeSpanMaxValue_ClampsStartTimeToDateTimeOffsetMaxValueWithoutThrowing()
    {
        _options.DelayGenerator = _ => TimeSpan.MaxValue;
        var (context, scheduler) = CreateContext();
        var capturedTrigger = CaptureTrigger(scheduler);

        var scheduled = await ScheduleRetryAsync(context);

        Assert.True(scheduled);
        Assert.Equal(DateTimeOffset.MaxValue, capturedTrigger()!.StartTimeUtc);
    }

    [Fact]
    public async Task ScheduleRetryAsync_DelayGenerator_OverridesTheComputedDelay()
    {
        _options.InitialRetryDelay = TimeSpan.FromSeconds(10);
        _options.MaxRetryAttempts = 7;
        QuartzJobRetryContext? capturedRetryContext = null;
        _options.DelayGenerator = retryContext =>
        {
            capturedRetryContext = retryContext;
            return TimeSpan.FromSeconds(42);
        };
        var (context, scheduler) = CreateContext(retryAttempt: "1");
        var capturedTrigger = CaptureRescheduledTrigger(scheduler);

        await ScheduleRetryAsync(context);

        Assert.Equal(Now.Add(TimeSpan.FromSeconds(42)), capturedTrigger()!.StartTimeUtc);
        Assert.NotNull(capturedRetryContext);
        Assert.Equal(2, capturedRetryContext!.AttemptNumber);
        Assert.Equal(7, capturedRetryContext.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromSeconds(20), capturedRetryContext.ComputedDelay);
        Assert.Same(_exception, capturedRetryContext.Exception);
        Assert.Equal(context.JobDetail.Key, capturedRetryContext.JobKey);
    }

    [Fact]
    public async Task ScheduleRetryAsync_DelayGeneratorReturningNull_FallsBackToTheComputedDelay()
    {
        _options.InitialRetryDelay = TimeSpan.FromSeconds(10);
        _options.DelayGenerator = _ => null;
        var (context, scheduler) = CreateContext();
        var capturedTrigger = CaptureTrigger(scheduler);

        await ScheduleRetryAsync(context);

        Assert.Equal(Now.Add(TimeSpan.FromSeconds(10)), capturedTrigger()!.StartTimeUtc);
    }

    [Fact]
    public async Task ScheduleRetryAsync_DelayGeneratorReturningANegativeDelay_SchedulesImmediately()
    {
        _options.DelayGenerator = _ => TimeSpan.FromSeconds(-10);
        var (context, scheduler) = CreateContext();
        var capturedTrigger = CaptureTrigger(scheduler);

        await ScheduleRetryAsync(context);

        Assert.Equal(Now, capturedTrigger()!.StartTimeUtc);
    }

    [Fact]
    public async Task ScheduleRetryAsync_ObsoleteRetryDelay_ConfiguresTheInitialDelay()
    {
#pragma warning disable CS0618 // The obsolete property must keep working for existing configuration.
        _options.TransientExceptionRetryDelay = TimeSpan.FromSeconds(10);
        Assert.Equal(TimeSpan.FromSeconds(10), _options.InitialRetryDelay);

        _options.InitialRetryDelay = TimeSpan.FromSeconds(3);
        Assert.Equal(TimeSpan.FromSeconds(3), _options.TransientExceptionRetryDelay);
#pragma warning restore CS0618

        var (context, scheduler) = CreateContext();
        var capturedTrigger = CaptureTrigger(scheduler);

        await ScheduleRetryAsync(context);

        Assert.Equal(Now.Add(TimeSpan.FromSeconds(3)), capturedTrigger()!.StartTimeUtc);
    }

    [Fact]
    public void IsRetryable_WithoutOverride_DefersToTheTransientExceptionDetector()
    {
        _transientDetector.SetupIsTransient(false);

        Assert.False(CreateSut().IsRetryable(_exception));

        _transientDetector.SetupIsTransient(true);

        Assert.True(CreateSut().IsRetryable(_exception));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void IsRetryable_WithOverride_IgnoresTheTransientExceptionDetector(bool overrideResult, bool detectorResult)
    {
        _transientDetector.SetupIsTransient(detectorResult);
        _options.IsRetryable = _ => overrideResult;

        Assert.Equal(overrideResult, CreateSut().IsRetryable(_exception));
        _transientDetector.Verify(x => x.IsTransient(It.IsAny<Exception>()), Times.Never);
    }

    private Task<bool> ScheduleRetryAsync(IJobExecutionContext context) => CreateSut().ScheduleRetryAsync(context, _exception);

    private QuartzJobRetryScheduler CreateSut(IQuartzScheduleCoordinator? scheduleCoordinator = null) =>
        new(_clock.Object, _options.AsOptions(), new QuartzRetryDelayCalculator(), _transientDetector.Object, NullLogger<QuartzJobRetryScheduler>.Instance, scheduleCoordinator);

    private static (IJobExecutionContext Context, Mock<QuartzScheduler> Scheduler) CreateContext(
        object? retryAttempt = null,
        string? triggerName = null,
        TriggerKey? originalTriggerKey = null,
        string? scheduleGeneration = null,
        bool? retryOriginalIsRecurring = null,
        bool? retryOriginalHasNextFireTime = null,
        DateTimeOffset? nextFireTimeUtc = null)
    {
        var triggerData = new Dictionary<string, object>
        {
            ["DefinitionVersionId"] = "workflow-def-123"
        };

        if (retryAttempt != null)
        {
            triggerData[QuartzJobDataKeys.RetryAttempt] = retryAttempt;
            triggerData[QuartzJobDataKeys.RetryTrigger] = bool.TrueString;
            var originalKey = originalTriggerKey ?? new TriggerKey("test-trigger", "Default");
            triggerData[QuartzJobDataKeys.RetryOriginalTriggerName] = originalKey.Name;
            triggerData[QuartzJobDataKeys.RetryOriginalTriggerGroup] = originalKey.Group;

            if (retryOriginalIsRecurring.HasValue)
                triggerData[QuartzJobDataKeys.RetryOriginalIsRecurring] = retryOriginalIsRecurring.Value.ToString();

            if (retryOriginalHasNextFireTime.HasValue)
                triggerData[QuartzJobDataKeys.RetryOriginalHasNextFireTime] = retryOriginalHasNextFireTime.Value.ToString();
        }

        if (scheduleGeneration != null)
            triggerData[QuartzJobDataKeys.RetryScheduleGeneration] = scheduleGeneration;

        var result = QuartzJobTestHelper.CreateJobExecutionContext(new Dictionary<string, object>(), triggerData: triggerData, triggerName: triggerName);
        Mock.Get(result.Context).Setup(c => c.NextFireTimeUtc).Returns(nextFireTimeUtc);
        return result;
    }

    private static Func<ITrigger?> CaptureTrigger(Mock<QuartzScheduler> scheduler)
    {
        ITrigger? capturedTrigger = null;
        scheduler
            .Setup(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .Callback<ITrigger, CancellationToken>((t, _) => capturedTrigger = t)
            .ReturnsAsync(Now);

        return () => capturedTrigger;
    }

    private static Func<ITrigger?> CaptureRescheduledTrigger(Mock<QuartzScheduler> scheduler)
    {
        ITrigger? capturedTrigger = null;
        scheduler
            .Setup(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .Callback<TriggerKey, ITrigger, CancellationToken>((_, trigger, _) => capturedTrigger = trigger)
            .ReturnsAsync(Now);

        return () => capturedTrigger;
    }

    private static ITrigger CreateCronTrigger(string? generation = null)
    {
        var builder = TriggerBuilder.Create()
            .WithIdentity("test-trigger")
            .ForJob(new JobKey("test-job"))
            .WithCronSchedule("0 0 12 1 1 ? 2099");

        if (generation != null)
            builder.UsingJobData(QuartzJobDataKeys.RetryScheduleGeneration, generation);

        return builder.Build();
    }

    private static ITrigger CreateFiniteRecurringTrigger(TriggerKey triggerKey, string generation)
    {
        return TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(new JobKey("test-job"))
            .UsingJobData(QuartzJobDataKeys.RetryScheduleGeneration, generation)
            .StartNow()
            .WithSimpleSchedule(schedule => schedule.WithInterval(TimeSpan.FromMinutes(1)).WithRepeatCount(1))
            .Build();
    }

    private static void VerifyNotRescheduled(Mock<QuartzScheduler> scheduler)
    {
        scheduler.Verify(s => s.ScheduleJob(It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
        scheduler.Verify(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Never);
    }

}
