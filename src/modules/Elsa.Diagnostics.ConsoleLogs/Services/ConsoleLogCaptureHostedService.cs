using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleLogCaptureHostedService(IConsoleLogCapture capture, IOptions<ConsoleLogsOptions> options) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await capture.StartAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await capture.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (capture is not ConsoleCaptureTee tee)
            return;

        var interval = TimeSpan.FromMilliseconds(Math.Max(100, options.Value.IdleFlushTimeout.TotalMilliseconds / 2));

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(interval, stoppingToken);
            tee.FlushIdleLines();
        }
    }
}
