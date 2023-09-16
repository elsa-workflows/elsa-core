using System.Text.Json.Serialization;

namespace Elsa.Workflows.Runtime.Entities;

/// <summary>
/// Represents a bookmark that has been stored in the database.
/// </summary>
public class StoredBookmark
{
    /// <summary>
    /// Represents a bookmark that has been stored in the database.
    /// </summary>
    public StoredBookmark(
        string bookmarkId,
        string activityTypeName,
        string hash,
        string workflowInstanceId,
        DateTimeOffset createdAt,
        string? activityInstanceId = default,
        string? correlationId = default,
        object? payload = default,
        IDictionary<string, string>? metadata = default)
    {
        BookmarkId = bookmarkId;
        ActivityTypeName = activityTypeName;
        Hash = hash;
        WorkflowInstanceId = workflowInstanceId;
        CreatedAt = createdAt;
        ActivityInstanceId = activityInstanceId;
        CorrelationId = correlationId;
        Payload = payload;
        Metadata = metadata;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoredBookmark"/> class.
    /// </summary>
    [JsonConstructor]
    public StoredBookmark()
    {
    }

    /// <summary>
    /// The ID of the bookmark.
    /// </summary>
    public string BookmarkId { get; set; } = default!;

    /// <summary>
    /// The name of the activity type associated with the bookmark.
    /// </summary>
    public string ActivityTypeName { get; set; } = default!;

    /// <summary>
    /// The hash of the bookmark.
    /// </summary>
    public string Hash { get; set; } = default!;

    /// <summary>
    /// The ID of the workflow instance associated with the bookmark.
    /// </summary>
    public string WorkflowInstanceId { get; set; } = default!;

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