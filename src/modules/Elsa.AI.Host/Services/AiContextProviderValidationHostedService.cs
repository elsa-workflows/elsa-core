using Elsa.AI.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.AI.Host.Services;

public class AiContextProviderValidationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<AiContextProviderValidationHostedService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var providers = scope.ServiceProvider.GetServices<IAiContextProvider>();

        foreach (var group in providers.GroupBy(x => x.Kind, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1))
            logger.LogWarning(
                "Multiple AI context providers are registered for kind {ContextKind}. The last registered provider will be used.",
                group.Key);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
