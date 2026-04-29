using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.HostedServices;

/// <summary>
/// Subscribes to <see cref="IHostApplicationLifetime.ApplicationStopping"/> and runs the drain orchestrator on
/// host stop. Registered AFTER <c>InstanceHeartbeatService</c> so that <see cref="IHostedService.StopAsync"/>
/// runs in reverse order — drain first, heartbeat last (research R5 / FR-029).
/// </summary>
public sealed class DrainOrchestratorHostedService : IHostedService
{
    private readonly IDrainOrchestrator _orchestrator;
    private readonly ILogger<DrainOrchestratorHostedService> _logger;

    public DrainOrchestratorHostedService(IDrainOrchestrator orchestrator, ILogger<DrainOrchestratorHostedService> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            var outcome = await _orchestrator.DrainAsync(DrainTrigger.HostStopSignal, cancellationToken);
            if (outcome.OverallResult is DrainResult.DeadlineExceeded or DrainResult.AbortedByUnhandledException)
                _logger.LogWarning("Graceful drain finished with non-clean result: {Result} (forceCancelled={Count}).", outcome.OverallResult, outcome.ExecutionCyclesForceCancelledCount);
            else
                _logger.LogInformation("Graceful drain finished: {Result}.", outcome.OverallResult);
        }
        catch (InvalidOperationException ex)
        {
            // A previous drain already ran (e.g., admin force endpoint). Safe to ignore on host stop.
            _logger.LogInformation("Host-stop drain skipped: {Reason}", ex.Message);
        }
    }
}
