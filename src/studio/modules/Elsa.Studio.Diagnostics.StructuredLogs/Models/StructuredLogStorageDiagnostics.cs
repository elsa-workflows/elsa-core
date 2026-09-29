namespace Elsa.Studio.Diagnostics.StructuredLogs.Models;

/// <summary>
/// Storage-level diagnostics returned by the backend.
/// </summary>
public class StructuredLogStorageDiagnostics
{
    [System.Text.Json.Serialization.JsonPropertyName("droppedWriteCount")]
    public long DroppedWriteCount { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("hasStorageDiagnosticsProvider")]
    public bool HasStorageDiagnosticsProvider { get; set; }
}
