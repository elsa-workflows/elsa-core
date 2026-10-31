using Elsa.Mediator.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Hosted service adapter for <see cref="BackgroundCommandProcessor"/>.
/// </summary>
public class BackgroundCommandSenderHostedService(BackgroundCommandProcessor processor) : BackgroundService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundCommandSenderHostedService"/> class.
    /// </summary>
    public BackgroundCommandSenderHostedService(BackgroundCommandProcessor processor, int workerCount) : this(processor)
    {
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => processor.ExecuteAsync(stoppingToken);
}
