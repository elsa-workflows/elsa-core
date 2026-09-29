using Elsa.Studio.Diagnostics.ConsoleLogs.Models;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Evaluates whether a console line matches the current viewer filter.
/// </summary>
public static class ConsoleLogFilterMatcher
{
    private const string WorkflowInstanceIdMetadataKey = "elsa.workflowInstanceId";
    private const string ActivityInstanceIdMetadataKey = "elsa.activityInstanceId";
    private const string ActivityIdMetadataKey = "elsa.activityId";
    private const string ActivityNodeIdMetadataKey = "elsa.activityNodeId";

    /// <summary>
    /// Returns true when the line matches all active filter fields.
    /// </summary>
    public static bool IsMatch(ConsoleLogLine line, ConsoleLogFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.SourceId) && !string.Equals(line.Source.Id, filter.SourceId, StringComparison.OrdinalIgnoreCase))
            return false;

        if (filter.Stream is not null && line.Stream != filter.Stream)
            return false;

        var receivedAt = line.ReceivedAt ?? line.Timestamp;

        if (filter.From is not null && receivedAt < filter.From)
            return false;

        if (filter.To is not null && receivedAt > filter.To)
            return false;

        if (!string.IsNullOrWhiteSpace(filter.Query) && !ContainsQuery(line, filter.Query))
            return false;

        if (!MatchesMetadata(line, WorkflowInstanceIdMetadataKey, filter.WorkflowInstanceId))
            return false;

        if (!MatchesMetadata(line, ActivityInstanceIdMetadataKey, filter.ActivityInstanceId))
            return false;

        if (!MatchesMetadata(line, ActivityIdMetadataKey, filter.ActivityId))
            return false;

        if (!MatchesMetadata(line, ActivityNodeIdMetadataKey, filter.ActivityNodeId))
            return false;

        return true;
    }

    private static bool ContainsQuery(ConsoleLogLine line, string query)
    {
        return line.Text.Contains(query, StringComparison.OrdinalIgnoreCase)
            || Contains(line.Source.Id, query)
            || Contains(line.Source.DisplayName, query)
            || Contains(line.Source.ServiceName, query)
            || Contains(line.Source.MachineName, query)
            || Contains(line.Source.PodName, query)
            || Contains(line.Source.ContainerName, query)
            || Contains(line.Source.Namespace, query)
            || Contains(line.Source.NodeName, query)
            || line.Metadata.Any(x => Contains(x.Key, query) || Contains(x.Value, query));
    }

    private static bool MatchesMetadata(ConsoleLogLine line, string key, string? filterValue)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
            return true;

        return TryGetMetadataValue(line, key, out var value) && string.Equals(value, filterValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string? candidate, string query) =>
        candidate?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false;

    private static bool TryGetMetadataValue(ConsoleLogLine line, string key, out string value)
    {
        if (line.Metadata.TryGetValue(key, out value!))
            return true;

        foreach (var item in line.Metadata)
        {
            if (!string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
                continue;

            value = item.Value;
            return true;
        }

        value = "";
        return false;
    }
}
