using Elsa.Common;
using Elsa.Common.Multitenancy;
using JetBrains.Annotations;

namespace Elsa.Workflows.Runtime.Tasks;

/// <summary>
/// Periodically signals the bookmark queue processor to check for new items. This is a reliability measure that ensures stimuli never gets missed.
/// </summary>
[UsedImplicitly]
public class TriggerBookmarkQueueRecurringTask(IBookmarkQueueWorker bookmarkQueueWorker, IBookmarkQueueSignaler signaler, ITenantAccessor? tenantAccessor = null) : IRecurringTask
{
    private Tenant? _tenant;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _tenant = tenantAccessor?.Tenant;
        bookmarkQueueWorker.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        bookmarkQueueWorker.Stop();
        return Task.CompletedTask;
    }

    public Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var tenantContext = tenantAccessor?.PushContext(_tenant);
        return signaler.TriggerAsync(stoppingToken);
    }
}
