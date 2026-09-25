namespace Elsa.Studio.Diagnostics.ConsoleLogs.Models;

/// <summary>
/// Describes backend-dropped console lines.
/// </summary>
public class ConsoleLogDroppedLineSummary
{
    /// <summary>
    /// Gets or sets the affected source ID.
    /// </summary>
    public string? SourceId { get; set; }

    /// <summary>
    /// Gets or sets the affected stream.
    /// </summary>
    public ConsoleLogStream? Stream { get; set; }

    /// <summary>
    /// Gets or sets the optional reason.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the dropped-line count.
    /// </summary>
    public long Count { get; set; }

    /// <summary>
    /// Gets or sets the optional start timestamp for the dropped range.
    /// </summary>
    public DateTimeOffset? From { get; set; }

    /// <summary>
    /// Gets or sets the optional end timestamp for the dropped range.
    /// </summary>
    public DateTimeOffset? To { get; set; }
}
