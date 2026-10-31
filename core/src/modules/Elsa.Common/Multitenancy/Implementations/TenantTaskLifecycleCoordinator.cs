using System.Collections.Concurrent;
using System.Reflection;
using Elsa.Common.Helpers;
using Elsa.Common.RecurringTasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Coordinates the lifecycle of startup, background, and recurring tasks for tenants.
/// Executes tasks in the proper sequence: startup tasks first, then background tasks, then recurring tasks.
/// </summary>
public class TenantTaskLifecycleCoordinator(
    RecurringTaskScheduleManager scheduleManager,
    ITenantAccessor tenantAccessor,
    IOptions<RecurringTaskOptions> options,
    ILogger<TenantTaskLifecycleCoordinator> logger) : IAsyncDisposable
{
    private readonly TenantRecurringTaskScheduler _recurringTaskScheduler = new(scheduleManager, tenantAccessor, options, logger);
    private readonly ConcurrentDictionary<string, TenantRuntimeState> _tenantStates = new();
    private readonly CancellationTokenSource _shutdownCancellationTokenSource = new();
    private int _disposeRequested;

    [Obsolete("Use the constructor that accepts an ITenantAccessor and IOptions<RecurringTaskOptions> instead. Removed in the next major version.")]
    public TenantTaskLifecycleCoordinator(RecurringTaskScheduleManager scheduleManager, ILogger<TenantTaskLifecycleCoordinator> logger)
        : this(scheduleManager, new DefaultTenantAccessor(), Options.Create(new RecurringTaskOptions()), logger)
    {
    }

    public async Task ActivateTenantAsync(TenantActivatedEventArgs args)
    {
        if (Volatile.Read(ref _disposeRequested) == 1)
            return;

        using var activationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(args.CancellationToken, _shutdownCancellationTokenSource.Token);
        var cancellationToken = activationTokenSource.Token;
        var tenantScope = args.TenantScope;
        var taskExecutor = tenantScope.ServiceProvider.GetRequiredService<ITaskExecutor>();
        var tenantId = GetTenantId(args.Tenant);
        var state = _tenantStates.GetOrAdd(tenantId, static _ => new TenantRuntimeState());

        await state.Gate.WaitAsync(cancellationToken);

        try
        {
            if (Volatile.Read(ref _disposeRequested) == 1)
                return;

            await StopTenantCoreAsync(state, cancellationToken);
            state.CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Step 1: Run startup tasks (with dependency ordering)
            await RunStartupTasksAsync(tenantScope, taskExecutor, cancellationToken);

            // Step 2: Run background tasks
            await RunBackgroundTasksAsync(tenantScope, taskExecutor, state, cancellationToken);

            // Step 3: Start recurring tasks
            await StartRecurringTasksAsync(args.Tenant, tenantScope, taskExecutor, state, cancellationToken);
        }
        catch
        {
            await StopTenantCoreAsync(state, cancellationToken);
            throw;
        }
        finally
        {
            state.Gate.Release();
        }
    }

    public async Task DeactivateTenantAsync(TenantDeactivatedEventArgs args)
    {
        var tenantId = GetTenantId(args.Tenant);

        if (!_tenantStates.TryGetValue(tenantId, out var state))
            return;

        await state.Gate.WaitAsync(args.CancellationToken);

        try
        {
            _tenantStates.TryRemove(tenantId, out _);
            await StopTenantCoreAsync(state, args.CancellationToken);
        }
        finally
        {
            state.Gate.Release();
        }
    }

    private async Task RunStartupTasksAsync(ITenantScope tenantScope, ITaskExecutor taskExecutor, CancellationToken cancellationToken)
    {
        var startupTasks = tenantScope.ServiceProvider.GetServices<IStartupTask>()
            .OrderBy(x => x.GetType().GetCustomAttribute<OrderAttribute>()?.Order ?? 0f)
            .ToList();

        // First apply OrderAttribute to determine a base order, then perform topological sorting.
        // The topological sort is the final ordering step to ensure dependency constraints are respected.
        var sortedTasks = TopologicalTaskSorter.Sort(startupTasks).ToList();
        foreach (var task in sortedTasks)
            await taskExecutor.ExecuteTaskAsync(task, cancellationToken);
    }

    private async Task RunBackgroundTasksAsync(ITenantScope tenantScope, ITaskExecutor taskExecutor, TenantRuntimeState state, CancellationToken cancellationToken)
    {
        var backgroundTasks = tenantScope.ServiceProvider.GetServices<IBackgroundTask>().ToList();
        var backgroundTaskStarter = tenantScope.ServiceProvider.GetRequiredService<IBackgroundTaskStarter>();
        var tenantCancellationToken = state.CancellationTokenSource?.Token ?? cancellationToken;

        foreach (var backgroundTask in backgroundTasks)
        {
            await backgroundTaskStarter.StartAsync(backgroundTask, tenantCancellationToken);
            state.BackgroundTasks.Add(new(backgroundTask, backgroundTaskStarter));

            var task = RunBackgroundTaskAsync(backgroundTask, taskExecutor, tenantCancellationToken);

            if (!task.IsCompleted)
                state.RunningBackgroundTasks.Add(task);
        }
    }

    private async Task RunBackgroundTaskAsync(IBackgroundTask backgroundTask, ITaskExecutor taskExecutor, CancellationToken cancellationToken)
    {
        try
        {
            await taskExecutor.ExecuteTaskAsync(backgroundTask, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Background task {TaskType} was cancelled", backgroundTask.GetType().Name);
        }
        catch (Exception e) when (!e.IsFatal())
        {
            logger.LogError(e, "Background task {TaskType} failed with an error", backgroundTask.GetType().Name);
        }
    }

    private async Task StartRecurringTasksAsync(Tenant tenant, ITenantScope tenantScope, ITaskExecutor taskExecutor, TenantRuntimeState state, CancellationToken cancellationToken)
    {
        var recurringTasks = tenantScope.ServiceProvider.GetServices<IRecurringTask>().ToList();
        var tenantCancellationToken = state.CancellationTokenSource?.Token ?? cancellationToken;

        foreach (var task in recurringTasks)
        {
            state.RecurringTasks.Add(task);
            await task.StartAsync(cancellationToken);
            state.RecurringTaskRegistrations.Add(await _recurringTaskScheduler.RegisterAsync(tenant, task, taskExecutor, tenantCancellationToken));
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeRequested, 1) == 1)
            return;

        try
        {
            await _shutdownCancellationTokenSource.CancelAsync();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed by a concurrent path.
        }

        foreach (var state in _tenantStates.Values)
        {
            await state.Gate.WaitAsync();

            try
            {
                await StopTenantCoreAsync(state, CancellationToken.None);
            }
            finally
            {
                state.Gate.Release();
            }
        }

        try
        {
            _shutdownCancellationTokenSource.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed by a concurrent path.
        }

        await _recurringTaskScheduler.DisposeAsync();
    }

    private async Task StopTenantCoreAsync(TenantRuntimeState state, CancellationToken cancellationToken)
    {
        var cancellationTokenSource = state.CancellationTokenSource;

        // Cancel first so callbacks and long-running tasks can drain while resources are being disposed.
        if (cancellationTokenSource != null)
        {
            try
            {
                await cancellationTokenSource.CancelAsync();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed by a concurrent path.
            }
            catch (Exception e) when (!e.IsFatal())
            {
                logger.LogWarning(e, "Failed to cancel tenant task cancellation token source while stopping tenant tasks");
            }
        }

        foreach (var registration in state.RecurringTaskRegistrations)
        {
            try
            {
                await _recurringTaskScheduler.UnregisterAsync(registration);
            }
            catch (Exception e) when (!e.IsFatal())
            {
                logger.LogWarning(e, "Failed to unregister recurring task {TaskType} while stopping tenant tasks", registration.TaskType.Name);
            }
        }
        state.RecurringTaskRegistrations.Clear();

        foreach (var backgroundTask in state.BackgroundTasks)
        {
            try
            {
                await backgroundTask.Starter.StopAsync(backgroundTask.Task, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected if caller requested cancellation during tenant deactivation.
            }
            catch (Exception e) when (!e.IsFatal())
            {
                logger.LogError(e, "Failed to stop background task {TaskType}", backgroundTask.Task.GetType().Name);
            }
        }
        state.BackgroundTasks.Clear();

        if (state.RunningBackgroundTasks.Count > 0)
        {
            try
            {
                await Task.WhenAll(state.RunningBackgroundTasks);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested.
            }
            catch (AggregateException e)
            {
                logger.LogError(e, "One or more background tasks failed while stopping tenant tasks");
            }
            catch (InvalidOperationException e)
            {
                logger.LogError(e, "Background task collection was in an invalid state while stopping tenant tasks");
            }
            state.RunningBackgroundTasks.Clear();
        }

        foreach (var recurringTask in state.RecurringTasks)
        {
            try
            {
                await recurringTask.StopAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected if caller requested cancellation during tenant deactivation.
            }
            catch (Exception e) when (!e.IsFatal())
            {
                logger.LogError(e, "Failed to stop recurring task {TaskType}", recurringTask.GetType().Name);
            }
        }
        state.RecurringTasks.Clear();

        if (cancellationTokenSource == null)
            return;

        try
        {
            cancellationTokenSource.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed by a concurrent path.
        }
        catch (Exception e) when (!e.IsFatal())
        {
            logger.LogWarning(e, "Failed to dispose tenant task cancellation token source");
        }

        state.CancellationTokenSource = null;
    }

    private static string GetTenantId(Tenant tenant) => tenant.Id.NormalizeTenantId();

    private class TenantRuntimeState
    {
        // SemaphoreSlim only allocates a kernel handle when AvailableWaitHandle is accessed.
        // Since we exclusively use WaitAsync(), no kernel handle is ever created and disposal is a no-op.
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public List<Task> RunningBackgroundTasks { get; } = [];
        public List<BackgroundTaskRegistration> BackgroundTasks { get; } = [];
        public List<IRecurringTask> RecurringTasks { get; } = [];
        public List<TenantRecurringTaskScheduler.Registration> RecurringTaskRegistrations { get; } = [];
        public CancellationTokenSource? CancellationTokenSource { get; set; }
    }

    private record BackgroundTaskRegistration(IBackgroundTask Task, IBackgroundTaskStarter Starter);
}
