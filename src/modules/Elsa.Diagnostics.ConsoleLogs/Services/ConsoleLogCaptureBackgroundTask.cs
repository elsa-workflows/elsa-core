using Elsa.Common;
using Microsoft.Extensions.Options;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleLogCaptureBackgroundTask(IConsoleLogCapture capture, IOptions<ConsoleLogsOptions> options) : IBackgroundTask
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await capture.StartAsync(CancellationToken.None);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var interval = TimeSpan.FromMilliseconds(Math.Max(100, options.Value.IdleFlushTimeout.TotalMilliseconds / 2));

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(interval, cancellationToken);
                await capture.FlushIdleAsync(cancellationToken);
            }
        }
        finally
        {
            await capture.StopAsync(CancellationToken.None);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await capture.StopAsync(cancellationToken);
    }
}
