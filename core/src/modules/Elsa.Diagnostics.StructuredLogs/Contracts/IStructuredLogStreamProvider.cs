using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Contracts;

public interface IStructuredLogStreamProvider : IStructuredLogProvider
{
    IAsyncEnumerable<StructuredLogStreamItem> SubscribeWithDroppedEventsAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);
}
