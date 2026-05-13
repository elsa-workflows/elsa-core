using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Contracts;

public interface IStructuredLogStore : IStructuredLogSink
{
    ValueTask<RecentStructuredLogsResult> QueryAsync(StructuredLogFilter filter, CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<StructuredLogSource>> ListSourcesAsync(CancellationToken cancellationToken = default);
}
