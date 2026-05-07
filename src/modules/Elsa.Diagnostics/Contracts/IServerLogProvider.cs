using Elsa.Diagnostics.Models;

namespace Elsa.Diagnostics.Contracts;

public interface IServerLogProvider
{
    ValueTask PublishAsync(ServerLogEvent logEvent, CancellationToken cancellationToken = default);
    
    ValueTask<RecentServerLogsResult> GetRecentAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
    
    IAsyncEnumerable<ServerLogEvent> SubscribeAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
    
    ValueTask<IReadOnlyCollection<ServerLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
