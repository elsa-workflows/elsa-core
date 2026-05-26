using ConsoleLogStreaming.Contracts;
using Elsa.Diagnostics.ConsoleLogs.Contracts;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal static class ConsoleLogFilterMapper
{
    public static ConsoleLogFilter ToStreamingFilter(ElsaConsoleLogFilter filter)
    {
        var metadata = filter.Metadata != null
            ? new Dictionary<string, string>(filter.Metadata, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(filter.WorkflowInstanceId))
            metadata[ConsoleLogMetadataKeys.WorkflowInstanceId] = filter.WorkflowInstanceId;

        return new()
        {
            SourceId = filter.SourceId,
            Stream = filter.Stream,
            Query = filter.Query,
            Metadata = metadata,
            From = filter.From,
            To = filter.To,
            Limit = filter.Limit
        };
    }
}
