using Elsa.Common;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;

public class StructuredLogWriteBufferBackgroundTask(StructuredLogWriteBuffer writeBuffer) : IBackgroundTask
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return writeBuffer.StartAsync(cancellationToken);
    }

    public Task ExecuteAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return writeBuffer.StopAsync(cancellationToken);
    }
}
