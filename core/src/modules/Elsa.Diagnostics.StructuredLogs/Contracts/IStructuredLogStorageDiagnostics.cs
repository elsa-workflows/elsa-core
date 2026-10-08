namespace Elsa.Diagnostics.StructuredLogs.Contracts;

public interface IStructuredLogStorageDiagnostics
{
    long DroppedWriteCount { get; }
}
