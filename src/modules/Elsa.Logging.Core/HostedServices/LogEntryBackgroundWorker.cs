using Elsa.Logging.Contracts;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Logging.HostedServices;

/// <summary>
/// Represents a background worker responsible for processing log entries from a queue
/// and routing them to appropriate log sinks.
/// </summary>
[UsedImplicitly]
public class LogEntryBackgroundWorker(ILogEntryQueue queue, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var instruction in queue.DequeueAsync().WithCancellation(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var router = scope.ServiceProvider.GetRequiredService<ILogSinkRouter>();
            await router.WriteAsync(
                instruction.SinkNames,
                instruction.Category,
                instruction.Level,
                instruction.Message,
                instruction.Arguments,
                instruction.Attributes, stoppingToken);
        }
    }
}
