using Elsa.Diagnostics.StructuredLogs.Contracts;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;

public interface IStructuredLogWriteBuffer : IStructuredLogSink
{
    long DroppedWriteCount { get; }

    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
