using Elsa.Common.Entities;
using Elsa.Workflows.Runtime.Options;

namespace Elsa.Workflows.Runtime.Entities;

/// <summary>
/// Represents a bookmark queue item that was moved out of the active queue for operator inspection.
/// </summary>
public class BookmarkQueueDeadLetterItem : Entity
{
    /// <summary>
    /// The ID of the original bookmark queue item.
    /// </summary>
    public string OriginalQueueItemId { get; set; } = null!;

    /// <summary>
    /// The workflow instance ID owning the bookmark to resume.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// The correlation ID associated with the workflow instance owning the bookmark to resume.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The bookmark ID to resume.
    /// </summary>
    public string? BookmarkId { get; set; }

    /// <summary>
    /// A bookmark payload hash of the bookmark to resume.
    /// </summary>
    public string? StimulusHash { get; set; }

    /// <summary>
    /// The ID of the activity instance associated with the bookmark.
    /// </summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// The type name of the activity associated with the bookmark.
    /// </summary>
    public string? ActivityTypeName { get; set; }

    /// <summary>
    /// Any options to apply when resuming the bookmark.
    /// </summary>
    public ResumeBookmarkOptions? Options { get; set; }

    /// <summary>
    /// The timestamp that the original queue item was created.
    /// </summary>
    public DateTimeOffset OriginalCreatedAt { get; set; }

    /// <summary>
    /// The timestamp that this item was moved to the dead-letter store.
    /// </summary>
    public DateTimeOffset DeadLetteredAt { get; set; }

    /// <summary>
    /// The reason this item was moved to the dead-letter store.
    /// </summary>
    public string Reason { get; set; } = null!;

    /// <summary>
    /// The number of failed delivery attempts observed before dead-lettering.
    /// </summary>
    public int DeliveryAttempts { get; set; }

    /// <summary>
    /// The timestamp of the last failed delivery attempt.
    /// </summary>
    public DateTimeOffset? LastAttemptedAt { get; set; }

    /// <summary>
    /// The type of the last exception observed while processing this item.
    /// </summary>
    public string? LastErrorType { get; set; }

    /// <summary>
    /// The message of the last exception observed while processing this item.
    /// </summary>
    public string? LastErrorMessage { get; set; }

    /// <summary>
    /// Whether this item can be replayed.
    /// </summary>
    public bool CanReplay { get; set; } = true;

    /// <summary>
    /// The timestamp that this item was replayed.
    /// </summary>
    public DateTimeOffset? ReplayedAt { get; set; }

    /// <summary>
    /// The ID of the queue item created when this dead-letter item was replayed.
    /// </summary>
    public string? ReplayedQueueItemId { get; set; }
}
