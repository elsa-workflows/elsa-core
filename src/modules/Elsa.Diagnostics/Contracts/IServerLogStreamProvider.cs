using Elsa.Diagnostics.Models;

namespace Elsa.Diagnostics.Contracts;

public interface IServerLogStreamProvider : IServerLogProvider
{
    IAsyncEnumerable<ServerLogStreamItem> SubscribeWithDroppedEventsAsync(ServerLogFilter filter, CancellationToken cancellationToken = default);
}
