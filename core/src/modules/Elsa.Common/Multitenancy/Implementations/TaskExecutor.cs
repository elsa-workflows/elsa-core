using System.Reflection;
using Elsa.Common.DistributedHosting;
using Elsa.Common.RecurringTasks;
using Medallion.Threading;
using Microsoft.Extensions.Options;

namespace Elsa.Common.Multitenancy;

public class TaskExecutor(IDistributedLockProvider distributedLockProvider, ITenantAccessor tenantAccessor, IOptions<DistributedLockingOptions> options) : ITaskExecutor, IBackgroundTaskStarter
{
    public async Task ExecuteTaskAsync(ITask task, CancellationToken cancellationToken)
    {
        await ExecuteInternalAsync(task, () => task.ExecuteAsync(cancellationToken), cancellationToken);
    }

    public async Task StartAsync(IBackgroundTask task, CancellationToken cancellationToken)
    {
        await ExecuteInternalAsync(task, () => task.StartAsync(cancellationToken), cancellationToken);
    }

    public async Task StopAsync(IBackgroundTask task, CancellationToken cancellationToken)
    {
        await ExecuteInternalAsync(task, () => task.StopAsync(cancellationToken), cancellationToken);
    }

    private async Task ExecuteInternalAsync(ITask task, Func<Task> action, CancellationToken cancellationToken)
    {
        var taskType = task.GetType();
        var singleNodeTask = taskType.GetCustomAttribute<SingleNodeTaskAttribute>();

        if (singleNodeTask == null)
        {
            await action();
            return;
        }

        var resourceName = GetResourceName(taskType, singleNodeTask.Scope);

        await using (await distributedLockProvider.AcquireLockAsync(resourceName, options.Value.LockAcquisitionTimeout, cancellationToken: cancellationToken))
            await action();
    }

    private string GetResourceName(Type taskType, SingleNodeTaskScope scope)
    {
        var taskName = taskType.AssemblyQualifiedName!;

        // A host-scoped task guards something every tenant shares, so its lock name must be the same for all of them.
        if (scope == SingleNodeTaskScope.Host)
            return taskName;

        var tenantId = tenantAccessor.TenantId;

        if (tenantId == Tenant.DefaultTenantId)
            return taskName;

        return $"{taskName}:{tenantId}";
    }
}
