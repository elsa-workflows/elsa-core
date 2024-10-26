using System.Reflection;
using Elsa.Common.DistributedHosting;
using Elsa.Common.RecurringTasks;
using Medallion.Threading;
using Microsoft.Extensions.Options;

namespace Elsa.Common.Multitenancy.HostedServices;

public class TaskExecutor(IDistributedLockProvider distributedLockProvider, IOptions<DistributedLockingOptions> options)
{
    public async Task ExecuteTaskAsync(ITask task, CancellationToken cancellationToken)
    {
        var taskType = task.GetType();
        var singleNodeTask = taskType.GetCustomAttribute<SingleNodeTaskAttribute>() != null;

        if (singleNodeTask)
        {
            var resourceName = taskType.AssemblyQualifiedName!;
            await using (await distributedLockProvider.AcquireLockAsync(resourceName, options.Value.LockAcquisitionTimeout, cancellationToken: cancellationToken)) 
                await task.ExecuteAsync(cancellationToken);
        }
        else
        {
            await task.ExecuteAsync(cancellationToken);
        }
    }
}