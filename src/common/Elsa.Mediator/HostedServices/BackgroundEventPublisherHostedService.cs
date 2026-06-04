using Elsa.Mediator.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Hosted service adapter for <see cref="BackgroundNotificationProcessor"/>.
/// </summary>
public class BackgroundEventPublisherHostedService(BackgroundNotificationProcessor processor) : BackgroundService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundEventPublisherHostedService"/> class.
    /// </summary>
    public BackgroundEventPublisherHostedService(BackgroundNotificationProcessor processor, int workerCount) : this(processor)
    {
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => processor.ExecuteAsync(stoppingToken);
}
