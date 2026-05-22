namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Provides options to the workflow dispatcher.
/// </summary>
public class WorkflowDispatcherOptions
{
    /// <summary>
    /// A list of available channels to dispatch workflows to.
    /// </summary>
    /// <remarks>
    /// Channels are used to dispatch workflows to different queues or endpoints.
    /// </remarks>
    public ICollection<DispatcherChannel> Channels { get; set; } = new List<DispatcherChannel>();

    /// <summary>
    /// Gets or sets whether workflow dispatch calls made during workflow execution are written to the transactional outbox.
    /// </summary>
    public bool UseTransactionalOutbox { get; set; }

    /// <summary>
    /// Gets or sets whether the shared outbox processor should run immediately after a workflow state commit that contains outbox items.
    /// </summary>
    /// <remarks>
    /// The immediate processor drains eligible pending items from the shared outbox. Disable this option to rely only on the recurring outbox sweep when commit latency is more important than eager dispatch.
    /// </remarks>
    public bool ProcessOutboxAfterCommit { get; set; } = true;

    /// <summary>
    /// Gets or sets how long an outbox item whose owner workflow instance cannot be found should be retained before cleanup.
    /// </summary>
    public TimeSpan OrphanedOutboxItemRetention { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Gets or sets the maximum number of failed delivery attempts before an outbox item is abandoned.
    /// </summary>
    public int MaxOutboxDeliveryAttempts { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of outbox items to load and process during one processor cycle.
    /// </summary>
    public int OutboxProcessorBatchSize { get; set; } = 100;
}
