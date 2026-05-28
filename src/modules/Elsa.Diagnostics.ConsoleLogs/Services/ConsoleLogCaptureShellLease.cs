using ConsoleLogStreaming.Core;
using ConsoleLogStreaming.Core.Capture;
using CShells.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

public class ConsoleLogCaptureShellLease(IServiceProvider serviceProvider)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _started;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_started)
                return;

            ConsoleLogStreamingHost.AddReference();
            var releaseReference = true;

            try
            {
                var capture = serviceProvider.GetRequiredService<IConsoleLogCapture>();
                await capture.StartAsync(cancellationToken);
                _started = true;
                releaseReference = false;
            }
            finally
            {
                if (releaseReference)
                    await ConsoleLogStreamingHost.ReleaseReferenceAsync(CancellationToken.None);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!_started)
                return;

            _started = false;
            await ConsoleLogStreamingHost.ReleaseReferenceAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }
}

public class ConsoleLogCaptureShellInitializer(ConsoleLogCaptureShellLease lease) : IShellInitializer
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) => lease.StartAsync(cancellationToken);
}

public class ConsoleLogCaptureShellDrainHandler(ConsoleLogCaptureShellLease lease) : IDrainHandler
{
    public Task DrainAsync(IDrainExtensionHandle extensionHandle, CancellationToken cancellationToken) => lease.StopAsync(cancellationToken);
}
