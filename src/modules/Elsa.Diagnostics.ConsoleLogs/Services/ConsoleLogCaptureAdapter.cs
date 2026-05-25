using Elsa.Diagnostics.ConsoleLogs.Contracts;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

#pragma warning disable CS0618
[Obsolete("Console log capture is host-owned. Use ConsoleLogsHost.Capture for host-level operations.")]
public sealed class ConsoleLogCaptureAdapter : IConsoleLogCapture
#pragma warning restore CS0618
{
    public ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        ConsoleLogsHost.EnsureInitialized();
        return ValueTask.CompletedTask;
    }

    public ValueTask StopAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public ValueTask FlushIdleAsync(CancellationToken cancellationToken = default) =>
        ConsoleLogsHost.Capture.FlushIdleAsync(cancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
