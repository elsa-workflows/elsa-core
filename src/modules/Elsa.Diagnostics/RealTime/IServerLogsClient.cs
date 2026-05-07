using Elsa.Diagnostics.Models;

namespace Elsa.Diagnostics.RealTime;

public interface IServerLogsClient
{
    Task ReceiveLogEventAsync(ServerLogEvent logEvent, CancellationToken cancellationToken = default);
    
    Task ReceiveDroppedEventsAsync(ServerLogDroppedEventSummary summary, CancellationToken cancellationToken = default);
    
    Task ReceiveSourceChangedAsync(ServerLogSource source, CancellationToken cancellationToken = default);
}
