using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Subscribes to <see cref="IHostApplicationLifetime.ApplicationStopping"/> and runs the drain orchestrator on
/// host stop. Registered AFTER <c>InstanceHeartbeatService</c> so that <see cref="IHostedService.StopAsync"/>
/// runs in reverse order — drain first, heartbeat last (research R5 / FR-029).
/// </summary>
public sealed class DrainOrchestratorHostedService(
    IDrainOrchestrator orchestrator,
    ILogger<DrainOrchestratorHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) =>
        DrainTriggerExecutor.RunAsync(orchestrator, DrainTrigger.HostStopSignal, logger, "Graceful drain", cancellationToken);
}
