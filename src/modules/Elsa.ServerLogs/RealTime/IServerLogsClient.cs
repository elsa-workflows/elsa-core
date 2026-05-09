using Elsa.ServerLogs.Models;

namespace Elsa.ServerLogs.RealTime;

public interface IServerLogsClient
{
    Task ReceiveLogEventAsync(ServerLogEvent logEvent, CancellationToken cancellationToken = default);
    
    Task ReceiveDroppedEventsAsync(ServerLogDroppedEventSummary summary, CancellationToken cancellationToken = default);
    
    Task ReceiveSourceChangedAsync(ServerLogSource source, CancellationToken cancellationToken = default);
}
