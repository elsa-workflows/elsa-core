using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Contracts;

public interface IStructuredLogLiveFeed
{
    ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default);

    IAsyncEnumerable<StructuredLogStreamItem> SubscribeAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);
}
