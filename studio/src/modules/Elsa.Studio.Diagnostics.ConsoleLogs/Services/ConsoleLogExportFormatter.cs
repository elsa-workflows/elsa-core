using Elsa.Studio.Diagnostics.ConsoleLogs.Models;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Formats visible console rows for copy/export.
/// </summary>
public class ConsoleLogExportFormatter
{
    /// <summary>
    /// Formats rows as tab-separated text with enough context outside Studio.
    /// </summary>
    public string FormatVisibleRows(IEnumerable<ConsoleLogLine> rows)
    {
        return string.Join(Environment.NewLine, rows.Select(FormatLine));
    }

    private static string FormatLine(ConsoleLogLine line)
    {
        return $"{line.Timestamp:O}\t{StreamLabel(line.Stream)}\t{SourceDisplayName(line.Source)}\t{line.Text}";
    }

    /// <summary>
    /// Formats a source display name.
    /// </summary>
    public static string SourceDisplayName(ConsoleLogSource source)
    {
        if (!string.IsNullOrWhiteSpace(source.DisplayName))
            return source.DisplayName;

        var parts = new[] { source.ServiceName, source.PodName, source.ContainerName, source.MachineName, source.Id }
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join("/", parts);
    }

    /// <summary>
    /// Formats a stream label.
    /// </summary>
    public static string StreamLabel(ConsoleLogStream stream) => stream == ConsoleLogStream.Stderr ? "stderr" : "stdout";
}
