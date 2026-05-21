using Elsa.AI.Abstractions.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.AI.Host.Services;

public class AiContextProviderValidationHostedService(IEnumerable<IAiContextProvider> providers) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = providers;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
