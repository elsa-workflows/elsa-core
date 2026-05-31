using Elsa.Mediator.Services;
using Microsoft.Extensions.Hosting;

namespace Elsa.Mediator.HostedServices;

/// <summary>
/// Hosted service adapter for <see cref="BackgroundJobProcessor"/>.
/// </summary>
public class JobRunnerHostedService(BackgroundJobProcessor processor) : BackgroundService
{
    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => processor.ExecuteAsync(stoppingToken);
}
