namespace Elsa.Diagnostics.ConsoleLogs.Contracts;

public interface IConsoleLogCapture : IAsyncDisposable
{
    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);

    ValueTask FlushIdleAsync(CancellationToken cancellationToken = default);
}
