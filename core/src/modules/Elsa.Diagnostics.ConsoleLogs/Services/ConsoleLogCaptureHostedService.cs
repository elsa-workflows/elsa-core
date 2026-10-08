using ConsoleLogStreaming.Core;
using Microsoft.Extensions.Hosting;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal sealed class ConsoleLogCaptureHostedService(IConsoleLogCapture capture) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => capture.StartAsync(cancellationToken).AsTask();

    public Task StopAsync(CancellationToken cancellationToken) => capture.StopAsync(cancellationToken).AsTask();
}
