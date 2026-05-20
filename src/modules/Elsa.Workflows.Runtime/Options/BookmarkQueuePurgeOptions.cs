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

    /// <summary>
    /// The maximum number of failed delivery attempts before a queue item is moved to the dead-letter store.
    /// </summary>
    public int MaxDeliveryAttempts { get; set; } = 3;

    /// <summary>
    /// The time-to-live for bookmark queue dead-letter items.
    /// </summary>
    public TimeSpan DeadLetterTtl { get; set; } = TimeSpan.FromDays(7);
}
