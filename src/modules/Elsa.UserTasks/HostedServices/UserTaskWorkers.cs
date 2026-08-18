using Elsa.Common;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.UserTasks.HostedServices;

/// <summary>
/// Shared plumbing for the User Tasks background workers: a periodic loop that never lets one failed pass
/// tear down the host, and a bounded scope per pass so scoped providers can be resolved safely.
/// </summary>
public abstract class UserTaskPeriodicWorker(IServiceScopeFactory scopeFactory, IOptions<UserTasksOptions> options, ILogger logger) : BackgroundService
{
    protected UserTasksOptions Options { get; } = options.Value;

    protected abstract TimeSpan Interval { get; }

    protected abstract Task ExecutePassAsync(IServiceProvider services, CancellationToken cancellationToken);

    /// <summary>The tenants a pass sweeps. Defaults to the configured default tenant when no catalog is set.</summary>
    protected IReadOnlyCollection<string> TenantIds => Options.WorkerTenantIds.Count > 0
        ? Options.WorkerTenantIds.ToArray()
        : [Options.DefaultTenantId];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await ExecutePassAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "A User Tasks background pass failed and will be retried");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    return;
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}

/// <summary>Marks tasks overdue and applies the reserved timeout outcome once a due date elapses.</summary>
public sealed class UserTaskDueWorker(IServiceScopeFactory scopeFactory, IOptions<UserTasksOptions> options, ILogger<UserTaskDueWorker> logger)
    : UserTaskPeriodicWorker(scopeFactory, options, logger)
{
    protected override TimeSpan Interval => Options.DueSweepInterval;

    protected override async Task ExecutePassAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dueService = services.GetRequiredService<IUserTaskDueService>();
        foreach (var tenantId in TenantIds)
            await dueService.MarkOverdueAsync(tenantId, cancellationToken: cancellationToken);
    }
}

/// <summary>Repairs projections that diverged from committed bookmarks after an interrupted write.</summary>
public sealed class UserTaskReconciliationWorker(IServiceScopeFactory scopeFactory, IOptions<UserTasksOptions> options, ILogger<UserTaskReconciliationWorker> logger)
    : UserTaskPeriodicWorker(scopeFactory, options, logger)
{
    protected override TimeSpan Interval => Options.ReconciliationInterval;

    protected override async Task ExecutePassAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var reconciler = services.GetRequiredService<IUserTaskReconciler>();
        foreach (var tenantId in TenantIds)
            await reconciler.ReconcileAsync(new UserTaskReconciliationRequest { TenantId = tenantId }, cancellationToken);
    }
}

/// <summary>
/// Drains the encrypted invitation outbox. A failed dispatch is rescheduled with the configured back-off and
/// is abandoned once the schedule is exhausted, so an undeliverable secret always expires rather than
/// lingering indefinitely.
/// </summary>
public sealed class UserTaskInvitationDeliveryWorker(IServiceScopeFactory scopeFactory, IOptions<UserTasksOptions> options, ILogger<UserTaskInvitationDeliveryWorker> logger)
    : UserTaskPeriodicWorker(scopeFactory, options, logger)
{
    private readonly ILogger<UserTaskInvitationDeliveryWorker> _logger = logger;

    protected override TimeSpan Interval => TimeSpan.FromSeconds(5);

    protected override async Task ExecutePassAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var outbox = services.GetRequiredService<IUserTaskInvitationOutbox>();
        var dispatcher = services.GetRequiredService<IUserTaskInvitationDispatcher>();
        var clock = services.GetRequiredService<ISystemClock>();

        foreach (var delivery in await outbox.DequeueDueAsync(50, cancellationToken))
        {
            try
            {
                await dispatcher.DispatchAsync(delivery, cancellationToken);
                await outbox.CompleteAsync(delivery.Id, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var delays = Options.InvitationDeliveryRetryDelays;
                var delay = delivery.Attempt < delays.Count ? delays[delivery.Attempt] : delays.Count > 0 ? delays[^1] : TimeSpan.FromMinutes(5);
                // The exception may carry recipient details, so only the invitation ID is logged.
                _logger.LogWarning(exception, "Invitation delivery {InvitationId} failed on attempt {Attempt}", delivery.InvitationId, delivery.Attempt + 1);
                await outbox.RescheduleAsync(delivery.Id, clock.UtcNow.Add(delay), cancellationToken);
            }
        }
    }
}
