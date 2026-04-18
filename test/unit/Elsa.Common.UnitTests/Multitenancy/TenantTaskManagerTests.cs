using Elsa.Common.Multitenancy;
using Elsa.Common.Multitenancy.EventHandlers;
using Elsa.Common.RecurringTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Common.UnitTests.Multitenancy;

public class TenantTaskManagerTests : IAsyncDisposable
{
    private readonly TenantTaskManager _manager = CreateManager();
    private readonly Tenant _tenant = new() { Id = "test-tenant" };
    private readonly TrackingRecurringTask _recurringTask = new();
    private readonly IServiceProvider _serviceProvider;

    public TenantTaskManagerTests() =>
        _serviceProvider = BuildTenantServiceProvider(recurringTasks: [_recurringTask]);

    public ValueTask DisposeAsync() => _manager.DisposeAsync();

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
            () => _manager.TenantDeactivatedAsync(DeactivationArgs(cancelledCts.Token)));

        // DisposeAsync must still find the state and stop the recurring task.
        await _manager.DisposeAsync();

        Assert.True(_recurringTask.WasStopCalled);
    }

    [Fact]
    public async Task DeactivateAsync_AfterActivation_StopsRecurringTasks()
    {
        await ActivateAsync();
        await _manager.TenantDeactivatedAsync(DeactivationArgs());
        Assert.True(_recurringTask.WasStopCalled);
    }

    [Fact]
    public async Task DisposeAsync_WithActiveTenant_StopsRecurringTasks()
    {
        await ActivateAsync();
        await _manager.DisposeAsync();
        Assert.True(_recurringTask.WasStopCalled);
    }

    // === Instance helpers ===

    private Task ActivateAsync() =>
        _manager.TenantActivatedAsync(new TenantActivatedEventArgs(_tenant, CreateTenantScope(_tenant, _serviceProvider), CancellationToken.None));

    private TenantDeactivatedEventArgs DeactivationArgs(CancellationToken cancellationToken = default) =>
        new(_tenant, CreateTenantScope(_tenant, _serviceProvider), cancellationToken);

    // === Static helpers ===

    private static TenantTaskManager CreateManager()
    {
        var scheduleManager = new RecurringTaskScheduleManager(Options.Create(new RecurringTaskOptions()), Substitute.For<ISystemClock>());
        return new TenantTaskManager(scheduleManager, NullLogger<TenantTaskManager>.Instance);
    }

    private static IServiceProvider BuildTenantServiceProvider(IEnumerable<IRecurringTask>? recurringTasks = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITaskExecutor>(new InstantTaskExecutor());
        services.AddSingleton(Substitute.For<IBackgroundTaskStarter>());
        foreach (var task in recurringTasks ?? [])
            services.AddSingleton<IRecurringTask>(task);
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

    private class InstantTaskExecutor : ITaskExecutor
    {
        public Task ExecuteTaskAsync(ITask task, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
