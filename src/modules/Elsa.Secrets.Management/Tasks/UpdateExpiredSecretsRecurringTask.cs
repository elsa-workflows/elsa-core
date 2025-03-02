using Elsa.Common;
using Elsa.Common.RecurringTasks;
using JetBrains.Annotations;

namespace Elsa.Secrets.Management.Tasks;

[UsedImplicitly]
[SingleNodeTask]
public class UpdateExpiredSecretsRecurringTask(IExpiredSecretsUpdater expiredSecretsUpdater) : RecurringTask
{
    public override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await expiredSecretsUpdater.UpdateExpiredSecretsAsync(stoppingToken);
    }
}