using Elsa.Common.Entities;

namespace Elsa.Workflows.Runtime.Entities;

/// <summary>
/// Represents a bookmark that has been stored in the database.
/// </summary>
public class StoredBookmark : Entity
{
    /// <summary>
    /// The name of the activity type associated with the bookmark.
    /// </summary>
    public string ActivityTypeName { get; set; } = null!;

    /// <summary>
    /// The hash of the bookmark.
    /// </summary>
    public string Hash { get; set; } = null!;

    /// <summary>
    /// The ID of the workflow instance associated with the bookmark.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = null!;

    /// <summary>
    /// The ID of the activity instance associated with the bookmark.
    /// </summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// The correlation ID of the workflow instance associated with the bookmark.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The data associated with the bookmark.
    /// </summary>
    public object? Payload { get; set; }

    /// <summary>
    /// Custom properties associated with the bookmark.
    /// </summary>
    public IDictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// The date and time the bookmark was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}