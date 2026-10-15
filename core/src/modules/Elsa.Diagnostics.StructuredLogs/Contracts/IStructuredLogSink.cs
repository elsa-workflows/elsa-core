using Elsa.Diagnostics.StructuredLogs.Models;

namespace Elsa.Diagnostics.StructuredLogs.Contracts;

public interface IStructuredLogSink
{
    ValueTask WriteAsync(StructuredLogEvent logEvent, CancellationToken cancellationToken = default);

    async ValueTask WriteManyAsync(IReadOnlyCollection<StructuredLogEvent> logEvents, CancellationToken cancellationToken = default)
    {
        foreach (var logEvent in logEvents)
            await WriteAsync(logEvent, cancellationToken);
    }
}
