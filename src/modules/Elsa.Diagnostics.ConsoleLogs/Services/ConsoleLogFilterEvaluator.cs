namespace Elsa.Diagnostics.ConsoleLogs.Services;

public static class ConsoleLogFilterEvaluator
{
    public static bool Matches(ConsoleLogLine line, ConsoleLogFilter filter)
    {
        if (!EqualsFilter(line.Source.Id, filter.SourceId))
            return false;

        if (filter.Stream is { } stream && line.Stream != stream)
            return false;

        if (!ContainsText(line, filter.Query))
            return false;

        if (!EqualsFilter(line.WorkflowInstanceId, filter.WorkflowInstanceId))
            return false;

        if (filter.From is { } from && line.ReceivedAt < from)
            return false;

        if (filter.To is { } to && line.ReceivedAt > to)
            return false;

        return true;
    }

    private static bool EqualsFilter(string? value, string? filter) => string.IsNullOrWhiteSpace(filter) || string.Equals(value, filter, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string? value, string? filter) => string.IsNullOrWhiteSpace(filter) || value?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true;

    private static bool ContainsText(ConsoleLogLine line, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return true;

        return Contains(line.Text, filter)
               || Contains(line.Source.Id, filter)
               || Contains(line.Source.DisplayName, filter)
               || Contains(line.Source.ServiceName, filter)
               || Contains(line.Source.MachineName, filter)
               || line.Source.Metadata.Any(x => Contains(x.Key, filter) || Contains(x.Value, filter));
    }
}
