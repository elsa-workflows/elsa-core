namespace Elsa.Retention.Options;

/// <summary>
///     Retention options
/// </summary>
public class CleanupOptions
{
    /// <summary>
    ///     Controls how often the database is checked for workflow instances and execution log records to remove.
    /// </summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    ///     Controls the page size of the workflow instance that are retained in a single batch
    /// </summary>
    public int PageSize { get; set; } = 25;
}