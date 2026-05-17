using Microsoft.Extensions.Hosting;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleLogSourceHealthService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
