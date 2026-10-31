using System.Collections.Concurrent;
using Elsa.Common.Multitenancy;
using Elsa.Common.RecurringTasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Common.UnitTests.Multitenancy;

public class TenantRecurringTaskSchedulerTests
{
    private readonly ControllableSchedule _schedule = new();
    private readonly DefaultTenantAccessor _tenantAccessor = new();

    [Fact]
    public async Task RunCycle_RunsEachTenantOnceInItsOwnContext()
    {
        var task = new HostScopedTask(_tenantAccessor);
        await using var scheduler = CreateScheduler(task.GetType());
        await RegisterTenantsAsync(scheduler, task, 3);

        await _schedule.TickAsync();
        await scheduler.WhenIdleAsync();

        Assert.Equal(["tenant-0", "tenant-1", "tenant-2"], task.ObservedTenantIds.Order());
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(0, 1)]
    public async Task RunCycle_RespectsMaxDegreeOfParallelism(int maxDegreeOfParallelism, int expectedPeak)
    {
        var task = new GatedTask();
        await using var scheduler = CreateScheduler(task.GetType(), maxDegreeOfParallelism);
        await RegisterTenantsAsync(scheduler, task, 6);

        await _schedule.TickAsync();
        await task.WaitForStartedAsync(expectedPeak);

        task.Release(6);
        await scheduler.WhenIdleAsync();

        Assert.Equal(6, task.StartedCount);
        Assert.Equal(expectedPeak, task.PeakConcurrency);
    }

    [Fact]
    public async Task RunCycle_AfterTenantUnregistered_SkipsThatTenant()
    {
        var task = new RecordingTask(_tenantAccessor);
        await using var scheduler = CreateScheduler(task.GetType());
        var registrations = await RegisterTenantsAsync(scheduler, task, 3);
        await RunCycleAsync(scheduler, task.GetType());

        await scheduler.UnregisterAsync(registrations[1]);
        await RunCycleAsync(scheduler, task.GetType());
        await scheduler.UnregisterAsync(registrations[0]);
        await scheduler.UnregisterAsync(registrations[2]);
        await RunCycleAsync(scheduler, task.GetType());

        Assert.Equal(["tenant-0", "tenant-0", "tenant-1", "tenant-2", "tenant-2"], task.ObservedTenantIds.Order());

        await RegisterAsync(scheduler, task, "tenant-3");
        Assert.Equal(2, _schedule.CreatedTimerCount);
    }

    [Fact]
    public async Task RunCycle_AfterTenantRegisteredBetweenCycles_IncludesThatTenant()
    {
        var task = new RecordingTask(_tenantAccessor);
        await using var scheduler = CreateScheduler(task.GetType());
        await RegisterAsync(scheduler, task, "tenant-0");
        await RunCycleAsync(scheduler, task.GetType());

        await RegisterAsync(scheduler, task, "tenant-1");
        await RunCycleAsync(scheduler, task.GetType());

        Assert.Equal(["tenant-0", "tenant-0", "tenant-1"], task.ObservedTenantIds.Order());
        Assert.Equal(1, _schedule.CreatedTimerCount);
    }

    [Fact]
    public async Task RunCycle_WhenTenantIsCancelledMidRun_CancelsOnlyThatTenant()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var wasCancelled = false;
        var task = new SpecialTenantTask(_tenantAccessor, "tenant-1", async cancellationToken =>
        {
            started.SetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                wasCancelled = true;
                throw;
            }
        });
        var logger = new RecordingLogger();
        await using var scheduler = CreateScheduler(task.GetType(), logger: logger);
        using var cancelledTenantSource = new CancellationTokenSource();
        await RegisterAsync(scheduler, task, "tenant-0");
        await RegisterAsync(scheduler, task, "tenant-1", cancelledTenantSource.Token);

        await _schedule.TickAsync();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await cancelledTenantSource.CancelAsync();
        await scheduler.WhenIdleAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(wasCancelled);
        Assert.Contains("tenant-0", task.CompletedTenantIds);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("tenant-1", entry.Message);
    }

    [Fact]
    public async Task RunCycle_WithAlreadyCancelledTenant_SkipsThatTenant()
    {
        var task = new RecordingTask(_tenantAccessor);
        await using var scheduler = CreateScheduler(task.GetType());
        using var cancelledTenantSource = new CancellationTokenSource();
        await cancelledTenantSource.CancelAsync();
        await RegisterAsync(scheduler, task, "tenant-0");
        await RegisterAsync(scheduler, task, "tenant-1", cancelledTenantSource.Token);

        await RunCycleAsync(scheduler, task.GetType());

        Assert.Equal(["tenant-0"], task.ObservedTenantIds);
    }

    [Fact]
    public async Task RunCycle_WhenOneTenantFails_StillRunsTheOthers()
    {
        var task = new SpecialTenantTask(_tenantAccessor, "tenant-1", _ => throw new InvalidOperationException("boom"));
        var logger = new RecordingLogger();
        await using var scheduler = CreateScheduler(task.GetType(), logger: logger);
        await RegisterTenantsAsync(scheduler, task, 3);

        await RunCycleAsync(scheduler, task.GetType());

        Assert.Equal(["tenant-0", "tenant-2"], task.CompletedTenantIds.Order());
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("tenant-1", entry.Message);
    }

    [Theory]
    [InlineData(true, "")]
    [InlineData(false, "tenant-b")]
    public async Task RunCycle_WithRunOnceForAllTenantsTask_RunsOnceUnderTheDefaultOrLowestActiveTenant(bool includeDefaultTenant, string expectedTenantId)
    {
        var task = new DerivedRunOnceTask(_tenantAccessor);
        await using var scheduler = CreateScheduler(task.GetType());
        using var cancelledTenantSource = new CancellationTokenSource();
        await cancelledTenantSource.CancelAsync();
        await RegisterAsync(scheduler, task, "tenant-b");
        await RegisterAsync(scheduler, task, includeDefaultTenant ? Tenant.DefaultTenantId : "tenant-c");
        await RegisterAsync(scheduler, task, "tenant-a", cancelledTenantSource.Token);

        await RunCycleAsync(scheduler, task.GetType());

        Assert.Equal([expectedTenantId], task.ObservedTenantIds);
    }

    [Fact]
    public async Task RunCycle_WithNullTenantId_RunsUnderTheDefaultTenantId()
    {
        var task = new RecordingTask(_tenantAccessor);
        await using var scheduler = CreateScheduler(task.GetType());
        await RegisterAsync(scheduler, task, null!);

        await RunCycleAsync(scheduler, task.GetType());

        Assert.Equal([""], task.ObservedTenantIds);
    }

    [Fact]
    public async Task Tick_WhileOneTenantIsStillRunning_KeepsRunningTheOtherTenantsWithoutStartingTheSlowOneTwice()
    {
        var task = new BlockingForOneTenantTask(_tenantAccessor, "tenant-0");
        await using var scheduler = CreateScheduler(task.GetType());
        var registrations = await RegisterTenantsAsync(scheduler, task, 2);

        await _schedule.TickAsync();
        await task.WaitForFastRunsAsync(1);
        await task.WaitForBlockedStartsAsync(1);
        await registrations[1].Run!.WaitAsync(TimeSpan.FromSeconds(5));
        await _schedule.TickAsync();
        await task.WaitForFastRunsAsync(1);

        Assert.Equal(1, task.BlockedStartCount);

        task.ReleaseBlockedRun();
        await scheduler.WhenIdleAsync();
        await _schedule.TickAsync();
        await task.WaitForBlockedStartsAsync(1);

        Assert.Equal(2, task.BlockedStartCount);
        task.ReleaseBlockedRun();
    }

    [Fact]
    public async Task Tick_WithRunOnceForAllTenantsTaskStillRunning_SkipsTheRun()
    {
        var task = new GatedRunOnceTask();
        await using var scheduler = CreateScheduler(task.GetType());
        await RegisterAsync(scheduler, task, "tenant-b");

        await _schedule.TickAsync();
        await task.WaitForStartedAsync(1);
        await RegisterAsync(scheduler, task, Tenant.DefaultTenantId);
        await _schedule.TickAsync();

        task.Release(2);
        await scheduler.WhenIdleAsync();

        Assert.Equal(1, task.StartedCount);
    }

    [Fact]
    public async Task Unregister_WithCancelledTenantRunQueuedOnTheLimiter_DropsTheQueuedRun()
    {
        var task = new BlockingForOneTenantTask(_tenantAccessor, "tenant-0");
        await using var scheduler = CreateScheduler(task.GetType(), maxDegreeOfParallelism: 1);
        await RegisterAsync(scheduler, task, "tenant-0");
        await _schedule.TickAsync();
        await task.WaitForBlockedStartsAsync(1);
        using var queuedTenantSource = new CancellationTokenSource();
        var queued = await RegisterAsync(scheduler, task, "tenant-1", queuedTenantSource.Token);
        await _schedule.TickAsync();

        await queuedTenantSource.CancelAsync();
        await scheduler.UnregisterAsync(queued).WaitAsync(TimeSpan.FromSeconds(5));
        task.ReleaseBlockedRun();
        await scheduler.WhenIdleAsync();

        Assert.Equal(0, task.FastRunCount);
    }

    [Fact]
    public async Task Unregister_WithRunQueuedOnTheLimiter_DropsTheQueuedRun()
    {
        var task = new BlockingForOneTenantTask(_tenantAccessor, "tenant-0");
        await using var scheduler = CreateScheduler(task.GetType(), maxDegreeOfParallelism: 1);
        await RegisterAsync(scheduler, task, "tenant-0");
        await _schedule.TickAsync();
        await task.WaitForBlockedStartsAsync(1);
        var queued = await RegisterAsync(scheduler, task, "tenant-1");
        await _schedule.TickAsync();

        var unregister = scheduler.UnregisterAsync(queued);
        task.ReleaseBlockedRun();
        await unregister.WaitAsync(TimeSpan.FromSeconds(5));
        await scheduler.WhenIdleAsync();

        Assert.Equal(0, task.FastRunCount);
    }

    [Fact]
    public async Task RunCycle_WithTwoInstancesOfOneTaskTypeForOneTenant_RunsBoth()
    {
        var observed = new ConcurrentQueue<string>();
        var first = new NamedTask("a", observed);
        var second = new NamedTask("b", observed);
        await using var scheduler = CreateScheduler(typeof(NamedTask));
        await RegisterAsync(scheduler, first, "tenant-0");
        await RegisterAsync(scheduler, second, "tenant-0");

        await RunCycleAsync(scheduler, typeof(NamedTask));

        Assert.Equal(["a", "b"], observed.Order());
    }

    [Fact]
    public async Task Dispose_WhileARunIsInFlight_WaitsForTheRunToFinish()
    {
        var task = new BlockingForOneTenantTask(_tenantAccessor, "tenant-0");
        var scheduler = CreateScheduler(task.GetType());
        await RegisterAsync(scheduler, task, "tenant-0");
        await _schedule.TickAsync();
        await task.WaitForBlockedStartsAsync(1);

        var dispose = scheduler.DisposeAsync().AsTask();

        Assert.False(dispose.IsCompleted);
        task.ReleaseBlockedRun();
        await dispose.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Register_AfterDispose_StartsNoTimerAndNoRuns()
    {
        var task = new RecordingTask(_tenantAccessor);
        var scheduler = CreateScheduler(task.GetType());
        await scheduler.DisposeAsync();

        await RegisterAsync(scheduler, task, "tenant-0");
        await _schedule.TickAsync();
        scheduler.RunCycle(task.GetType());
        await scheduler.WhenIdleAsync();

        Assert.Equal(0, _schedule.CreatedTimerCount);
        Assert.Empty(task.ObservedTenantIds);
    }

    [Fact]
    public async Task Tick_WithTaskThrowingCancellationWithoutTenantCancellation_LogsError()
    {
        var task = new SpecialTenantTask(_tenantAccessor, "tenant-0", _ => throw new OperationCanceledException());
        var logger = new RecordingLogger();
        await using var scheduler = CreateScheduler(task.GetType(), logger: logger);
        await RegisterAsync(scheduler, task, "tenant-0");

        await RunCycleAsync(scheduler, task.GetType());

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("tenant-0", entry.Message);
    }

    [Fact]
    public async Task Unregister_WithTwoRegistrationsForOneTenant_KeepsTheOtherRegistration()
    {
        var task = new RecordingTask(_tenantAccessor);
        await using var scheduler = CreateScheduler(task.GetType());
        var oldRegistration = await RegisterAsync(scheduler, task, "tenant-0");
        await RegisterAsync(scheduler, task, "tenant-0");

        await scheduler.UnregisterAsync(oldRegistration);
        await _schedule.TickAsync();
        await scheduler.WhenIdleAsync();

        Assert.Equal(["tenant-0"], task.ObservedTenantIds);
    }

    [Fact]
    public async Task Unregister_WhileTheTenantRunIsInFlight_WaitsForTheRunToFinish()
    {
        var task = new BlockingForOneTenantTask(_tenantAccessor, "tenant-0");
        await using var scheduler = CreateScheduler(task.GetType());
        var registrations = await RegisterTenantsAsync(scheduler, task, 1);
        await _schedule.TickAsync();
        await task.WaitForBlockedStartsAsync(1);

        var unregister = scheduler.UnregisterAsync(registrations[0]);

        Assert.False(unregister.IsCompleted);
        task.ReleaseBlockedRun();
        await unregister.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Register_WhenTimerCreationFails_DoesNotLeaveTheTenantRegistered()
    {
        var task = new RecordingTask(_tenantAccessor);
        var schedule = new FailingFirstTimerSchedule(_schedule);
        await using var scheduler = CreateScheduler(task.GetType(), schedule: schedule);

        await Assert.ThrowsAsync<InvalidOperationException>(() => RegisterAsync(scheduler, task, "tenant-0"));
        await RegisterAsync(scheduler, task, "tenant-1");
        await _schedule.TickAsync();
        await scheduler.WhenIdleAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(["tenant-1"], task.ObservedTenantIds);
    }

    [Fact]
    public async Task Timer_DoesNotInheritTheContextOfTheRegisteringTenant()
    {
        var task = new RecordingTask(_tenantAccessor);
        var timerAmbientTenant = new TaskCompletionSource<Tenant?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var schedule = new AmbientObservingSchedule(_tenantAccessor, timerAmbientTenant);
        await using var scheduler = CreateScheduler(task.GetType(), schedule: schedule);

        using (_tenantAccessor.PushContext(new() { Id = "registering-tenant" }))
            await RegisterAsync(scheduler, task, "registering-tenant");

        var observed = await timerAmbientTenant.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Null(observed);
    }

    private TenantRecurringTaskScheduler CreateScheduler(Type taskType, int maxDegreeOfParallelism = 4, ISchedule? schedule = null, ILogger? logger = null)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new RecurringTaskOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism });
        var scheduleManager = new RecurringTaskScheduleManager(options, Substitute.For<ISystemClock>())
        {
            ScheduledTasks = { [taskType] = schedule ?? _schedule }
        };
        return new(scheduleManager, _tenantAccessor, options, logger ?? NullLogger.Instance);
    }

    private static Task<TenantRecurringTaskScheduler.Registration> RegisterAsync(TenantRecurringTaskScheduler scheduler, IRecurringTask task, string tenantId, CancellationToken tenantCancellationToken = default) =>
        scheduler.RegisterAsync(new() { Id = tenantId }, task, new DirectTaskExecutor(), tenantCancellationToken);

    private static async Task<List<TenantRecurringTaskScheduler.Registration>> RegisterTenantsAsync(TenantRecurringTaskScheduler scheduler, IRecurringTask task, int tenantCount)
    {
        var registrations = new List<TenantRecurringTaskScheduler.Registration>();

        for (var i = 0; i < tenantCount; i++)
            registrations.Add(await RegisterAsync(scheduler, task, $"tenant-{i}"));

        return registrations;
    }

    private static async Task RunCycleAsync(TenantRecurringTaskScheduler scheduler, Type taskType)
    {
        scheduler.RunCycle(taskType);
        await scheduler.WhenIdleAsync();
    }

    private class FailingFirstTimerSchedule(ISchedule inner) : ISchedule
    {
        private bool _failed;

        public ScheduledTimer CreateTimer(Func<Task> action, ILogger? logger = null)
        {
            if (_failed)
                return inner.CreateTimer(action, logger);

            _failed = true;
            throw new InvalidOperationException("Timer creation failed.");
        }
    }

    private class AmbientObservingSchedule(DefaultTenantAccessor tenantAccessor, TaskCompletionSource<Tenant?> observedTenant) : ISchedule
    {
        public ScheduledTimer CreateTimer(Func<Task> action, ILogger? logger = null)
        {
            var fired = false;

            return new(() =>
            {
                if (!fired)
                {
                    fired = true;
                    observedTenant.TrySetResult(tenantAccessor.Tenant);
                }

                return Task.CompletedTask;
            }, () => TimeSpan.FromMilliseconds(10), logger);
        }
    }

    private class NamedTask(string name, ConcurrentQueue<string> observed) : RecurringTask
    {
        public override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            observed.Enqueue(name);
            return Task.CompletedTask;
        }
    }

    private class RecordingTask(ITenantAccessor tenantAccessor) : RecurringTask
    {
        public ConcurrentQueue<string> ObservedTenantIds { get; } = new();

        public override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ObservedTenantIds.Enqueue(tenantAccessor.Tenant?.Id.NormalizeTenantId() ?? "<none>");
            return Task.CompletedTask;
        }
    }

    [RunOnceForAllTenants]
    private class RunOnceTask(ITenantAccessor tenantAccessor) : RecordingTask(tenantAccessor);

    private class DerivedRunOnceTask(ITenantAccessor tenantAccessor) : RunOnceTask(tenantAccessor);

    [SingleNodeTask(SingleNodeTaskScope.Host)]
    private class HostScopedTask(ITenantAccessor tenantAccessor) : RecordingTask(tenantAccessor);

    [RunOnceForAllTenants]
    private class GatedRunOnceTask : GatedTask;

    private class GatedTask : RecurringTask
    {
        private readonly SemaphoreSlim _release = new(0);
        private readonly SemaphoreSlim _started = new(0);
        private int _current;
        private int _peak;
        private int _startedCount;

        public int StartedCount => Volatile.Read(ref _startedCount);
        public int PeakConcurrency => Volatile.Read(ref _peak);

        public async Task WaitForStartedAsync(int count)
        {
            for (var i = 0; i < count; i++)
                Assert.True(await _started.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        public void Release(int count) => _release.Release(count);

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref _current);
            int peak;

            while (current > (peak = Volatile.Read(ref _peak)))
                Interlocked.CompareExchange(ref _peak, current, peak);

            Interlocked.Increment(ref _startedCount);
            _started.Release();
            Assert.True(await _release.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken));
            Interlocked.Decrement(ref _current);
        }
    }

    private class SpecialTenantTask(ITenantAccessor tenantAccessor, string specialTenantId, Func<CancellationToken, Task> specialRun) : RecurringTask
    {
        public ConcurrentQueue<string> CompletedTenantIds { get; } = new();

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var tenantId = tenantAccessor.TenantId;

            if (tenantId == specialTenantId)
                await specialRun(cancellationToken);

            CompletedTenantIds.Enqueue(tenantId);
        }
    }

    private class BlockingForOneTenantTask(ITenantAccessor tenantAccessor, string blockedTenantId) : RecurringTask
    {
        private readonly SemaphoreSlim _release = new(0);
        private readonly SemaphoreSlim _fastRuns = new(0);
        private readonly SemaphoreSlim _blockedStarts = new(0);
        private int _blockedStartCount;
        private int _fastRunCount;

        public int BlockedStartCount => Volatile.Read(ref _blockedStartCount);
        public int FastRunCount => Volatile.Read(ref _fastRunCount);

        public async Task WaitForFastRunsAsync(int count)
        {
            for (var i = 0; i < count; i++)
                Assert.True(await _fastRuns.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        public async Task WaitForBlockedStartsAsync(int count)
        {
            for (var i = 0; i < count; i++)
                Assert.True(await _blockedStarts.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        public void ReleaseBlockedRun() => _release.Release();

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (tenantAccessor.TenantId != blockedTenantId)
            {
                Interlocked.Increment(ref _fastRunCount);
                _fastRuns.Release();
                return;
            }

            Interlocked.Increment(ref _blockedStartCount);
            _blockedStarts.Release();
            Assert.True(await _release.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken));
        }
    }

    private class RecordingLogger : ILogger
    {
        private readonly ConcurrentQueue<LogEntry> _entries = new();

        public IReadOnlyCollection<LogEntry> Entries => _entries;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            _entries.Enqueue(new(logLevel, formatter(state, exception)));
    }

    private record LogEntry(LogLevel Level, string Message);

    private class DirectTaskExecutor : ITaskExecutor
    {
        public Task ExecuteTaskAsync(ITask task, CancellationToken cancellationToken) => task.ExecuteAsync(cancellationToken);
    }
}

internal class ControllableSchedule : ISchedule
{
    private volatile Func<Task>? _action;

    public int CreatedTimerCount { get; private set; }

    public Task TickAsync() => (_action?.Invoke() ?? Task.CompletedTask).WaitAsync(TimeSpan.FromSeconds(5));

    public ScheduledTimer CreateTimer(Func<Task> action, ILogger? logger = null)
    {
        CreatedTimerCount++;
        _action = action;
        return new(action, () => Timeout.InfiniteTimeSpan, logger);
    }
}
