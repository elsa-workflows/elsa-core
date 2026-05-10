using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.RealTime;

public interface IStructuredLogsClient
{
    Task ReceiveLogEventAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default);
    
    Task ReceiveDroppedEventsAsync(StructuredLogDroppedEventSummary summary, CancellationToken cancellationToken = default);
    
    Task ReceiveSourceChangedAsync(StructuredLogSource source, CancellationToken cancellationToken = default);
}
