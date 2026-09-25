using CShells.Lifecycle;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using IQuartzScheduler = Quartz.IScheduler;
using IQuartzSchedulerFactory = Quartz.ISchedulerFactory;

namespace Elsa.Scheduling.Quartz.ShellFeatures;

/// <summary>
/// Starts and stops the Quartz scheduler in sync with the CShells shell lifecycle.
/// </summary>
/// <remarks>
/// Shell activation always occurs after <c>IHostApplicationLifetime.ApplicationStarted</c>
/// has fired (shells are activated inside <c>ShellStartupHostedService.StartAsync</c>),
/// so the <c>AwaitApplicationStarted</c> deferral logic from <c>QuartzHostedService</c>
/// is intentionally omitted — it is irrelevant in this context.
/// </remarks>
[LifecycleOrder(LifecyclePhase.Start, 100)]
public class QuartzShellLifecycleHandler(
    IQuartzSchedulerFactory schedulerFactory,
    IHostApplicationLifetime appLifetime,
    ILogger<QuartzShellLifecycleHandler> logger)
    : IShellInitializer, IDrainHandler
{
    private IQuartzScheduler? _scheduler;
    private Task? _startupTask;

    /// <summary>
    /// Optional delay before the scheduler begins processing jobs after activation.
    /// Mirrors <c>QuartzHostedServiceOptions.StartDelay</c>.
    /// </summary>
    public TimeSpan? StartDelay { get; set; }

    /// <summary>
    /// Whether to wait for running jobs to complete before the scheduler shuts down.
    /// Mirrors <c>QuartzHostedServiceOptions.WaitForJobsToComplete</c>. Defaults to <c>true</c>.
    /// </summary>
    public bool WaitForJobsToComplete { get; set; } = true;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting Quartz scheduler for shell");
        try
        {
            _scheduler = await schedulerFactory.GetScheduler(cancellationToken);
            _startupTask = StartSchedulerAsync(cancellationToken);
            await _startupTask.ConfigureAwait(false);
            logger.LogInformation("Quartz scheduler started successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start Quartz scheduler for shell");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DrainAsync(IDrainExtensionHandle extensionHandle, CancellationToken cancellationToken)
    {
        if (_scheduler is null || _startupTask is null) return;

        logger.LogInformation("Stopping Quartz scheduler for shell");
        try
        {
            // Wait for any in-progress startup to finish before shutting down,
            // mirroring the StopAsync logic in QuartzHostedService.
            await Task.WhenAny(_startupTask, Task.Delay(Timeout.Infinite, cancellationToken))
                .ConfigureAwait(false);

            await _scheduler.Shutdown(WaitForJobsToComplete, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Quartz scheduler stopped successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to stop Quartz scheduler for shell");
        }
    }

    private async Task StartSchedulerAsync(CancellationToken cancellationToken)
    {
        // Guard against a race where application shutdown begins before we finish starting.
        // Mirrors the equivalent check in QuartzHostedService.StartSchedulerAsync.
        if (appLifetime.ApplicationStopping.IsCancellationRequested)
            return;

        if (StartDelay.HasValue)
            await _scheduler!.StartDelayed(StartDelay.Value, cancellationToken).ConfigureAwait(false);
        else
            await _scheduler!.Start(cancellationToken).ConfigureAwait(false);
    }
}
