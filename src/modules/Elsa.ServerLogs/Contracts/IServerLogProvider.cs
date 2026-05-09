using Elsa.ServerLogs.Models;

namespace Elsa.ServerLogs.Contracts;

public interface IServerLogProvider
{
    ValueTask PublishAsync(ServerLogEvent logEvent, CancellationToken cancellationToken = default);
    
    ValueTask<RecentServerLogsResult> GetRecentAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
    
    IAsyncEnumerable<ServerLogEvent> SubscribeAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
    
    ValueTask<IReadOnlyCollection<ServerLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
