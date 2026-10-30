using Microsoft.Extensions.Hosting;

namespace Elsa.AI.Host.Services;

public class AIToolEnablementConfigurationHostedService(
    AIToolEnablementService enablementService,
    IEnumerable<Action<AIToolEnablementService>> configureActions) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var configure in configureActions)
            configure(enablementService);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
