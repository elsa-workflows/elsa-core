using Elsa.ServerLogs.Models;

namespace Elsa.ServerLogs.Contracts;

public interface IServerLogStreamProvider : IServerLogProvider
{
    IAsyncEnumerable<ServerLogStreamItem> SubscribeWithDroppedEventsAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
}
