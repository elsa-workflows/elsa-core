using Elsa.Diagnostics.StructuredLogs.Contracts;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Contracts;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Services;

public class StructuredLogWriteBufferStorageDiagnostics(IStructuredLogWriteBuffer writeBuffer) : IStructuredLogStorageDiagnostics
{
    public long DroppedWriteCount => writeBuffer.DroppedWriteCount;
}
