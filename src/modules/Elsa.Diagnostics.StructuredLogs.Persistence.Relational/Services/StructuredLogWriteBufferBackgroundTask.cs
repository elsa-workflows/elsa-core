using Elsa.Common;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;

public class StructuredLogWriteBufferBackgroundTask(StructuredLogWriteBuffer writeBuffer) : IBackgroundTask
{
    private bool _started;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
            return;

        await writeBuffer.StartAsync(cancellationToken);
        _started = true;
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (!_started)
            return;

        _started = false;
        await writeBuffer.StopAsync(cancellationToken);
    }
}
