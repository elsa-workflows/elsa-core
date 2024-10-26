using Elsa.Common;
using JetBrains.Annotations;

namespace Elsa.Http.HostedServices;

/// <summary>
/// Update the route table based on workflow triggers and bookmarks.
/// </summary>
[UsedImplicitly]
public class UpdateRouteTableStartupTask(IRouteTableUpdater routeTableUpdater) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await routeTableUpdater.UpdateAsync(stoppingToken);
    }
}