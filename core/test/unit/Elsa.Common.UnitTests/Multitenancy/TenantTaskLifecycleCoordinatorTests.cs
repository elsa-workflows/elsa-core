using System.Collections.Concurrent;
using Elsa.Common.Multitenancy;
using Elsa.Common.RecurringTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Elsa.Common.UnitTests.Multitenancy;

public class TenantTaskLifecycleCoordinatorTests : IAsyncDisposable
{
    private readonly TenantTaskLifecycleCoordinator _coordinator = CreateCoordinator();
    private readonly Tenant _tenant = new() { Id = "test-tenant" };
    private readonly TrackingRecurringTask _recurringTask = new();
    private readonly IServiceProvider _serviceProvider;

    public TenantTaskLifecycleCoordinatorTests() =>
        _serviceProvider = BuildTenantServiceProvider(recurringTasks: [_recurringTask]);

    public ValueTask DisposeAsync() => _coordinator.DisposeAsync();

    [Fact]
    public async Task ActivateAsync_WithQueuedWork_ReturnsBeforeWorkItemCompletes()
    {
        QueueBlockingWorkStartupTask? startupTask = null;
        var serviceProvider = BuildTenantServiceProvider(
            startupTasks: [sp => startupTask = ActivatorUtilities.CreateInstance<QueueBlockingWorkStartupTask>(sp)],
            backgroundTasks: [sp => ActivatorUtilities.CreateInstance<TenantBackgroundWorkQueueWorker>(sp)]);
        var activationTask = _coordinator.ActivateTenantAsync(new TenantActivatedEventArgs(_tenant, CreateTenantScope(_tenant, serviceProvider), CancellationToken.None));

        var completedTask = await Task.WhenAny(activationTask, Task.Delay(TimeSpan.FromSeconds(1)));

        Assert.Same(activationTask, completedTask);
        await activationTask;
        await startupTask!.WorkStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.False(startupTask.WorkCompleted.Task.IsCompleted);

        startupTask.ReleaseWork();
        await startupTask.WorkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DeactivateAsync_WithRunningBackgroundTask_StopsTask()
    {
        var backgroundTask = new TrackingBackgroundTask();
        var serviceProvider = BuildTenantServiceProvider(backgroundTasks: [_ => backgroundTask]);

        await _coordinator.ActivateTenantAsync(new TenantActivatedEventArgs(_tenant, CreateTenantScope(_tenant, serviceProvider), CancellationToken.None));
        await backgroundTask.Started.Task.WaitAsync(TimeSpan.FromSeconds(1));
        await _coordinator.DeactivateTenantAsync(DeactivationArgs(serviceProvider));

        Assert.True(backgroundTask.WasStopCalled);
        Assert.True(backgroundTask.WasCancelled);
    }

    /// <summary>
    /// Regression test: before the fix, <c>TryRemove</c> was called before <c>WaitAsync</c>,
    /// so a cancelled <c>WaitAsync</c> would orphan the state (removed from the dictionary but
    /// never stopped). <see cref="IAsyncDisposable.DisposeAsync"/> would then skip cleanup,
    /// leaving background tasks and recurring-task timers running until process exit.
    /// </summary>
    [Fact]
    public async Task DeactivateAsync_WhenCancelledBeforeGateAcquired_StateRemainsForDispose()
    {
        await ActivateAsync();

        using var cancelledCts = new CancellationTokenSource();
        await cancelledCts.CancelAsync();

        // WaitAsync throws immediately for an already-cancelled token, before TryRemove is reached.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _coordinator.DeactivateTenantAsync(DeactivationArgs(cancelledCts.Token)));

        // DisposeAsync must still find the state and stop the recurring task.
        await _coordinator.DisposeAsync();

        Assert.True(_recurringTask.WasStopCalled);
    }

    [Fact]
    public async Task DeactivateAsync_AfterActivation_StopsRecurringTasks()
    {
        await ActivateAsync();
        await _coordinator.DeactivateTenantAsync(DeactivationArgs());
        Assert.True(_recurringTask.WasStopCalled);
    }

    [Fact]
    public async Task ActivateAndDeactivateAsync_WithNullTenantId_TreatsTenantAsDefaultTenant()
    {
        var tenant = new Tenant { Id = null! };

        await ActivateAsync(tenant);
        await _coordinator.DeactivateTenantAsync(DeactivationArgs(tenant));

        Assert.True(_recurringTask.WasStopCalled);
    }

    [Fact]
    public async Task DisposeAsync_WithActiveTenant_StopsRecurringTasks()
    {
        await ActivateAsync();
        await _coordinator.DisposeAsync();
        Assert.True(_recurringTask.WasStopCalled);
    }

    [Fact]
    public async Task Tick_WithTwoTenantsOnOneTaskType_KeepsRunningTheRemainingTenantAfterOneIsDeactivated()
    {
        var schedule = new ControllableSchedule();
        var log = new RunLog();
        await using var coordinator = CreateCoordinator(typeof(LoggingRecurringTask), schedule);
        var tenantA = new Tenant { Id = "tenant-a" };
        var tenantB = new Tenant { Id = "tenant-b" };
        var providerA = BuildTenantServiceProvider(recurringTasks: [new LoggingRecurringTask(log)]);
        var providerB = BuildTenantServiceProvider(recurringTasks: [new LoggingRecurringTask(log)]);
        await coordinator.ActivateTenantAsync(new TenantActivatedEventArgs(tenantA, CreateTenantScope(tenantA, providerA), CancellationToken.None));
        await coordinator.ActivateTenantAsync(new TenantActivatedEventArgs(tenantB, CreateTenantScope(tenantB, providerB), CancellationToken.None));

        await coordinator.DeactivateTenantAsync(new TenantDeactivatedEventArgs(tenantA, CreateTenantScope(tenantA, providerA), CancellationToken.None));
        await schedule.TickAsync();
        await log.WaitForExecutionsAsync(1);
        await coordinator.DeactivateTenantAsync(new TenantDeactivatedEventArgs(tenantB, CreateTenantScope(tenantB, providerB), CancellationToken.None));

        Assert.Equal(["start", "start", "stop", "execute:tenant-b", "stop"], log.Events);
    }

    [Fact]
    public async Task Activate_WhileRecurringTaskIsStarting_DoesNotRunTheTaskBeforeStartCompletes()
    {
        var schedule = new ControllableSchedule();
        var log = new RunLog();
        var startGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = new LoggingRecurringTask(log, startGate.Task);
        await using var coordinator = CreateCoordinator(typeof(LoggingRecurringTask), schedule);
        var serviceProvider = BuildTenantServiceProvider(recurringTasks: [task]);

        var activation = coordinator.ActivateTenantAsync(new TenantActivatedEventArgs(_tenant, CreateTenantScope(_tenant, serviceProvider), CancellationToken.None));
        await task.StartEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var timersWhileStarting = schedule.CreatedTimerCount;
        startGate.SetResult();
        await activation;
        await schedule.TickAsync();
        await log.WaitForExecutionsAsync(1);
        await coordinator.DeactivateTenantAsync(DeactivationArgs(serviceProvider));

        Assert.Equal(0, timersWhileStarting);
        Assert.Equal(["start", "execute:test-tenant", "stop"], log.Events);
    }

    [Fact]
    public async Task Tick_WithMaxDegreeOfParallelismOfOne_DoesNotOverlapTenantRuns()
    {
        var schedule = new ControllableSchedule();
        var probe = new ConcurrencyProbe();
        await using var coordinator = CreateCoordinator(typeof(ProbedRecurringTask), schedule, maxDegreeOfParallelism: 1);
        var tenantA = new Tenant { Id = "tenant-a" };
        var tenantB = new Tenant { Id = "tenant-b" };
        var providerA = BuildTenantServiceProvider(recurringTasks: [new ProbedRecurringTask(probe)]);
        var providerB = BuildTenantServiceProvider(recurringTasks: [new ProbedRecurringTask(probe)]);
        await coordinator.ActivateTenantAsync(new TenantActivatedEventArgs(tenantA, CreateTenantScope(tenantA, providerA), CancellationToken.None));
        await coordinator.ActivateTenantAsync(new TenantActivatedEventArgs(tenantB, CreateTenantScope(tenantB, providerB), CancellationToken.None));

        await schedule.TickAsync();
        await probe.WaitForStartedAsync(1);
        probe.Release(1);
        await probe.WaitForStartedAsync(1);
        probe.Release(1);
        await coordinator.DeactivateTenantAsync(new TenantDeactivatedEventArgs(tenantA, CreateTenantScope(tenantA, providerA), CancellationToken.None));
        await coordinator.DeactivateTenantAsync(new TenantDeactivatedEventArgs(tenantB, CreateTenantScope(tenantB, providerB), CancellationToken.None));

        Assert.Equal(1, probe.PeakConcurrency);
    }

    // === Instance helpers ===

    private Task ActivateAsync(Tenant? tenant = null)
    {
        tenant ??= _tenant;
        return _coordinator.ActivateTenantAsync(new TenantActivatedEventArgs(tenant, CreateTenantScope(tenant, _serviceProvider), CancellationToken.None));
    }

    private TenantDeactivatedEventArgs DeactivationArgs(CancellationToken cancellationToken = default) =>
        DeactivationArgs(_tenant, cancellationToken);

    private TenantDeactivatedEventArgs DeactivationArgs(IServiceProvider serviceProvider, CancellationToken cancellationToken = default) =>
        new(_tenant, CreateTenantScope(_tenant, serviceProvider), cancellationToken);

    private TenantDeactivatedEventArgs DeactivationArgs(Tenant tenant, CancellationToken cancellationToken = default) =>
        new(tenant, CreateTenantScope(tenant, _serviceProvider), cancellationToken);

    // === Static helpers ===

    private static TenantTaskLifecycleCoordinator CreateCoordinator(int maxDegreeOfParallelism = 4) =>
        CreateCoordinator(new RecurringTaskScheduleManager(Microsoft.Extensions.Options.Options.Create(new RecurringTaskOptions()), Substitute.For<ISystemClock>()), maxDegreeOfParallelism);

    private static TenantTaskLifecycleCoordinator CreateCoordinator(Type taskType, ISchedule schedule, int maxDegreeOfParallelism = 4)
    {
        var scheduleManager = new RecurringTaskScheduleManager(Microsoft.Extensions.Options.Options.Create(new RecurringTaskOptions()), Substitute.For<ISystemClock>())
        {
            ScheduledTasks = { [taskType] = schedule }
        };
        return CreateCoordinator(scheduleManager, maxDegreeOfParallelism);
    }

    private static TenantTaskLifecycleCoordinator CreateCoordinator(RecurringTaskScheduleManager scheduleManager, int maxDegreeOfParallelism) =>
        new(scheduleManager, new DefaultTenantAccessor(), Microsoft.Extensions.Options.Options.Create(new RecurringTaskOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism }), NullLogger<TenantTaskLifecycleCoordinator>.Instance);

    private static IServiceProvider BuildTenantServiceProvider(
        IEnumerable<IRecurringTask>? recurringTasks = null,
        IEnumerable<Func<IServiceProvider, IStartupTask>>? startupTasks = null,
        IEnumerable<Func<IServiceProvider, IBackgroundTask>>? backgroundTasks = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITaskExecutor>(new DirectTaskExecutor());
        services.AddSingleton<IBackgroundTaskStarter, DirectBackgroundTaskStarter>();
        services.AddSingleton<ITenantBackgroundWorkQueue, TenantBackgroundWorkQueue>();
        services.AddSingleton<ILogger<TenantBackgroundWorkQueueWorker>>(NullLogger<TenantBackgroundWorkQueueWorker>.Instance);
        foreach (var taskFactory in startupTasks ?? [])
            services.AddSingleton<IStartupTask>(taskFactory);
        foreach (var task in recurringTasks ?? [])
            services.AddSingleton<IRecurringTask>(task);
        foreach (var taskFactory in backgroundTasks ?? [])
            services.AddSingleton<IBackgroundTask>(taskFactory);
        return services.BuildServiceProvider();
    }

    private static TenantScope CreateTenantScope(Tenant tenant, IServiceProvider serviceProvider)
    {
        var serviceScope = Substitute.For<IServiceScope>();
        serviceScope.ServiceProvider.Returns(serviceProvider);
        var tenantAccessor = Substitute.For<ITenantAccessor>();
        tenantAccessor.PushContext(Arg.Any<Tenant?>()).Returns(Substitute.For<IDisposable>());
        return new TenantScope(serviceScope, tenantAccessor, tenant);
    }

    // === Test doubles ===

    private class TrackingRecurringTask : IRecurringTask
    {
        public bool WasStopCalled { get; private set; }

        public Task ExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken)
        {
            WasStopCalled = true;
            return Task.CompletedTask;
        }
    }

    private class RunLog
    {
        private readonly ConcurrentQueue<string> _events = new();
        private readonly SemaphoreSlim _executions = new(0);

        public IReadOnlyCollection<string> Events => _events.ToArray();

        public void Add(string entry) => _events.Enqueue(entry);

        public void AddExecution(string entry)
        {
            _events.Enqueue(entry);
            _executions.Release();
        }

        public async Task WaitForExecutionsAsync(int count)
        {
            for (var i = 0; i < count; i++)
                Assert.True(await _executions.WaitAsync(TimeSpan.FromSeconds(5)));
        }
    }

    private class LoggingRecurringTask(RunLog log, Task? startGate = null) : RecurringTask
    {
        private readonly DefaultTenantAccessor _tenantAccessor = new();

        public TaskCompletionSource StartEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            log.AddExecution($"execute:{_tenantAccessor.TenantId}");
            return Task.CompletedTask;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            StartEntered.TrySetResult();

            if (startGate != null)
                await startGate;

            log.Add("start");
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            log.Add("stop");
            return Task.CompletedTask;
        }
    }

    private class ConcurrencyProbe
    {
        private readonly SemaphoreSlim _release = new(0);
        private readonly SemaphoreSlim _started = new(0);
        private int _current;
        private int _peak;

        public int PeakConcurrency => Volatile.Read(ref _peak);

        public async Task WaitForStartedAsync(int count)
        {
            for (var i = 0; i < count; i++)
                Assert.True(await _started.WaitAsync(TimeSpan.FromSeconds(5)));
        }

        public void Release(int count) => _release.Release(count);

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref _current);
            int peak;

            while (current > (peak = Volatile.Read(ref _peak)))
                Interlocked.CompareExchange(ref _peak, current, peak);

            _started.Release();
            await _release.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            Interlocked.Decrement(ref _current);
        }
    }

    private class ProbedRecurringTask(ConcurrencyProbe probe) : RecurringTask
    {
        public override Task ExecuteAsync(CancellationToken cancellationToken) => probe.RunAsync(cancellationToken);
    }

    private class QueueBlockingWorkStartupTask(ITenantBackgroundWorkQueue workQueue) : IStartupTask
    {
        private readonly TaskCompletionSource _releaseWork = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource WorkStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource WorkCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await workQueue.EnqueueAsync(async (_, ct) =>
            {
                WorkStarted.SetResult();
                await _releaseWork.Task.WaitAsync(ct);
                WorkCompleted.SetResult();
            }, cancellationToken);
        }

        public void ReleaseWork() => _releaseWork.SetResult();
    }

    private class TrackingBackgroundTask : BackgroundTask
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool WasCancelled { get; private set; }
        public bool WasStopCalled { get; private set; }

        public override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Started.SetResult();

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                WasCancelled = true;
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            WasStopCalled = true;
            return Task.CompletedTask;
        }
    }

    private class DirectTaskExecutor : ITaskExecutor
    {
        public Task ExecuteTaskAsync(ITask task, CancellationToken cancellationToken) => task.ExecuteAsync(cancellationToken);
    }

    private class DirectBackgroundTaskStarter : IBackgroundTaskStarter
    {
        public Task StartAsync(IBackgroundTask task, CancellationToken cancellationToken) => task.StartAsync(cancellationToken);
        public Task StopAsync(IBackgroundTask task, CancellationToken cancellationToken) => task.StopAsync(cancellationToken);
    }
}
