using Elsa.AI.Abstractions.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.AI.Host.Services;

public class AiContextProviderValidationHostedService(
    IEnumerable<IAiContextProvider> providers,
    ILogger<AiContextProviderValidationHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var group in providers.GroupBy(x => x.Kind, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
            logger.LogWarning(
                "Multiple AI context providers are registered for kind {ContextKind}. The last registered provider will be used.",
                group.Key);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
