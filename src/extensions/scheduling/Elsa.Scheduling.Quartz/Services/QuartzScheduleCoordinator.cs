using Elsa.Common.DistributedHosting;
using Elsa.Scheduling.Quartz.Contracts;
using Medallion.Threading;
using Microsoft.Extensions.Options;
using Quartz;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// Coordinates mutations for one original trigger without serializing unrelated workflow schedules.
/// </summary>
public class QuartzScheduleCoordinator(
    IDistributedLockProvider distributedLockProvider,
    IOptions<DistributedLockingOptions> distributedLockingOptions) : IQuartzScheduleCoordinator
{
    /// <inheritdoc />
    public async Task ExecuteAsync(TriggerKey originalTriggerKey, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        var lockName = QuartzTriggerKeys.GetScheduleLockKey(originalTriggerKey);
        var timeout = distributedLockingOptions.Value.LockAcquisitionTimeout;

        await using var lockHandle = await distributedLockProvider.AcquireLockAsync(lockName, timeout, cancellationToken: cancellationToken);
        await action(cancellationToken);
    }
}
