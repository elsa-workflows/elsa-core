using System.Collections.Concurrent;
using Elsa.Common.Helpers;
using Elsa.Common.RecurringTasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Common.Multitenancy;

/// <summary>
/// Runs recurring tasks for every active tenant from one timer per task type. A tick starts a run for each tenant that
/// is not already running without waiting for it; runs of one task type share a parallelism limit. Task types marked
/// with <see cref="RunOnceForAllTenantsAttribute"/> run for one tenant per tick.
/// </summary>
internal sealed class TenantRecurringTaskScheduler(
    RecurringTaskScheduleManager scheduleManager,
    ITenantAccessor tenantAccessor,
    IOptions<RecurringTaskOptions> options,
    ILogger logger) : IAsyncDisposable
{
    private readonly ConcurrentDictionary<Type, ScheduledTaskType> _scheduledTaskTypes = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _disposed;

    public async Task<Registration> RegisterAsync(Tenant tenant, IRecurringTask task, ITaskExecutor taskExecutor, CancellationToken tenantCancellationToken)
    {
        var registration = new Registration(tenant, task, taskExecutor, tenantCancellationToken);
        var taskType = registration.TaskType;

        await _gate.WaitAsync();

        try
        {
            if (_disposed)
                return registration;

            var scheduledTaskType = _scheduledTaskTypes.GetOrAdd(taskType, t => new(t.IsDefined(typeof(RunOnceForAllTenantsAttribute), inherit: true), Math.Max(1, options.Value.MaxDegreeOfParallelism)));
            scheduledTaskType.Timer ??= CreateTimer(taskType);
            scheduledTaskType.Registrations[registration] = true;
            return registration;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UnregisterAsync(Registration registration)
    {
        Task? run;

        lock (registration)
        {
            registration.Removed = true;
            run = registration.Run;
        }

        await _gate.WaitAsync();

        try
        {
            if (_scheduledTaskTypes.TryGetValue(registration.TaskType, out var scheduledTaskType)
                && scheduledTaskType.Registrations.TryRemove(registration, out _)
                && scheduledTaskType.Registrations.IsEmpty
                && _scheduledTaskTypes.TryRemove(registration.TaskType, out var removed))
            {
                await removed.DisposeAsync();
            }
        }
        finally
        {
            _gate.Release();
        }

        if (run != null)
            await run;
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();

        try
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var scheduledTaskType in _scheduledTaskTypes.Values)
                await scheduledTaskType.DisposeAsync();

            _scheduledTaskTypes.Clear();
        }
        finally
        {
            _gate.Release();
        }
    }

    internal Task WhenIdleAsync()
    {
        var runs = _scheduledTaskTypes.Values.SelectMany(t => t.Registrations.Keys).Select(r => r.Run).OfType<Task>();
        return Task.WhenAll(runs);
    }

    internal void RunCycle(Type taskType)
    {
        if (!_scheduledTaskTypes.TryGetValue(taskType, out var scheduledTaskType))
            return;

        var registrations = scheduledTaskType.Registrations.Keys.ToArray();

        if (scheduledTaskType.RunOnceForAllTenants)
        {
            if (registrations.Any(r => r.IsRunning))
                return;

            // The lowest ordinal tenant ID wins, so the default tenant ("") is preferred; stopping tenants are skipped.
            var registration = registrations
                .Where(r => !r.CancellationToken.IsCancellationRequested)
                .MinBy(r => r.TenantId, StringComparer.Ordinal);

            if (registration != null)
                StartRun(scheduledTaskType, registration);

            return;
        }

        foreach (var registration in registrations)
            StartRun(scheduledTaskType, registration);
    }

    private ScheduledTimer CreateTimer(Type taskType)
    {
        using (ExecutionContext.SuppressFlow())
            return scheduleManager.GetScheduleFor(taskType).CreateTimer(() =>
            {
                RunCycle(taskType);
                return Task.CompletedTask;
            }, logger);
    }

    private void StartRun(ScheduledTaskType scheduledTaskType, Registration registration)
    {
        lock (registration)
        {
            if (registration.Removed || registration.CancellationToken.IsCancellationRequested || registration.IsRunning)
                return;

            registration.Run = Task.Run(() => RunAsync(scheduledTaskType, registration));
        }
    }

    private async Task RunAsync(ScheduledTaskType scheduledTaskType, Registration registration)
    {
        try
        {
            await scheduledTaskType.Limiter.WaitAsync(registration.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            await RunForTenantAsync(registration);
        }
        finally
        {
            scheduledTaskType.Limiter.Release();
        }
    }

    private async Task RunForTenantAsync(Registration registration)
    {
        lock (registration)
        {
            if (registration.Removed || registration.CancellationToken.IsCancellationRequested)
                return;
        }

        var taskName = registration.Task.GetType().Name;

        try
        {
            using (tenantAccessor.PushContext(registration.Tenant))
                await registration.TaskExecutor.ExecuteTaskAsync(registration.Task, registration.CancellationToken);
        }
        catch (OperationCanceledException e) when (registration.CancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(e, "Recurring task {TaskType} was cancelled for tenant {TenantId}", taskName, registration.Tenant.Id);
        }
        catch (Exception e) when (!e.IsFatal())
        {
            logger.LogError(e, "Recurring task {TaskType} failed with an error for tenant {TenantId}", taskName, registration.Tenant.Id);
        }
    }

    private sealed class ScheduledTaskType(bool runOnceForAllTenants, int maxDegreeOfParallelism) : IAsyncDisposable
    {
        public bool RunOnceForAllTenants { get; } = runOnceForAllTenants;
        public ConcurrentDictionary<Registration, bool> Registrations { get; } = new();

        public SemaphoreSlim Limiter { get; } = new(maxDegreeOfParallelism, maxDegreeOfParallelism);
        public ScheduledTimer? Timer { get; set; }

        public async ValueTask DisposeAsync()
        {
            if (Timer != null)
                await Timer.DisposeAsync();

            Timer = null;
            await Task.WhenAll(Registrations.Keys.Select(r => r.Run).OfType<Task>());
            Registrations.Clear();
        }
    }

    internal sealed class Registration(Tenant tenant, IRecurringTask task, ITaskExecutor taskExecutor, CancellationToken cancellationToken)
    {
        public Tenant Tenant { get; } = tenant;
        public IRecurringTask Task { get; } = task;
        public ITaskExecutor TaskExecutor { get; } = taskExecutor;
        public CancellationToken CancellationToken { get; } = cancellationToken;
        public Type TaskType { get; } = task.GetType();
        public string TenantId { get; } = tenant.Id.NormalizeTenantId();
        public Task? Run { get; set; }
        public bool Removed { get; set; }
        public bool IsRunning => Run is { IsCompleted: false };
    }
}
