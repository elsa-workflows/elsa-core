using Microsoft.Extensions.Hosting;

namespace Elsa.Tenants.HostedServices;

public class CreateShellsHostedService(IShellHost shellHost) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await shellHost.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}