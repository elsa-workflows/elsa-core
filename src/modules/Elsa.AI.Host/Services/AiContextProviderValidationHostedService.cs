using Elsa.AI.Abstractions.Contracts;
using Microsoft.Extensions.Hosting;

namespace Elsa.AI.Host.Services;

public class AiContextProviderValidationHostedService(IEnumerable<IAiContextProvider> providers) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var duplicateGroups = providers
            .GroupBy(x => x.Kind, StringComparer.OrdinalIgnoreCase)
            .Where(x => x.Count() > 1)
            .ToList();

        if (duplicateGroups.Count > 0)
        {
            var duplicates = string.Join(", ", duplicateGroups.Select(group =>
            {
                var providerNames = string.Join("/", group.Select(provider => provider.GetType().Name));
                return $"{group.Key} ({providerNames})";
            }));
            throw new InvalidOperationException($"Multiple AI context providers are registered for the same kind: {duplicates}.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
