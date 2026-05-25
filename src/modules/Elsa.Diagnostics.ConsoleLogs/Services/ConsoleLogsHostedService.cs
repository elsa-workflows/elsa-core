using Microsoft.Extensions.Hosting;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Host-level hosted service that ensures the console capture is initialized at host build time,
/// periodically flushes idle partial lines, and drains pending writes during host shutdown.
/// </summary>
public sealed class ConsoleLogsHostedService : BackgroundService
{
    private readonly Action? _configureHost;

    public ConsoleLogsHostedService() : this(null)
    {
    }

    public ConsoleLogsHostedService(Action? configureHost)
    {
        _configureHost = configureHost;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _configureHost?.Invoke();

        // Touch the host so initialization happens before the first request, not lazily on first capture.
        ConsoleLogsHost.AddReference();
        ConsoleLogsHost.EnsureInitialized();
        return base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        await ConsoleLogsHost.ReleaseReferenceAsync().ConfigureAwait(false);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMilliseconds(Math.Max(100, ConsoleLogsHost.Options.Value.IdleFlushTimeout.TotalMilliseconds / 2));

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(interval, stoppingToken).ConfigureAwait(false);
            await ConsoleLogsHost.Capture.FlushIdleAsync(stoppingToken).ConfigureAwait(false);
        }
    }
}

