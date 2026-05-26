using ConsoleLogStream.Core.Models;
using Elsa.Diagnostics.ConsoleLogs.Contracts;

namespace Elsa.Diagnostics.ConsoleLogs.Services;

internal static class ConsoleLogFilterMapper
{
    public static ConsoleLogFilter ToStreamingFilter(ElsaConsoleLogFilter filter)
    {
        var metadata = filter.Metadata != null
            ? new Dictionary<string, string>(filter.Metadata, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        AddMetadata(metadata, ConsoleLogMetadataKeys.WorkflowInstanceId, filter.WorkflowInstanceId);
        AddMetadata(metadata, ConsoleLogMetadataKeys.WorkflowDefinitionId, filter.WorkflowDefinitionId);
        AddMetadata(metadata, ConsoleLogMetadataKeys.WorkflowDefinitionVersionId, filter.WorkflowDefinitionVersionId);
        AddMetadata(metadata, ConsoleLogMetadataKeys.ActivityInstanceId, filter.ActivityInstanceId);
        AddMetadata(metadata, ConsoleLogMetadataKeys.ActivityId, filter.ActivityId);
        AddMetadata(metadata, ConsoleLogMetadataKeys.ActivityNodeId, filter.ActivityNodeId);

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

    private static void AddMetadata(IDictionary<string, string> metadata, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            metadata[key] = value;
    }
}
