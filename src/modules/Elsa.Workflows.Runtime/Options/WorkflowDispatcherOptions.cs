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
    /// Gets or sets whether the outbox should be processed immediately after each workflow state commit.
    /// </summary>
    public bool ProcessOutboxAfterCommit { get; set; } = true;
}
