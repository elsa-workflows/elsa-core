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

    private static TenantTaskLifecycleCoordinator CreateCoordinator()
    {
        var scheduleManager = new RecurringTaskScheduleManager(Microsoft.Extensions.Options.Options.Create(new RecurringTaskOptions()), Substitute.For<ISystemClock>());
        return new TenantTaskLifecycleCoordinator(scheduleManager, NullLogger<TenantTaskLifecycleCoordinator>.Instance);
    }

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
