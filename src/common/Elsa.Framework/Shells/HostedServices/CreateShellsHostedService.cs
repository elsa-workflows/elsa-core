using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;

namespace Elsa.Framework.Shells.HostedServices;

[UsedImplicitly]
public class CreateShellsHostedService(ITenantShellHost tenantShellHost) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await tenantShellHost.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}