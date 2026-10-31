using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Contracts;

public interface IStructuredLogProvider
{
    ValueTask PublishAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default);
    
    ValueTask<RecentStructuredLogsResult> GetRecentAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);
    
    IAsyncEnumerable<StructuredLogEvent> SubscribeAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);
    
    ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
