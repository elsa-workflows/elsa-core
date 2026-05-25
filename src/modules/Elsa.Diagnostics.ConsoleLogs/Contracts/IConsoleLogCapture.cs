namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

[Obsolete("Console log capture is host-owned. Use ConsoleLogsHost.Capture for host-level operations.")]
public interface IConsoleLogCapture : IAsyncDisposable
{
    ValueTask StartAsync(CancellationToken cancellationToken = default);
    ValueTask StopAsync(CancellationToken cancellationToken = default);
    ValueTask FlushIdleAsync(CancellationToken cancellationToken = default);
}
