namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Options for purging the bookmark queue.
/// </summary>
public class BookmarkQueuePurgeOptions
{
    /// <summary>
    /// The time-to-live for bookmark queue items.
    /// </summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The number of records to clean up per sweep.
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}